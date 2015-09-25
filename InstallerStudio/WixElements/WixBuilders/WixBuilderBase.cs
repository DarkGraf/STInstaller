using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

using InstallerStudio.Utils;
using InstallerStudio.Models;

namespace InstallerStudio.WixElements.WixBuilders
{
  public class ProcessRunnerEventArgs : EventArgs
  {
    public ProcessRunnerEventArgs(string message)
    {
      Message = message;
    }

    public string Message { get; private set; }
  }

  public interface IProcessRunner
  {
    /// <summary>
    /// Запуск процесса.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="arguments"></param>
    void Start(string fileName, string arguments);

    bool HasError { get; }

    event EventHandler<ProcessRunnerEventArgs> OutputMessageReceived;
  }

  // Паттерн "Шаблонный метод".
  abstract class WixBuilderBase : TempFileStoreBase
  {
    // Комментарии-скобки служащие для разработки MSI и подлежащие 
    // замене или удалению для формирования конечной MSI.
    protected const string DevelopmentInfoBegin = "<!--DEVELOPMENT_INFO_BEGIN-->";
    protected const string DevelopmentInfoEnd = "<!--DEVELOPMENT_INFO_END-->";
    protected const string EmptyComment = "<!---->";

    protected const string XmlNameSpaceWIX = "{http://schemas.microsoft.com/wix/2006/wi}";
    protected const string XmlNameSpaceST = "{http://www.systemt.ru/STSchema}";

    protected const string CandleDescription = "Запуск компилятора Candle.exe (получение объектных модулей по исходным XML-документам).";
    protected const string LightDescription = "Запуск компоновщика Light.exe (сборка инсталляционного пакета из объектных модулей и других ресурсов).";
    protected const string TorchDescription = "Запуск утилиты Torch.exe (генерации различий).";
    protected const string PyroDescription = "Запуск утилиты Pyro.exe (создание патчей (.msp-пакетов) без использования Windows Installer SDK).";

    private void LoadingTemplates(IBuildContext context, CancellationTokenSource cts)
    {
      string[] templateNames = GetTemplateFileNames();

      // У полученных путей определяем общую максимальную директорию (только если ресурсов болььше 1).
      // Далее ее удаляем, и во временной директории создаем копию структуры директорий
      // как в ресурсах, без общего пути.
      // Алгоритм поиска общего пути следующий.
      // В первой строке получаем диапазон от 0 до минимального количества символов
      // в путях минус один и реверсируем его.
      // Затем перебираем и берем первый путь, где для всех путей выполняется
      // условие: пути начинаются с заданной строки.
      string commonPath;
      if (templateNames.Length > 1)
      {
        commonPath = (from len in Enumerable.Range(0, templateNames.Min(s => s.Length)).Reverse()
                      let possiblePath = templateNames.First().Substring(0, len)
                      where templateNames.All(f => f.StartsWith(possiblePath))
                      select possiblePath).FirstOrDefault();
      }
      else
        commonPath = Path.GetDirectoryName(templateNames[0]).IncludeTrailingPathDelimiter();

      // Создаем анонимный тип с первоначальным путем и с удаленным общим путем.
      // Предусмотрим, что общий тип может быть null.
      var paths = from p in templateNames
                  select new
                  {
                    Original = p,
                    Relative = p.Remove(0, (commonPath ?? string.Empty).Length)
                  };

      foreach (var path in paths)
      {
        // Для получения ресурса используем первоначальный путь.
        string strUri = string.Format("pack://application:,,,/{0};component/{1}",
          Assembly.GetExecutingAssembly().GetName().Name, "WixElements/WixBuilders/" + path.Original);
        Uri uri = new Uri(strUri);

        StreamResourceInfo resourceInfo = Application.GetResourceStream(uri);

        // Проверим существование директории и при отсутствии ее, создадим.
        string directory = Path.Combine(StoreDirectory, Path.GetDirectoryName(path.Relative));
        if (!Directory.Exists(directory))
          Directory.CreateDirectory(directory);

        string fileName = Path.Combine(StoreDirectory, path.Relative);
        using (FileStream file = new FileStream(fileName, FileMode.CreateNew))
        {
          resourceInfo.Stream.CopyTo(file);
        }
      }
    }

    /// <summary>
    /// Удаляет в исходном файле информацию для разработки.
    /// </summary>
    /// <param name="content"></param>
    protected void DeleteDevelopmentInfo(string path)
    {
      string content = File.ReadAllText(path);

      // Определим количество вхождений открывающего и закрывающего комментария.
      int countBegin = new Regex(DevelopmentInfoBegin).Matches(content).Count;
      int countEnd = new Regex(DevelopmentInfoEnd).Matches(content).Count;
      if (countBegin != countEnd)
        throw new Exception(string.Format("Несоответствие количества комментариев {0} и {1}",
          DevelopmentInfoBegin, DevelopmentInfoEnd));

      // Заменяем комментарии разработки на пустые комментарии.
      // Сделано преднамеренно для контроля правильного удаления.
      while (countBegin-- > 0)
      {
        int begin = content.IndexOf(DevelopmentInfoBegin) + DevelopmentInfoBegin.Length;
        int end = content.IndexOf(DevelopmentInfoEnd);
        content = content.Remove(begin, end - begin);
        content = new Regex(DevelopmentInfoBegin).Replace(content, EmptyComment, 1);
        content = new Regex(DevelopmentInfoEnd).Replace(content, EmptyComment, 1);
      }

      File.WriteAllText(path, content);
    }

    private IProcessRunner CreateProcessRunner()
    {
      // Всегда будем создавать новый процесс, чтобы в наследниках
      // не очищать события привязанные к анонимным методам.
      return new ProcessRunner(new Process());
    }

    protected void RunCommand(IBuildContext context, string fileName, string command)
    {
      context.BuildMessageWriteLine(">" + fileName + " " + command, BuildMessageTypes.ConsoleSend);
      IProcessRunner runner = CreateProcessRunner();
      runner.OutputMessageReceived += (s, e) =>
      {
        context.BuildMessageWriteLine(e.Message, BuildMessageTypes.ConsoleReceive);
      };
      runner.Start(fileName, command);
      if (runner.HasError)
        throw new Exception(string.Format("Запуск {0} завершился с ошибкой.", Path.GetFileName(fileName)));
    }

    public void Build(IBuildContext context)
    {
      string prefix = context.OnlyCheck ? "Проверка" : "Сборка";

      context.ClearBuildMessage();
      context.BuildMessageWriteLine(prefix + " начата.", BuildMessageTypes.Notification);

      // Общее действие для вызова методов с отменой выполнения задач.
      // Параметры: 
      // a - метод для вызова;
      // c - токен для отмены и других операций;
      // m - сообщение о начале действия.
      Action<Action<IBuildContext, CancellationTokenSource>, IBuildContext, CancellationTokenSource, string> actionWithErrorHandling =
        (Action<IBuildContext, CancellationTokenSource> a, IBuildContext ctx, CancellationTokenSource c, string m) =>
        {
          // Не будем выводить названия действия при проверке, так как некоторые могут не выполнятся.
          if (!context.OnlyCheck)
            context.BuildMessageWriteLine(m, BuildMessageTypes.Notification);
          try 
          {
            a(ctx, c);
          }
          catch (Exception e)
          {
            context.BuildMessageWriteLine(e.Message, BuildMessageTypes.Error);
            c.Cancel();
            c.Token.ThrowIfCancellationRequested();
          }
        };

      Stopwatch stopwatch = Stopwatch.StartNew();
      CancellationTokenSource cts = new CancellationTokenSource();
      Task.Factory.
        StartNew(delegate
        {
          actionWithErrorHandling(CheckProduct, context, cts, "");
        }, cts.Token).
        ContinueWith(delegate
        {
          // Загружаем шаблоны во временную папку.
          actionWithErrorHandling(LoadingTemplates, context, cts, "Загрузка шаблонов.");
        }, cts.Token).
        ContinueWith(delegate
        {
          actionWithErrorHandling(ProcessingTemplates, context, cts, "Обработка шаблонов.");
        }, cts.Token).
        ContinueWith(delegate
        {
          actionWithErrorHandling(CompilationAndBuild, context, cts, "Компиляция и сборка.");
        }, cts.Token).
        ContinueWith(delegate
        {
          stopwatch.Stop();
          // Сюда токен не передаем.
          // Здесь должен быть код, выполняющийся в любом случае, завершились задачи или нет.
          context.BuildMessageWriteLine(
            string.Format(prefix + " завершена {0} за {1}.", cts.IsCancellationRequested ? "с ошибками" : "успешно",
            stopwatch.Elapsed.ToString("mm\\:ss")), 
            cts.IsCancellationRequested ? BuildMessageTypes.Error : BuildMessageTypes.Information);
          if (context.BuildIsFinished != null)
            context.BuildIsFinished();
        });
    }

    /// <summary>
    /// Проверка продукта. Здесь должна происходить проверка информации об ошибках
    /// сгенерированная атрибутами проверки. Остальные проверки могут делаться в 
    /// других методах выполняющихся при установленном флаге IBuildContext.OnlyCheck.
    /// </summary>
    protected void CheckProduct(IBuildContext context, CancellationTokenSource cts)
    {
      string error = ProductErrorInfo.Error;
      if (!string.IsNullOrEmpty(error))
        throw new Exception(error);
    }

    #region Абстрактные защищенные методы и свойства.

    /// <summary>
    /// Получает пути к шаблонам в ресурсах.
    /// </summary>
    /// <returns></returns>
    protected abstract string[] GetTemplateFileNames();

    protected abstract IDataErrorInfo ProductErrorInfo { get; }

    /// <summary>
    /// Обработка шаблонов.
    /// </summary>
    protected abstract void ProcessingTemplates(IBuildContext context, CancellationTokenSource cts);

    /// <summary>
    /// Компиляция и сборка.
    /// </summary>
    protected abstract void CompilationAndBuild(IBuildContext context, CancellationTokenSource cts);

    #endregion
  }
}

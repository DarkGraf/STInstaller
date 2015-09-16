using System;
using System.ComponentModel;
using System.Runtime.Serialization;

using InstallerStudio.Common;
using InstallerStudio.Models;
using InstallerStudio.WixElements.WixBuilders;
using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Описывает интерфейс самой главной сущности Wix.
  /// </summary>
  interface IWixMainEntity : INotifyPropertyChanged
  {
    /// <summary>
    /// Корневой элемент сущности Wix.
    /// </summary>
    IWixElement RootElement { get; }

    /// <summary>
    /// Построение целевого объекта.
    /// </summary>
    void Build(IBuildContext buildContext);
  }

  /// <summary>
  /// Контекст для построения.
  /// Содержит информацию необходимую для построения.
  /// </summary>
  interface IBuildContext
  {
    /// <summary>
    /// Вывод сообщений о построении.
    /// </summary>
    void BuildMessageWriteLine(string message, BuildMessageTypes messageType);
    /// <summary>
    /// Очистка сообщений о построении.
    /// </summary>
    void ClearBuildMessage();
    /// <summary>
    /// Настройки программы.
    /// </summary>
    ISettingsInfo ApplicationSettings { get; }
    /// <summary>
    /// Данные приложения.
    /// </summary>
    IApplicationInfo ApplicationInfo { get; }
    /// <summary>
    /// Имя файла загруженного проекта.
    /// </summary>
    string ProjectFileName { get; }
    /// <summary>
    /// Метод вызываемый когда построение закончено.
    /// Внимание. Вызывается с не UI потока.
    /// </summary>
    Action BuildIsFinished { get; }
    /// <summary>
    /// Временная директория исходных файлов (msizip, mspzip).
    /// </summary>
    string SourceStoreDirectory { get; }
  }

  /// <summary>
  /// Данные для приложения необходимые в процессе построения.
  /// </summary>
  public interface IApplicationInfo
  {
    /// <summary>
    ///  Возвращает директорию запуска приложения.
    /// </summary>
    string ApplicationDirectory { get; }
  }

  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixMainEntity : ChangeableObject, IWixMainEntity, IDataErrorInfo
  {
    IDataErrorHandler errorHandler;

    public WixMainEntity()
    {
      Initialize();
    }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context)
    {
      Initialize();
    }

    protected virtual void Initialize()
    {
      // При десериализации DataContractSerializer не вызывается конструктор
      // по умолчанию, поэтому вызываем данный метод из конструктора и из
      // метода OnDeserializing(). В нем производим всю необходимую инициализацию.
      errorHandler = new DataErrorHandler(this);
    }

    protected abstract WixBuilderBase CreateBuilder();

    #region IWixMainEntity

    [DataMember]
    public abstract IWixElement RootElement { get; protected set; }

    public void Build(IBuildContext buildContext)
    {
      // Внимание!!! Не используем конструкцию using, так как
      // внутри объекта происходит асинхронная работа и мы удалим 
      // быстрее чем она завершится. Среда сама удалит объект и 
      // ресурсы после освобождения всех ссылок.
      WixBuilderBase builder = CreateBuilder();
      builder.Build(buildContext);
    }

    #endregion

    #region IDataErrorInfo

    public string Error
    {
      get { return string.Empty; }
    }

    public string this[string columnName]
    {
      get { return errorHandler.Check(columnName); }
    }

    #endregion
  }
}

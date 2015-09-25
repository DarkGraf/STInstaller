using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  /// <summary>
  /// Базовый класс выполнения пользовательского действия.
  /// </summary>
  abstract class ActionWorkerBase : IActionWorker
  {
    public ActionWorkerBase(Session session)
    {
      Session = session;
    }

    public ActionResult Execute()
    {
      ActionResult result = ActionResult.Success;

      try
      {
        // Проверка разрешения выполнения.
        if ((UITypeMode == UIType.Unknow || (UITypeMode & Session.GetService<ISessionUITypeGetterExtension>().GetUIType()) != UIType.Unknow) && IsAllowed)
        {
          DebugHelper.WriteActionNameToLog(Session, GetType().Name);
          result = DoWork();
        }
      }
      catch (InstallCanceledException)
      {
        result = ActionResult.UserExit;
      }
      catch (Exception ex)
      {
        Log(ex);
        result = ActionResult.Failure;
      }

      return result;
    }

    protected Session Session { get; private set; }

    /// <summary>
    /// Разрешение по режиму клиент-сервер.
    /// </summary>
    protected abstract UIType UITypeMode { get; }

    /// <summary>
    /// Другие разрешения.
    /// </summary>
    protected abstract bool IsAllowed { get; }

    protected abstract ActionResult DoWork();

    private void Log(Exception ex)
    {
      string message = string.Format("{0} exception: {1}", ex.TargetSite, ex.ToString());
      Session.Log(message);
      System.Windows.Forms.MessageBox.Show(message, DebugHelper.GetActionName());
    }
  }

  /// <summary>
  /// Базовый класс действий для работы в разных фазах инсталляции.
  /// </summary>
  abstract class ActionInstallPhaseWorker : ActionWorkerBase
  {
    public ActionInstallPhaseWorker(Session session) : base(session) { }

    protected override ActionResult DoWork()
    {
      ActionResult result = ActionResult.Success;

      switch (Phase)
      {
        case InstallPhase.Immediate:
          result = DoWorkImmediate();
          break;
        case InstallPhase.Deferred:
          result = DoWorkDeferred();
          break;
        case InstallPhase.Rollback:
          result = DoWorkRollback();
          break;
        default:
          throw new NotImplementedException();
      }

      return result;
    }

    protected InstallPhase Phase { get { return Session.GetService<ISessionInstallPhaseExtension>().GetInstallPhase(); } }

    protected abstract ActionResult DoWorkImmediate();

    protected abstract ActionResult DoWorkDeferred();

    protected abstract ActionResult DoWorkRollback();
  }

  /// <summary>
  /// Базовый класс действий для поддержки WIX-расширений.
  /// </summary>
  abstract class ActionExtensionWorker : ActionInstallPhaseWorker
  {
    protected struct ActionDescription
    {
      public string Name { get; set; }
      public string Description { get; set; }
    }

    CurrentInstallStatus? installStatus;

    /// <summary>
    /// Имена методов-подписчиков для получения параметров.
    /// </summary>
    protected string[] Subscribers { get; private set; }

    public ActionExtensionWorker(Session session, string[] subscribers)
      : base(session)
    {
      this.installStatus = null;
      this.Subscribers = subscribers;
    }

    /// <summary>
    /// Показать название действия в UI.
    /// </summary>
    protected void UpdateActionDescription()
    {
      ActionDescription description = GetActionDescription(Session.GetService<ISessionInstallPhaseExtension>().GetInstallPhase());
      Record record = new Record(3);
      record[1] = description.Name;
      record[2] = description.Description;
      record[3] = "Incrementing tick [1] of [2]";
      Session.Message(InstallMessage.ActionStart, record);
    }

    protected override bool IsAllowed
    {
      get
      {
        bool result = (InstallStatus & WorkingStatus) != CurrentInstallStatus.Unknow;
        // Если выполение разрешено, выводим название действия в UI.
        if (result)
          UpdateActionDescription();
        return result;
      }
    }

    /// <summary>
    /// Статус текущей установки.
    /// </summary>
    protected CurrentInstallStatus InstallStatus
    {
      get
      {
        // Так как инициализируется один раз, во время отладки проверять
        // значение только через поле.
        if (!installStatus.HasValue)
        {
          /* В фазе Immediate определяется статус установки и он сохраняется в CustomActionData
           * для каждого подписчика. Фазы Deferred и Rollback считывают статус из CustomActionData. */
          switch (Session.GetService<ISessionInstallPhaseExtension>().GetInstallPhase())
          {
            case InstallPhase.Immediate:
              installStatus = Session.GetService<ISessionCurrentInstallStatusExtension>().GetStatus();
              // Здесь строго передача типа InstallStatus!!!
              Session.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData<CurrentInstallStatus>(installStatus.Value, Subscribers);
              break;
            case InstallPhase.Deferred:
            case InstallPhase.Rollback:
              installStatus = Session.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<CurrentInstallStatus>();
              break;
            default:
              installStatus = CurrentInstallStatus.Unknow;
              break;
          }
        }

        return installStatus.Value;
      }
    }

    /// <summary>
    /// Возвращает статусы при которых выполняется действие.
    /// </summary>
    protected abstract CurrentInstallStatus WorkingStatus { get; }

    /// <summary>
    /// Возвращает описание действия для вывода в UI для каждой фазы.
    /// </summary>
    protected abstract ActionDescription GetActionDescription(InstallPhase phase);
  }

  /// <summary>
  /// Расширение с возможностью генерации события прогресса.
  /// </summary>
  abstract class ActionProgressExtensionWorker : ActionExtensionWorker
  {
    public ActionProgressExtensionWorker(Session session, string[] subscribers = null) : base(session, subscribers) { }

    protected void OnProgress(int allCount, int increment, bool isInitializedData)
    {
      // Если произойдет отмена пользователем, то будет выброс исключения InstallCanceledException
      // и установка будет отменена.        
      Record record;

      // При первой итерации необходимо произвести подготовку.
      if (isInitializedData)
      {
        UpdateActionDescription();

        record = new Record(4);
        record[1] = "0"; // "Reset" message.
        record[2] = allCount.ToString(); // Total ticks.
        record[3] = "0"; // Forward motion (0 возрастает, 1 убывает).
        record[4] = "0"; // 0 = calc time remaining.
        Session.Message(InstallMessage.Progress, record);
      }

      // При инициализации посылаем 0, без этого ProgressBar не дойдет до конца.
      record = new Record(3);
      record[1] = "2"; // "ProgressReport" message.
      record[2] = increment.ToString(); // Ticks to increment.
      record[3] = "0"; // Ignore.
      Session.Message(InstallMessage.Progress, record);
    }
  }

  /// <summary>
  /// Класс для отладки.
  /// </summary>
  static class DebugHelper
  {
    /// <summary>
    /// Выводит имя метода CustomAction перед выполнением.
    /// </summary>
    internal static string GetActionName()
    {
      string methodName = "";
      System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
      for (int i = 0; i < st.FrameCount; i++)
      {
        string str = st.GetFrame(i).GetMethod().Name;
        if (str == "InvokeMethod")
          break;
        methodName = str;
      }

      return methodName;
    }

    internal static bool sessionIsInitialized = false;

    internal static void WriteActionNameToLog(Session session, string className)
    {
      // Записываем в файл, если он существует.
      string logFileName = Path.Combine(Path.GetTempPath(), "STInstallLogger.log");
      if (!File.Exists(logFileName))
        return;

      System.Text.StringBuilder builder = new System.Text.StringBuilder();

      if (!sessionIsInitialized)
      {
        sessionIsInitialized = true;
        builder.AppendLine("#New Session");

        if (!session.GetMode(InstallRunMode.Scheduled) && !session.GetMode(InstallRunMode.Rollback))
        {
          builder.AppendLine("Current Install Status: " + session.GetService<ISessionCurrentInstallStatusExtension>().GetStatus());

          builder.AppendLine("#Variables");

          string[] variables = new string[] { "Installed", "PATCH", "REMOVE", "REINSTALL", 
                                              "UPGRADINGPRODUCTCODE", "ACTION", "ADDLOCAL", 
                                              "REINSTALLMODE", "Preselected", "RESUME" };
          foreach (string state in variables)
            builder.AppendLine(string.Format(@"{0}=""{1}""", state, session[state]));
        }

        builder.AppendLine("#InstallRunMode");
        foreach (var e in Enum.GetValues(typeof(InstallRunMode)))
        {
          builder.AppendLine(string.Format(@"{0}=""{1}""", e.ToString(), session.GetMode((InstallRunMode)e)));
        }

        builder.AppendLine();
      }

      builder.AppendLine(string.Format("{0} Action: {1}, Class: {2}", DateTime.Now, GetActionName(), className));

      #region Запись в файл

      using (StreamWriter file = new StreamWriter(logFileName, true))
      {
        file.WriteLine(builder.ToString());
      }

      #endregion
    }
  }
}
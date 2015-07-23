using System;
using System.Reflection;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.ActionWorker;
using WixSTActions.SqlWorker;

namespace WixSTActions
{
  public partial class CustomAction
  {
    static ActionResult DispatcherExecute(Session session, string methodName)
    {
      CustomActionProperties properties = new CustomActionProperties(session);
      SqlWorkersFactory factory = new SqlWorkersFactory();

      switch (methodName)
      {
        case "AfterInstallInitializeImmediate":
          //System.Diagnostics.Debugger.Launch();
          // Добавляем общие действия.
          CommonInitialization(session, properties);
          // Пишем в отчет о старте установки (самым первым после установки UIType).
          ActionEngine.Instance.AddWorker(new ActionInstallReportWorker(session, ActionInstallReportWorkerMode.Start));
          // До выполнения плагинов (так как это действие выполяется первым), необходимо создать временную директорию в данном действии.
          ActionEngine.Instance.AddWorker(new ActionTempDirectoryControlWorker(session, ActionTempDirectoryControlWorkerMode.Create));
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session, true));
          break;
        case "AfterInstallInitializeDeferred":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session));
          break;
        case "AfterInstallInitializeRollback":
          //System.Diagnostics.Debugger.Launch();
          // Пишем в отчет об отмене установки (самым первым).
          ActionEngine.Instance.AddWorker(new ActionInstallReportWorker(session, ActionInstallReportWorkerMode.IsNotFinished));
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session));
          // В случае сбоя необходимо удалить временную директорию.
          ActionEngine.Instance.AddWorker(new ActionTempDirectoryControlWorker(session, ActionTempDirectoryControlWorkerMode.Delete));
          break;
        case "BeforeInstallFinalizeImmediate":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session));
          break;
        case "BeforeInstallFinalizeDeferred":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session));
          // После выполнения плагинов, необходимо удалить временную директорию.
          ActionEngine.Instance.AddWorker(new ActionTempDirectoryControlWorker(session, ActionTempDirectoryControlWorkerMode.Delete));
          // Пишем в отчет об окончании установки (самым последним).
          ActionEngine.Instance.AddWorker(new ActionInstallReportWorker(session, ActionInstallReportWorkerMode.IsFinished));
          break;
        case "BeforeInstallFinalizeRollback":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionMefControlWorker(session));
          break;
        case "RestoringDataBaseImmediate":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRestoringDatabaseWorker(session, factory, new string[] { "RestoringDataBaseDeferred", "RestoringDataBaseRollback" }));
          break;
        case "RestoringDataBaseDeferred":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRestoringDatabaseWorker(session, factory));
          break;
        case "RestoringDataBaseRollback":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRestoringDatabaseWorker(session, factory));
          break;
        case "RunSqlScriptImmediate":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptNewDbWorker(session, factory, new string[] { "RunSqlScriptDeferred", "RunSqlScriptRollback" }));
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptExistingDbWorker(session, factory, properties.RunSqlScriptExistingDb, new string[] { "RunSqlScriptDeferred", "RunSqlScriptRollback" }));
          break;
        case "RunSqlScriptDeferred":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptNewDbWorker(session, factory));
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptExistingDbWorker(session, factory));
          break;
        case "RunSqlScriptRollback":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptNewDbWorker(session, factory));
          ActionEngine.Instance.AddWorker(new ActionRunSqlScriptExistingDbWorker(session, factory));
          break;
        case "InstallingExtendedProceduresImmediate":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionInstallingExtendedProceduresWorker(session, new string[] { "InstallingExtendedProceduresDeferred", "InstallingExtendedProceduresRollback" }));
          break;
        case "InstallingExtendedProceduresDeferred":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionInstallingExtendedProceduresWorker(session));
          break;
        case "InstallingExtendedProceduresRollback":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionInstallingExtendedProceduresWorker(session));
          break;
      }

      return ActionEngine.Instance.Run();
    }

    #region AfterInstallInitialize.

    [CustomAction]
    public static ActionResult AfterInstallInitializeImmediate(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult AfterInstallInitializeDeferred(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult AfterInstallInitializeRollback(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    #endregion

    #region BeforeInstallFinalize.

    [CustomAction]
    public static ActionResult BeforeInstallFinalizeImmediate(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult BeforeInstallFinalizeDeferred(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult BeforeInstallFinalizeRollback(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    #endregion

    #region RestoringDataBase.

    /// <summary>
    /// Используется для получения информации о восстановлении базы данных.
    /// </summary>
    [CustomAction]
    public static ActionResult RestoringDataBaseImmediate(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Восстановление базы данных.
    /// </summary>
    [CustomAction]
    public static ActionResult RestoringDataBaseDeferred(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Откат восстановления базы данных.
    /// </summary>
    [CustomAction]
    public static ActionResult RestoringDataBaseRollback(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    #endregion

    #region RunSqlScript.

    [CustomAction]
    public static ActionResult RunSqlScriptImmediate(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult RunSqlScriptDeferred(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult RunSqlScriptRollback(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    #endregion

    #region InstallingExtendedProcedures.

    [CustomAction]
    public static ActionResult InstallingExtendedProceduresImmediate(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult InstallingExtendedProceduresDeferred(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult InstallingExtendedProceduresRollback(Session session)
    {
      return DispatcherExecute(session, MethodBase.GetCurrentMethod().Name);
    }

    #endregion
  }
}

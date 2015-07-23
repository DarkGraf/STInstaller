using System;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.ActionWorker
{
  /// <summary>
  /// Базовый класс для доступа к свойствам сессии.
  /// </summary>
  class SessionProperties
  {
    protected Session Session { get; private set; }

    public SessionProperties(Session session)
    {
      Session = session;
    }
  }

  class ActionUITypeSessionProperties : SessionProperties, IActionUITypeSessionProperties
  {
    string mode;

    public ActionUITypeSessionProperties(Session session, string mode) : base(session)
    {
      this.mode = mode;
    }

    #region IActionUITypeSessionProperties

    public string Mode
    {
      get { return Session[mode]; }
      set { Session[mode] = value; }
    }

    #endregion
  }

  class SelectDatabasesSessionProperties : SessionProperties, ISelectDatabasesSessionProperties
  {
    string version;
    string selectedDatabase;
    string selectedServer;
    string newDatabase;
    string existDatabase;

    public SelectDatabasesSessionProperties(Session session, string version, string selectedDatabase, string selectedServer, string newDatabase, string existDatabase)
      : base(session)
    {
      this.version = version;
      this.selectedDatabase = selectedDatabase;
      this.selectedServer = selectedServer;
      this.newDatabase = newDatabase;
      this.existDatabase = existDatabase;
    }

    #region ISelectDatabasesSessionProperties

    public string Version
    {
      get { return Session[version]; }
    }

    public string SelectedDatabase
    {
      get { return Session[selectedDatabase]; }
    }

    public string SelectedServer
    {
      get { return Session[selectedServer]; }
    }

    public string NewDatabase
    {
      get { return Session[newDatabase]; }
      set { Session[newDatabase] = value; }
    }

    public string ExistDatabase
    {
      get { return Session[existDatabase]; }
    }

    public string ExistControlProperty
    {
      get { return existDatabase; }
    }

    #endregion    
  }

  class DatabaseUIControlSessionProperties : SessionProperties, IDatabaseUIControlSessionProperties
  {
    string controlProperty;
    string newDatabaseIconName;
    string existDatabaseIconName;
    string lockDatabaseIconName;

    public DatabaseUIControlSessionProperties(Session session, string controlProperty, string newDatabaseIconName, string existDatabaseIconName, string lockDatabaseIconName)
      : base(session)
    {
      this.controlProperty = controlProperty;
      this.newDatabaseIconName = newDatabaseIconName;
      this.existDatabaseIconName = existDatabaseIconName;
      this.lockDatabaseIconName = lockDatabaseIconName;
    }

    #region IDatabaseUIControlSessionProperties

    public string ControlProperty
    {
      get { return controlProperty; }
    }

    public string NewDatabaseIconName
    {
      get { return newDatabaseIconName; }
    }

    public string ExistDatabaseIconName
    {
      get { return existDatabaseIconName; }
    }

    public string LockDatabaseIconName
    {
      get { return lockDatabaseIconName; }
    }

    #endregion
  }

  class ServerUIControlSessionProperties : SessionProperties, IServerUIControlSessionProperties
  {
    string controlProperty;

    public ServerUIControlSessionProperties(Session session, string controlProperty) : base(session)
    {
      this.controlProperty = controlProperty;
    }

    #region IServerUIControlSessionProperties

    public string ControlProperty
    {
      get { return controlProperty; }
    }

    #endregion
  }

  class CheckConnectionSessionProperties : SessionProperties, ICheckConnectionSessionProperties
  {
    string server;
    string database;
    string connectionSuccessful;
    string stringMessage;

    public CheckConnectionSessionProperties(Session session, string server, string database, string connectionSuccessful, string stringMessage)
      : base(session)
    {
      this.server = server;
      this.database = database;
      this.connectionSuccessful = connectionSuccessful;
      this.stringMessage = stringMessage;
    }

    #region ICheckConnectionSessionProperties

    public string Server
    {
      get { return Session[server]; }
    }

    public string Database
    {
      get { return Session[database]; }
    }

    public string ConnectionSuccessful
    {
      get { return Session[connectionSuccessful]; }
      set { Session[connectionSuccessful] = value; }
    }

    public string StringMessage
    {
      get { return Session[stringMessage]; }
      set { Session[stringMessage] = value; }
    }

    #endregion
  }

  class RunSqlScriptExistingDbSessionProperties : SessionProperties, IRunSqlScriptExistingDbSessionProperties
  {
    string backupPath;

    public RunSqlScriptExistingDbSessionProperties(Session session, string backupPath)
      : base(session)
    {
      this.backupPath = backupPath;
    }

    #region IRunSqlScriptExistingDbSessionProperties

    public string BackupPath
    {
      get { return Session[backupPath]; }
    }

    #endregion
  }

  class InitializationFinishInfoSessionProperties : SessionProperties, IInitializationFinishInfoSessionProperties
  {
    string controlProperty;
    string controlVisible;
    string backupDirectory;

    public InitializationFinishInfoSessionProperties(Session session, string controlProperty, string controlVisible, string backupDirectory)
      : base(session)
    {
      this.controlProperty = controlProperty;
      this.controlVisible = controlVisible;
      this.backupDirectory = backupDirectory;
    }

    public string ControlProperty
    {
      get { return controlProperty; }
    }

    public string InstallMode
    {
      get 
      {
        switch (Session["WixUI_InstallMode"])
        {
          case "InstallTypical":
            return "Обычная";
          case "InstallCustom":
            return "Выборочная";
          case "InstallComplete":
            return "Полная";
          default:
            return "Неизвестно";
        }
      }
    }

    public string BackupDirectory
    {
      get { return Session[backupDirectory]; }
    }


    public string ControlVisible
    {
      get { return Session[controlVisible]; }
      set { Session[controlVisible] = value; }
    }
  }

  class WidgetCreaterSessionProperties : SessionProperties, IWidgetCreaterSessionProperties
  {
    string installReportDialog;
    string installReportFileName;

    string finishInfoDialog;
    string finishInfoProperty;
    string finishInfoVisible;

    public WidgetCreaterSessionProperties(Session session, string installReportDialog, string installReportFileName,
      string finishInfoDialog, string finishInfoProperty, string finishInfoVisible)
      : base(session)
    {
      this.installReportDialog = installReportDialog;
      this.installReportFileName = installReportFileName;

      this.finishInfoDialog = finishInfoDialog;
      this.finishInfoProperty = finishInfoProperty;
      this.finishInfoVisible = finishInfoVisible;
    }

    public string InstallReportDialog
    {
      get { return installReportDialog; }
    }

    public string InstallReportFileName
    {
      get { return installReportFileName; }
    }

    public string VersionMsi 
    {
      get { return Session["VersionMsi"]; }
    }

    public string FinishInfoDialog
    {
      get { return finishInfoDialog; }
    }

    public string FinishInfoProperty
    {
      get { return finishInfoProperty; }
    }


    public string FinishInfoVisible
    {
      get { return finishInfoVisible; }
    }
  }
}

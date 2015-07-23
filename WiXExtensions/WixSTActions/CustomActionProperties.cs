using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.ActionWorker;
using WixSTActions.Utils;

namespace WixSTActions
{
  class CustomActionProperties
  {
    Session session;

    public CustomActionProperties(Session session)
    {
      this.session = session;
    }

    public IActionUITypeSessionProperties UIType
    {
      get
      {
        return new ActionUITypeSessionProperties(session, "UITYPEMODE");
      }
    }

    public ISelectDatabasesSessionProperties SelectDatabases
    {
      get
      {
        return new SelectDatabasesSessionProperties(session, "ProductVersion", "SELECTEDDATABASE", "SELECTEDSERVER", "NEWDATABASE", "EXISTDATABASE");
      }
    }

    public IDatabaseUIControlSessionProperties DatabaseUIControl
    {
      get
      {
        return new DatabaseUIControlSessionProperties(session, "SELECTEDDATABASE", "NewDatabase", "ExistDatabase", "LockDatabase");
      }
    }

    public IServerUIControlSessionProperties ServerUIControl
    {
      get
      {
        return new ServerUIControlSessionProperties(session, "SELECTEDSERVER");
      }
    }

    public ICheckConnectionSessionProperties CheckConnection
    {
      get
      {
        return new CheckConnectionSessionProperties(session, "SELECTEDSERVER", "NEWDATABASE", "CONNECTIONSUCCESSFUL", "STRINGMESSAGE");
      }
    }

    public IRunSqlScriptExistingDbSessionProperties RunSqlScriptExistingDb
    {
      get
      {
        return new RunSqlScriptExistingDbSessionProperties(session, "BACKUPPATH");
      }
    }

    public IInitializationFinishInfoSessionProperties InitializationFinishInfo
    {
      get
      {
        return new InitializationFinishInfoSessionProperties(session, "FINISHINFO", "FINISHINFOVISIBLE", "BACKUPPATH");
      }
    }

    public IWidgetCreaterSessionProperties WidgetCreater
    {
      get
      {
        string fileName = session.GetService<ISessionInstallReportExtension>().PathToFile;
        return new WidgetCreaterSessionProperties(session, "ExitDialog", fileName, "VerifyReadyDlg", "FINISHINFO", "FINISHINFOVISIBLE");
      }
    }
  }
}

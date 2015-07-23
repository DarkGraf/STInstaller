using System;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Mef;

namespace ProgressMessageDemo
{
  [InstallerPluginExport(CustomActionNames.AfterInstallInitializeImmediate)]
  public class ProgressMessage : IInstallerPlugin
  {
    public ActionResult DoAction(Session session)
    {
      Record record = new Record(3);
      for (int i = 1; i <= 100; i++)
      {
        record[1] = "ProgressMessageDemo";
        record[2] = string.Format("Выполнение Demo-плагина (выполнено {0}%)", i);
        record[3] = "";
        session.Message(InstallMessage.ActionStart, record);
        Thread.Sleep(20);
      }
      return ActionResult.Success;
    }
  }
}

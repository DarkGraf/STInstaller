using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  enum ActionInstallReportWorkerMode
  {
    /// <summary>
    /// Установка начата.
    /// </summary>
    Start,
    /// <summary>
    /// Установка успешно завершена.
    /// </summary>
    IsFinished,
    /// <summary>
    /// Установка не завершена.
    /// </summary>
    IsNotFinished
  }

  /// <summary>
  /// Добавляет сообщения в отчет установки.
  /// </summary>
  class ActionInstallReportWorker : ActionWorkerBase
  {
    ActionInstallReportWorkerMode mode;

    public ActionInstallReportWorker(Session session, ActionInstallReportWorkerMode mode)
      : base(session) 
    {
      this.mode = mode;
    }

    protected override UIType UITypeMode
    {
      get { return UIType.Server | UIType.Client; }
    }

    protected override bool IsAllowed
    {
      get { return true; }
    }

    protected override ActionResult DoWork()
    {
      string str = null;
      switch (mode)
      {
        case ActionInstallReportWorkerMode.Start:
          str = "Установка продукта начата";
          break;
        case ActionInstallReportWorkerMode.IsFinished:
          str = "Установка продукта закончена";
          break;
        case ActionInstallReportWorkerMode.IsNotFinished:
          str = "Установка продукта прервана";
          break;
      }

      if (str != null)
      {
        str = string.Format("{0} {1}", DateTime.Now, str);
        Session.GetService<ISessionInstallReportExtension>().AddInfo(str, InstallReportInfoType.Header1);
      }

      return ActionResult.Success;
    }
  }
}

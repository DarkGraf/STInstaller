using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  enum ActionTempDirectoryControlWorkerMode
  {
    Create,
    Delete
  }

  /// <summary>
  /// Управление временной директорией (создание (только в фазе Immediate) и удаление).
  /// </summary>
  class ActionTempDirectoryControlWorker : ActionWorkerBase
  {
    ActionTempDirectoryControlWorkerMode mode;

    public ActionTempDirectoryControlWorker(Session session, ActionTempDirectoryControlWorkerMode mode) : base(session)
    {
      this.mode = mode;
    }

    #region ActionWorkerBase

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
      switch (mode)
      {
        case ActionTempDirectoryControlWorkerMode.Create:
          if (Session.GetService<ISessionTempDirectoryExtension>().GetTempDirectory() == null)
            Session.GetService<ISessionTempDirectoryExtension>().CreateTempDirectory();
          break;
        case ActionTempDirectoryControlWorkerMode.Delete:
          if (Session.GetService<ISessionTempDirectoryExtension>().GetTempDirectory() != null)
            Session.GetService<ISessionTempDirectoryExtension>().DeleteTempDirectory();
          break;
      }

      return ActionResult.Success;
    }

    #endregion
  }
}

using System;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;
using WixSTActions.WixControl;

namespace WixSTActions.ActionWorker
{
  interface IServerUIControlSessionProperties
  {
    /// <summary>
    /// Список серверов.
    /// </summary>
    string ControlProperty { get; }
  }

  /// <summary>
  /// Инициализация элемента серверами.
  /// </summary>
  class ActionServerUIControlWorker : ActionWorkerBase
  {
    IServerUIControlSessionProperties sessionProp;

    public ActionServerUIControlWorker(Session session, IServerUIControlSessionProperties sessionProp) : base(session) 
    {
      this.sessionProp = sessionProp;
    }

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override bool IsAllowed
    {
      get { return true; }
    }

    protected override ActionResult DoWork()
    {
      // Заполняем ComboBox серверами.
      WixComboItem[] servers = (from s in Session.GetService<ISessionServerInfoExtension>().GetServerInfos()
                                select new WixComboItem(s.Name, s.Name)).ToArray();
      WixComboBox combo = new WixComboBox(Session, sessionProp.ControlProperty);
      combo.ClearItems();
      if (servers.Length > 0)
      {
        combo.AddItems(servers);
        combo.SelectedValue = servers[0].Value;
      }
      else
        combo.SelectedValue = "";

      return ActionResult.Success;
    }
  }
}

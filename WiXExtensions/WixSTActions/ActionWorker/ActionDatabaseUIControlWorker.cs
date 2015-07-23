using System;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;
using WixSTActions.WixControl;

namespace WixSTActions.ActionWorker
{
  interface IDatabaseUIControlSessionProperties
  {
    /// <summary>
    /// Свойство списка баз данных.
    /// </summary>
    string ControlProperty { get; }

    string NewDatabaseIconName { get; }
    string ExistDatabaseIconName { get; }
    string LockDatabaseIconName { get; }
  }

  /// <summary>
  /// Инициализация элемента баз данных.
  /// </summary>
  class ActionDatabaseUIControlWorker : ActionWorkerBase
  {
    IDatabaseUIControlSessionProperties sessionProp;

    public ActionDatabaseUIControlWorker(Session session, IDatabaseUIControlSessionProperties sessionProp) : base(session) 
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
      // Заполняем ListView базами данных.
      WixListItem[] databases = (from d in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
                                let name = NameViewConverter.GetNameView(d.Server, d.Name)
                                select new WixListItem(name + " " + d.Version, name, d.IsNew ? sessionProp.NewDatabaseIconName : 
                                  (d.IsRequiringUpdate ? sessionProp.ExistDatabaseIconName : sessionProp.LockDatabaseIconName))).ToArray();
                              
      WixListView view = new WixControl.WixListView(Session, sessionProp.ControlProperty);
      view.ClearItems();
      if (databases.Length > 0)
      {
        view.AddItems(databases);
        view.SelectedValue = databases[0].Value;
      }
      else
        view.SelectedValue = "";
      
      return ActionResult.Success;
    }
  }
}

using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  interface IWidgetCreaterSessionProperties
  {
    string InstallReportDialog { get; }
    string InstallReportFileName { get; }
    string VersionMsi { get; }
    string FinishInfoDialog { get; }
    string FinishInfoProperty { get; }
    string FinishInfoVisible { get; }
  }

  /// <summary>
  /// Динамическое создание элементов управления на существующих формах.
  /// </summary>
  class ActionWidgetCreaterWorker : ActionWorkerBase
  {
    IWidgetCreaterSessionProperties properties;

    public ActionWidgetCreaterWorker(Session session, IWidgetCreaterSessionProperties properties)
      : base(session)
    {
      this.properties = properties;
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
      // Вывод ссылки на файл отчета.
      // Элемент управления Hyperlink появился только в 5 версии.
      if (properties.VersionMsi.CompareTo("5") >= 0)
      {
        string text = string.Format(@"<a href=""file:///{0}"">Отчет установки</a>", properties.InstallReportFileName);

        // Название виджета любое.
        WixWidget.WixHyperlinkWidget link = new WixWidget.WixHyperlinkWidget(Session, properties.InstallReportDialog, "ErrorInfoHyperlink", text);
        link.X = 135;
        link.Y = 110;
        link.Width = 100;
        link.Height = 20;
        link.Transparent = true;
      }

      // Вывод итоговой информации об установке.
      // Название виджета любое.
      WixWidget.WixListBoxWidget list = new WixWidget.WixListBoxWidget(Session, properties.FinishInfoDialog, "FinishInfoComboBox", properties.FinishInfoProperty);
      list.X = 25;
      list.Y = 110;
      list.Width = 320;
      list.Height = 113;
      list.Sorted = true;
      list.AddCondition(WixWidget.WixWidgetContditionType.Show, string.Format("{0}", properties.FinishInfoVisible));
      list.AddCondition(WixWidget.WixWidgetContditionType.Hide, string.Format("NOT {0}", properties.FinishInfoVisible));

      return ActionResult.Success;
    }
  }
}

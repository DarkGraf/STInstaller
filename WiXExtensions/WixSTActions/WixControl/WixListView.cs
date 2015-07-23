using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixControl
{
  class WixListItem
  {
    public WixListItem(string text, string value, string icon)
    {
      Text = text;
      Value = value;
      Icon = icon;
    }

    public string Text { get; private set; }
    public string Value { get; private set; }
    public string Icon { get; private set; }
  }

  class WixListView : WixContainerControl<WixListItem>
  {
    public WixListView(Session session, string property) : base(session, property) { }

    /// <summary>
    /// Добавляет один элемент в открытое представление.
    /// </summary>
    protected override void InsertRecord(View view, WixListItem item, int order)
    {
      Record record = Session.Database.CreateRecord(5);
      record.SetString(1, Property);
      record.SetInteger(2, order);
      record.SetString(3, item.Value);
      record.SetString(4, item.Text);
      record.SetString(5, item.Icon);
      view.Modify(ViewModifyMode.InsertTemporary, record);
    }

    protected override string SelectQuery
    {
      get { return "SELECT * FROM ListView WHERE ListView.Property = '{0}'"; }
    }

    protected override string DeleteQuery
    {
      get { return "DELETE FROM ListView WHERE ListView.Property = '{0}' AND ListView.Value = '{1}'"; }
    }

    protected override string ClearQuery
    {
      get { return "DELETE FROM ListView WHERE ListView.Property = '{0}'"; }
    }

    protected override WixListItem CreateItem(Record record)
    {
      return new WixListItem(record.GetString(4), record.GetString(3), record.GetString(5));
    }
  }
}

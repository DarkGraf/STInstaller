using Microsoft.Deployment.WindowsInstaller;
using System;

namespace WixSTActions.WixControl
{
  class WixListBoxItem
  {
    public WixListBoxItem(string text, string value)
    {
      Text = text;
      Value = value;
    }

    public string Text { get; private set; }
    public string Value { get; private set; }
  }

  class WixListBox : WixContainerControl<WixListBoxItem>
  {
    public WixListBox(Session session, string property) : base(session, property) { }

    protected override string SelectQuery
    {
      get { return "SELECT * FROM ListBox WHERE ListBox.Property = '{0}'"; }
    }

    protected override string DeleteQuery
    {
      get { return "DELETE FROM ListBox WHERE ListBox.Property = '{0}' AND ListBox.Value = '{1}'"; }
    }

    protected override string ClearQuery
    {
      get { return "DELETE FROM ListBox WHERE ListBox.Property = '{0}'"; }
    }

    protected override void InsertRecord(View view, WixListBoxItem item, int order)
    {
      Record record = Session.Database.CreateRecord(4);
      record.SetString(1, Property);
      record.SetInteger(2, order);
      record.SetString(3, item.Value);
      record.SetString(4, item.Text);
      view.Modify(ViewModifyMode.InsertTemporary, record);
    }

    protected override WixListBoxItem CreateItem(Record record)
    {
      return new WixListBoxItem(record.GetString(4), record.GetString(3));
    }
  }
}

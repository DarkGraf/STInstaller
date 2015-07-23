using System;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixControl
{
  class WixComboItem
  {
    public WixComboItem(string text, string value)
    {
      Text = text;
      Value = value;
    }

    public string Text { get; private set; }
    public string Value { get; private set; }
  }

  class WixComboBox : WixContainerControl<WixComboItem>
  {
    public WixComboBox(Session session, string property) : base(session, property) { }

    protected override void InsertRecord(View view, WixComboItem item, int order)
    {
      Record record = Session.Database.CreateRecord(4);
      record.SetString(1, Property);
      record.SetInteger(2, order);
      record.SetString(3, item.Value);
      record.SetString(4, item.Text);
      view.Modify(ViewModifyMode.InsertTemporary, record);
    }

    protected override string SelectQuery
    {
      get { return "SELECT * FROM ComboBox WHERE ComboBox.Property = '{0}'"; }
    }

    protected override string DeleteQuery
    {
      get { return "DELETE FROM ComboBox WHERE ComboBox.Property = '{0}' AND ComboBox.Value = '{1}'"; }
    }

    protected override string ClearQuery
    {
      get { return "DELETE FROM ComboBox WHERE ComboBox.Property = '{0}'"; }
    }

    protected override WixComboItem CreateItem(Record record)
    {
      return new WixComboItem(record.GetString(4), record.GetString(3));
    }
  }
}

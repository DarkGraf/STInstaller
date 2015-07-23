using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.WixControl;

namespace WixSTActions.WixWidget
{
  // Если понадобиться создать WixComboBoxWidget, то сделать общий базовый класс с данным классом.
  class WixListBoxWidget : WixWidget<WixListBox>
  {
    const uint attrSorted = 0x10000;

    public WixListBoxWidget(Session session, string dialog, string name, string property)
      : base(session, dialog, name, "ListBox")
    {
      Property = property;
    }

    public bool Sorted
    {
      get { return GetAttributes(attrSorted); }
      set { SetAttributes(attrSorted, value); }
    }

    public override WixListBox Control
    {
      get { throw new NotImplementedException(); }
    }
  }
}

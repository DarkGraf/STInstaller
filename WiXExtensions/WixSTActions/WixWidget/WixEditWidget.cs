using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.WixControl;

namespace WixSTActions.WixWidget
{
  class WixEditWidget : WixWidget<WixEdit>
  {
    const uint attrMultiLine = 0x10000;

    WixEdit control;

    public WixEditWidget(Session session, string dialog, string name, string property) 
      : base(session, dialog, name, "Edit") 
    {
      Property = property;
      control = new WixEdit(Session, property);
    }

    public bool MultiLine
    {
      get { return GetAttributes(attrMultiLine); }
      set { SetAttributes(attrMultiLine, value); }
    }

    public override WixEdit Control { get { return control; } }
  }
}

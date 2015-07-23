using System;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.WixWidget
{
  // Появился только в 5 версии инсталлятора.
  class WixHyperlinkWidget : WixWidget<WixControl.WixControl>
  {
    const uint attrTransparent = 0x10000;

    public WixHyperlinkWidget(Session session, string dialog, string name, string text)
      : base(session, dialog, name, "Hyperlink") 
    {
      Text = text;
    }

    public bool Transparent
    {
      get { return GetAttributes(attrTransparent); }
      set { SetAttributes(attrTransparent, value); }
    }

    public override WixControl.WixControl Control
    {
      get { throw new NotImplementedException(); }
    }
  }
}

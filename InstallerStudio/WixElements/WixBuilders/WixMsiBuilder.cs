using System;

namespace InstallerStudio.WixElements.WixBuilders
{
  class WixMsiBuilder : WixBuilderBase
  {
    private readonly WixProduct product;

    public WixMsiBuilder(WixProduct product)
    {
      this.product = product;
    }

    #region WixBuilderBase

    protected override string[] GetTemplateFileNames()
    {
      return new string[]
      {
        "MsiTemplate/Components.wxs",
        "MsiTemplate/Directories.wxs",
        "MsiTemplate/Features.wxs",
        "MsiTemplate/Product.wxs",
        "MsiTemplate/Variables.wxi"
      };
    }

    #endregion
  }
}

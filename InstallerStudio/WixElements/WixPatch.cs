using System;
using System.Runtime.Serialization;

using InstallerStudio.WixElements.WixBuilders;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  class WixPatch : WixMainEntity
  {
    public WixPatch()
    {
      RootElement = new WixPatchFamilyElement();
    }

    #region WixMainEntity

    public override IWixElement RootElement { get; protected set; }

    protected override WixBuilderBase CreateBuilder()
    {
      return new WixMspBuilder();
    }

    #endregion
  }
}

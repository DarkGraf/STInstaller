using System;
using System.Runtime.Serialization;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  class WixPatch : IWixMainEntity
  {
    public WixPatch()
    {
      RootElement = new WixPatchFamilyElement();
    }

    public IWixElement RootElement { get; private set; }
  }
}

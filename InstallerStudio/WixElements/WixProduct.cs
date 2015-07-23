using System;
using System.Runtime.Serialization;

using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  [KnownType(typeof(WixFeatureElement))]
  [KnownType(typeof(WixComponentElement))]
  class WixProduct : IWixMainEntity
  {
    #region Основные свойства.

    [DataMember]
    public Guid Id { get; set; }

    [DataMember]
    public Guid UpgradeCode { get; set; }

    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string Manufacturer { get; set; }

    [DataMember]
    public AppVersion Version { get; set; }

    #endregion

    public WixProduct()
    {
      Id = Guid.NewGuid();
      UpgradeCode = Guid.NewGuid();
      Name = string.Empty;
      Manufacturer = string.Empty;
      Version = new AppVersion();

      RootElement = new WixFeatureElement();
    }

    [DataMember]
    public IWixElement RootElement { get; private set; }
  }
}

using System;
using System.Runtime.Serialization;

using InstallerStudio.Utils;
using InstallerStudio.WixElements.WixBuilders;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  [KnownType(typeof(WixFeatureElement))]
  [KnownType(typeof(WixComponentElement))]
  [KnownType(typeof(WixFileElement))]
  class WixProduct : WixMainEntity
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

    #region WixMainEntity

    [DataMember]
    public override IWixElement RootElement { get; protected set; }

    protected override WixBuilderBase CreateBuilder()
    {
      return new WixMsiBuilder(this);
    }

    #endregion
  }
}

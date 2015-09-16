using System;
using System.Runtime.Serialization;

using InstallerStudio.WixElements.WixBuilders;
using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  [KnownType(typeof(WixFeatureElement))]
  [KnownType(typeof(WixComponentElement))]
  [KnownType(typeof(WixFileElement))]
  [KnownType(typeof(WixDbComponentElement))]
  [KnownType(typeof(WixShortcutElement))]
  [KnownType(typeof(WixSqlScriptElement))]
  [KnownType(typeof(WixSqlExtentedProceduresElement))]
  [KnownType(typeof(WixMefPluginElement))]
  class WixProduct : WixMainEntity
  {
    #region Основные свойства.
#warning При изменении каждого свойства нужно сделать уведомление.
    [DataMember]
    public Guid Id { get; set; }

    [DataMember]
    public Guid UpgradeCode { get; set; }

    [DataMember]
    [CheckingRequired]
    public string Name { get; set; }

    [DataMember]
    [CheckingRequired]
    public string Manufacturer { get; set; }

    [DataMember]
    public AppVersion Version { get; set; }

    [DataMember]
    [CheckingRequired]
    public string PackageDescription { get; set; }

    [DataMember]
    [CheckingRequired]
    public string PackageComments { get; set; }

    [DataMember]
    [CheckingRequired]
    public string InstallLocationFamilyFolder { get; set; }

    [DataMember]
    [CheckingRequired]
    public string InstallLocationProductFolder { get; set; }

    [DataMember]
    public Guid ProgramMenuFamilyDirComponentGuid { get; set; }

    [DataMember]
    public Guid ProgramMenuProductDirComponentGuid { get; set; }

    [DataMember]
    public Guid ProgramMenuReinstallComponentGuid { get; set; }

    #endregion

    public WixProduct()
    {
      Id = Guid.NewGuid();
      UpgradeCode = Guid.NewGuid();
      Name = string.Empty;
      Manufacturer = string.Empty;
      Version = new AppVersion();
      PackageDescription = string.Empty;
      PackageComments = string.Empty;
      InstallLocationFamilyFolder = string.Empty;
      InstallLocationProductFolder = string.Empty;

      // Уникальные идентификаторы для компонент деинсталляции директорий
      // семейства продукта, самого продукта и ярлыка переустановки из меню пуск.
      // Должны быть одинаковые в пределах продукта основной версии.
      // В GUI не показываем, для пользователя не нужны.
      ProgramMenuFamilyDirComponentGuid = Guid.NewGuid();
      ProgramMenuProductDirComponentGuid = Guid.NewGuid();
      ProgramMenuReinstallComponentGuid = Guid.NewGuid();

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

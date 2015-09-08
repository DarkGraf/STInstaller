using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using InstallerStudio.Models;
using InstallerStudio.WixElements.WixBuilders;

namespace InstallerStudio.WixElements
{
  /* ****************************************************************
   * WixPatchProduct // Аналог в Wix отсутствует. MainItem.         *
   *  └► WixPatchRootElement // Аналог в Wix отсутствует. RootItem. *
   *      └► WixPatchElement                                        *
   *          └► WixPatchComponentElement                           *
   ******************************************************************/

  [DataContract(Namespace = StringResources.Namespace)]
  [KnownType(typeof(WixPatchRootElement))]
  [KnownType(typeof(WixPatchElement))]
  [KnownType(typeof(WixPatchComponentElement))]
  [KnownType(typeof(WixProductUpdateInfo))]
  class WixPatchProduct : WixMainEntity
  {
    public WixPatchProduct()
    {
      RootElement = new WixPatchRootElement();
      UpdateComponents = new List<UpdateComponentInfo>();
    }

    /// <summary>
    /// Обновлемые компоненты.
    /// </summary>
    [DataMember]
    public IList<UpdateComponentInfo> UpdateComponents { get; private set; }

    #region Свойства базового и целевого пакета.

    [DataMember]
    public Guid BaseId { get; set; }
    [DataMember]
    public string BaseName { get; set; }
    [DataMember]
    public string BaseManufacturer { get; set; }
    [DataMember]
    public AppVersion BaseVersion { get; set; }
    [DataMember]
    public string BasePath { get; set; }

    [DataMember]
    public Guid TargetId { get; set; }
    [DataMember]
    public string TargetName { get; set; }
    [DataMember]
    public string TargetManufacturer { get; set; }
    [DataMember]
    public AppVersion TargetVersion { get; set; }
    [DataMember]
    public string TargetPath { get; set; }

    #endregion

    #region WixMainEntity

    public override IWixElement RootElement { get; protected set; }

    protected override WixBuilderBase CreateBuilder()
    {
      return new WixMspBuilder(this);
    }

    #endregion
  }
}

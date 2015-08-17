using System;
using System.Runtime.Serialization;

using InstallerStudio.Models;
using InstallerStudio.WixElements.WixBuilders;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Описывает интерфейс самой главной сущности Wix.
  /// </summary>
  interface IWixMainEntity
  {
    /// <summary>
    /// Корневой элемент сущности Wix.
    /// </summary>
    IWixElement RootElement { get; }

    /// <summary>
    /// Построение целевого объекта.
    /// </summary>
    void Build();
  }

  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixMainEntity : IWixMainEntity
  {
    protected abstract WixBuilderBase CreateBuilder();

    #region IWixMainEntity

    [DataMember]
    public abstract IWixElement RootElement { get; protected set; }

    public void Build()
    {
      using (WixBuilderBase builder = CreateBuilder())
      {
        builder.Build();
      }
    }

    #endregion
  }
}

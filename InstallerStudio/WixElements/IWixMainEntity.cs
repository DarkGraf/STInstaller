using System;

using InstallerStudio.Models;

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
  }
}

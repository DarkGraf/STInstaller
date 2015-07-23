using System;
using System.Collections.Generic;

using InstallerStudio.Views.Utils;

namespace InstallerStudio.WixElements
{
  public interface IWixElement
  {
    /// <summary>
    /// Идентификатор элемента.
    /// </summary>
    string Id { get; set; }
    /// <summary>
    /// Краткое название типа элемента.
    /// </summary>
    string ShortTypeName { get; }
    /// <summary>
    /// Тип изображения.
    /// </summary>
    ElementsImagesTypes ImageType { get; }
    /// <summary>
    /// Дочерние элементы.
    /// </summary>
    IList<IWixElement> Items { get; }
    /// <summary>
    /// Проверка на доступность элемента.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    bool AvailableForRun(Type type, IWixElement rootItem);
    /// <summary>
    /// Признак зафиксированного элемента.
    /// У зафиксированного элемента нельзя изменять свойства.
    /// </summary>
    bool IsFrozen { get; }
    /// <summary>
    /// Признак предопределенного элемента.
    /// Является служебным элементом.
    /// </summary>
    bool IsPredefined { get;  }
    /// <summary>
    /// Только для чтения.
    /// Является комбинацией IsFrozen || IsPredefined.
    /// </summary>
    bool IsReadOnly { get; }
  }
}

using System;

namespace InstallerStudio.WixElements
{
  class StringResources
  {
    public const string Namespace = "http://systemt.ru/2015/InstallerStudio";

    public const string WixElementBaseIdDescription = "Идентификатор элемента. Должен быть уникальным в пределах пакета.";
    public const string WixElementBaseIsFrozenDescription = "Признак зафиксированного элемента. У зафиксированного элемента нельзя изменять свойства.";
    public const string WixElementBaseIsPredefinedDescription = "Признак предопределенного элемента. Является служебным элементом.";

    public const string CategoryMain = "Основное";
    public const string CategoryAuxiliary = "Служебное";
  }
}

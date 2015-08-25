using System;

namespace InstallerStudio.WixElements
{
  class StringResources
  {
    public const string Namespace = "http://systemt.ru/2015/InstallerStudio";

    public const string WixElementBaseIdDescription = "Идентификатор элемента. Должен быть уникальным в пределах пакета.";
    public const string WixElementBaseIsFrozenDescription = "Признак зафиксированного элемента. У зафиксированного элемента нельзя изменять свойства.";
    public const string WixElementBaseIsPredefinedDescription = "Признак предопределенного элемента. Является служебным элементом.";

    public const string WixFeatureElementTitleDescription = "Заголовок набора, отображается в соответствующем диалоговом окне.";
    public const string WixFeatureElementDescriptionDescription = "Описание набора, отображается в соответствующем диалоговом окне.";
    public const string WixFeatureElementDisplayDescription = "Управляет отображением набора в соответствующем диалоге. Collapse отображает содержимое набора свернутым, Expand – развернутым, а Hidden скрывает набор. Необязательное поле (Collapse по умолчанию).";

    public const string WixShortcutElementNameDescription = "Отображаемое имя.";
    public const string WixShortcutElementDescriptionDescription = "Описание, отображаемое во всплывающей подсказке при наведении курсора мыши. Необязятельное поле.";
    public const string WixShortcutElementDirectoryDescription = "Идентификатор каталога, где будет располагаться ярлык.";
    public const string WixShortcutElementIconDescription = "Пиктограмма для ярлыка. Необязятельное поле (в этом случае для exe-файлов будет использоваться их значок, для других - значок по умолчанию).";
    public const string WixShortcutElementArgumentsDescription = "Параметры командной строки для ярлыка. Необязятельное поле.";

    public const string CategoryMain = "Основное";
    public const string CategoryAuxiliary = "Служебное";
    public const string CategoryFiles = "Файлы и директории";
    public const string CategoryMiscellaneous = "Разное";
  }
}

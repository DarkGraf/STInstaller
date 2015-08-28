using System;

namespace InstallerStudio.WixElements
{
  class StringResources
  {
    public const string Namespace = "http://systemt.ru/2015/InstallerStudio";

    public const string WixElementBaseIdDescription = "Идентификатор элемента. Должен быть уникальным в пределах пакета.";
    public const string WixElementBaseIsFrozenDescription = "Признак зафиксированного элемента. У зафиксированного элемента нельзя изменять свойства.";
    public const string WixElementBaseIsPredefinedDescription = "Признак предопределенного элемента. Является служебным элементом.";

    public const string WixFeatureElementTitleDescription = "Заголовок набора, отображается в соответствующем диалоговом окне. Необязятельное поле.";
    public const string WixFeatureElementDescriptionDescription = "Описание набора, отображается в соответствующем диалоговом окне. Необязятельное поле.";
    public const string WixFeatureElementDisplayDescription = "Управляет отображением набора в соответствующем диалоге. Collapse отображает содержимое набора свернутым, Expand – развернутым, а Hidden скрывает набор. Необязательное поле (Collapse по умолчанию).";
    public const string WixFeatureElementAbsentDescription = "Принимает два значения: Allow и Disallow. В первом случае пользователь получит возможность не устанавливать набор совсем; во втором – набор будет устанавливаться в любом случае – так следует помечать необходимые для работы компоненты. Необязательное поле (Allow по умолчанию).";

    public const string WixShortcutElementNameDescription = "Отображаемое имя.";
    public const string WixShortcutElementDescriptionDescription = "Описание, отображаемое во всплывающей подсказке при наведении курсора мыши. Необязятельное поле.";
    public const string WixShortcutElementDirectoryDescription = "Идентификатор каталога, где будет располагаться ярлык.";
    public const string WixShortcutElementIconDescription = "Пиктограмма для ярлыка. Необязятельное поле (в этом случае для exe-файлов будет использоваться их значок, для других - значок по умолчанию).";
    public const string WixShortcutElementArgumentsDescription = "Параметры командной строки для ярлыка. Необязятельное поле.";

    public const string WixDbComponentElementMdfFileDescription = "Файл данных базы данных.";
    public const string WixDbComponentElementLdfFileDescription = "Журнал базы данных.";

    public const string WixSqlScriptElementScriptDescription = "Файл SQL-скрипта.";
    public const string WixSqlScriptElementSequenceDescription = "Последовательность выполнения скриптов. Скрипты с меньшим значением Sequence выполняются первыми. При одинаковых значениях Sequence последовательность выполнения не определена.";
    public const string WixSqlScriptElementExecuteOnInstallDescription = "Выполнение скрипта во время установки. Должен быть выбран хотя бы один режим.";
    public const string WixSqlScriptElementExecuteOnReinstallDescription = "Выполнение скрипта во время переустановки. Должен быть выбран хотя бы один режим.";
    public const string WixSqlScriptElementExecuteOnUninstallDescription = "Выполнения скрипта во время удаления. Должен быть выбран хотя бы один режим.";

    public const string WixSqlExtentedProceduresElementFileNameDescription = "Файл содержащий хранимые процедуры или необходимый для их работы (настройки и прочее).";

    public const string WixMefPluginElementFileNameDescription = "Файл содержащий расширение для инсталлятора созданное по технологии Managed Extensibility Framework.";

    public const string CategoryMain = "Основное";
    public const string CategoryAuxiliary = "Служебное";
    public const string CategoryFiles = "Файлы и директории";
    public const string CategoryMiscellaneous = "Разное";
    public const string CategoryDBFiles = "Файлы базы данных";
    public const string CategoryRunModes = "Режимы выполнения";
  }
}

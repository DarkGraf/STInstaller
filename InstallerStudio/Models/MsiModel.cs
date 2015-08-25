using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using InstallerStudio.WixElements;
using InstallerStudio.ViewModels;

namespace InstallerStudio.Models
{
  class MsiModel : BuilderModel, IMsiModelAdditional
  {
    #region Статические члены.

    /// <summary>
    /// Предопределенные установочные директории.
    /// </summary>
    internal static IList<string> PredefinedInstallDirectories = new List<string>
    {
      "[ProductFolder]", // Каталог для конкретного продукта.
      "[ProgramFilesFolder]", // Program Files.
      "[INSTALLLOCATION]", // Общий каталог для семейства продуктов.
      "[ProgramMenuFolder]", // Пуск.
      "[ProgramMenuFamilyDir]", // Меню общее для семейства продуктов.
      "[ProgramMenuProductDir]", // Меню для конкретного продукта.
      "[DesktopFolder]", // Рабочий стол.
      "[StartMenuFolder]",
      "[StartupFolder]",
      "[WindowsFolder]"
    };

    /// <summary>
    /// Приводит предопределенную директорию к формату для использования в
    /// файлах формирования msi. При передачи не предопределенной директории,
    /// просто возвращает ее.
    /// </summary>
    internal static string FormatInstallDirectory(string directory)
    {
      if (PredefinedInstallDirectories.Contains(directory))
        return directory.Substring(1, directory.Length - 2);
      else
        return directory;
    }

    #endregion

    public MsiModel()
    {
      // Создаем установочные директории для сессии и добавляем 
      // туда предопределенные установочные директории.
      InstallDirectories = new ObservableCollection<string>(PredefinedInstallDirectories);
    }

    #region BuilderModel

    protected override IWixMainEntity CreateMainEntity()
    {
      WixProduct product = new WixProduct();

      // Создаем предопределенные элементы. Это общие элементы для 
      // всех инсталляторов (для серверной и клиентской частей).
      // При построении msi данные секции уже должны быть в файлах wxs с
      // заполненными атрибутами.

      // Корневой элемент с типом WixFeatureElement создаст сам WixProduct.
      // Заполняем свойствами корневую Feature.
      product.RootElement.Id = "RootFeature";

      WixFeatureElement commonFeature = new WixFeatureElement();
      commonFeature.Id = "CommonFeature";
      commonFeature.Predefinition();
      product.RootElement.Items.Add(commonFeature);

      WixComponentElement component;

      component = new WixComponentElement();
      component.Id = "ProgramMenuFamilyDirComponent";
      component.Predefinition();
      commonFeature.Items.Add(component);

      component = new WixComponentElement();
      component.Id = "ProgramMenuProductDirComponent";
      component.Predefinition();
      commonFeature.Items.Add(component);

      component = new WixComponentElement();
      component.Id = "ReinstallComponent";
      component.Predefinition();
      commonFeature.Items.Add(component);

      return product;
    }

    public override CommandMetadata[] GetElementCommands()
    {
      return new CommandMetadata[]
      {
        new CommandMetadata("Общие", typeof(WixFeatureElement)),
        new CommandMetadata("Общие", typeof(WixComponentElement)),
        new CommandMetadata("Общие", typeof(WixMefPluginElement)),

        new CommandMetadata("Файлы", typeof(WixFileElement)),
        new CommandMetadata("Файлы", typeof(WixShortcutElement)),

        new CommandMetadata("База данных", typeof(WixDbComponentElement)),
        new CommandMetadata("База данных", typeof(WixSqlScriptElement)),
        new CommandMetadata("База данных", typeof(WixSqlExtentedProceduresElement))
      };
    }

    #endregion

    #region IMsiModelAdditional

    /// <summary>
    /// Содержит установочные директории для выбора пользователем.
    /// </summary>
    public ObservableCollection<string> InstallDirectories { get; private set; }

    public bool CheckInstallDirectoryForDeleting(string directory)
    {
      // Возвращаем истину, если удаление возможно.
      // Если директория не является предопределенной и если ни один 
      // элемент не использует данную директорию, то разрешаем удаление.

      // Возможно в будущем нужно будет разрешать удалять в том случае, если
      // директория есть у одного элемента и этот элемент редактируется
      // в текущий момент, то можно воспользоваться свойством SelectedItem.
      return !PredefinedInstallDirectories.Contains(directory)
        && !RootItem.Items.Descendants().OfType<IFileSupport>().SelectMany(v => v.GetInstallDirectories()).Contains(directory);
    }

    #endregion
  }

  class MsiModelFactory : BuilderModelFactory
  {
    public override BuilderModel Create()
    {
      return new MsiModel();
    }
  }
}
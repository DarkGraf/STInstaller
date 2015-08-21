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
    /// <summary>
    /// Предопределенные установочные директории.
    /// </summary>
    internal static IList<string> PredefinedInstallDirectories = new List<string>
    {
      "[ProductFolder]",
      "[SystemFolder]"
    };

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
      // Несут информационную нагрузку, в формированиии результирующего
      // файла *.wxs в данный момент не участвуют.

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

        new CommandMetadata("Файлы", typeof(WixFileElement)),
        new CommandMetadata("Файлы", typeof(WixDesktopShortcutElement)),
        new CommandMetadata("Файлы", typeof(WixStartMenuShortcutElement)),

        new CommandMetadata("База данных", typeof(WixDbComponentElement)),
        new CommandMetadata("База данных", typeof(WixSqlScriptElement))
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
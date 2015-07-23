using System;

using InstallerStudio.WixElements;
using InstallerStudio.ViewModels;
using InstallerStudio.Views.Utils;

namespace InstallerStudio.Models
{
  class MsiModel : BuilderModel
  {
    #region BuilderModel

    protected override IWixMainEntity CreateMainEntity()
    {
      WixProduct product = new WixProduct();

      // Заполняем свойствами корневую Feature.
      product.RootElement.Id = "RootFeature";

      // Создаем предопределенные элементы. Это общие элементы для 
      // всех инсталляторов (для серверной и клиентской частей).
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
        new CommandMetadata("База данных", typeof(WixDbTemplateElement)),
        new CommandMetadata("База данных", typeof(WixSqlScriptElement))
      };
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
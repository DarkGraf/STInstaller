using System;
using System.Linq;
using System.Windows.Input;

using InstallerStudio.Models;
using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;
using InstallerStudio.WixElements;
using InstallerStudio.Views.Controls;

namespace InstallerStudio.ViewModels
{
  class MspViewModel : BuilderViewModel
  {
#warning Сделать скрытие панели элементов.
    public MspViewModel(IRibbonManager ribbonManager) : base(ribbonManager) { }

    /// <summary>
    /// Загрузка в модель с "нуля".
    /// </summary>
    /// <param name="parameters"></param>
    public void Load(IMspModelLoadingParamters parameters)
    {
      ((MspModel)Model).Load(parameters);
      LoadUpdateComponents();
    }

    /// <summary>
    /// Загрузка модели из файла.
    /// </summary>
    /// <param name="fileName"></param>
    public override void Load(string fileName)
    {
      base.Load(fileName);
      LoadUpdateComponents();
    }

    /// <summary>
    /// Загрузка обновленных компонент.
    /// </summary>
    private void LoadUpdateComponents()
    {
      IRibbonGroup group = RibbonManager.Categories[1].Pages[0].Add("Обновленные компоненты");
      foreach (UpdateComponentInfo info in ((MspModel)Model).UpdateComponents.OrderBy(v => v.Id))
      {
        // Создаем WPF команду и привязываем ее к кнопке.
        // Команда доступна, если выбранный элемент Patch и в нем
        // нет такого же элемента (сравнение по идентификатору).
        ICommand relayCommand = new RelayCommand(
          (param) =>
          {
            Model.AddItem(typeof(WixPatchComponentElement));
            // Меняем у добавленного элемента Id.
            ((WixPatchComponentElement)Model.SelectedItem).Id = info.Id;
          },
          delegate
          {
            bool result = false;
            WixPatchElement element = (SelectedItem ?? Model.RootItem) as WixPatchElement;
            if (element != null)
            {
              result = !element.Items.Select(v => v.Id).Contains(info.Id);
            }
            return result;
          });
        group.Add(info.Id, relayCommand, RibbonButtonType.Small).SetImage(ElementsImagesTypes.Component);
      }
    }

    /// <summary>
    /// Для вывода списка установочных директорий в PropertyGrid.
    /// В данный момент не используется, но нужна так как есть на нее ссылка из WixPropertyGridControl.xaml.
    /// </summary>
    public IWixPropertyGridControlDataSource WixPropertyGridControlDataSource { get; private set; }

    #region BuilderViewModel

    protected override BuilderModelFactory BuilderModelFactory
    {
      get { return new MspModelFactory(); }
    }

    public override FileDescription FileDescription
    {
      get 
      {
        return new FileDescription
        {
          FileExtension = "mspzip",
          Description = "Msp Zip"
        };
      }
    }

    #endregion
  }

  class MspViewModelFactory : BuilderViewModelFactory
  {
    public override BuilderViewModel Create(IRibbonManager ribbonManager)
    {
      return new MspViewModel(ribbonManager);
    }
  }
}

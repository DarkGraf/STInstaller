using System;
using System.Collections.ObjectModel;

using InstallerStudio.Models;
using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Controls;

namespace InstallerStudio.ViewModels
{
  /// <summary>
  /// Дополнительная реализация для MsiModel.
  /// </summary>
  interface IMsiModelAdditional
  {
    /// <summary>
    /// Содержит установочные директории для выбора пользователем.
    /// </summary>
    ObservableCollection<string> InstallDirectories { get; }
    /// <summary>
    /// Проверка каталога на возможность удаления из списка.
    /// </summary>
    /// <param name="directory">Проверяемая директория.</param>
    /// <returns>Истино, если можно удалить.</returns>
    bool CheckInstallDirectoryForDeleting(string directory);
  }

  class MsiViewModel : BuilderViewModel
  {
    // Класс-обёртка, реализация IWixPropertyGridControlDataSource.
    class WixPropertyGridControlDataSourceWrapper : IWixPropertyGridControlDataSource
    {
      Func<string, bool> checkForDeleting;

      public WixPropertyGridControlDataSourceWrapper(ObservableCollection<string> installDirectories, Func<string, bool> checkForDeleting)
      {
        InstallDirectories = installDirectories;
        this.checkForDeleting = checkForDeleting;
      }

      public ObservableCollection<string> InstallDirectories { get; private set; }

      public bool CheckInstallDirectoryForDeleting(string directory)
      {
        return checkForDeleting(directory);
      }
    }

    public MsiViewModel(IRibbonManager ribbonManager) : base(ribbonManager) 
    {
      IMsiModelAdditional model = Model as IMsiModelAdditional;
      WixPropertyGridControlDataSource = new WixPropertyGridControlDataSourceWrapper(model.InstallDirectories, model.CheckInstallDirectoryForDeleting);
    }

    /// <summary>
    /// Для вывода списка установочных директорий в PropertyGrid.
    /// </summary>
    public IWixPropertyGridControlDataSource WixPropertyGridControlDataSource { get; private set; }

    #region BuilderViewModel

    protected override BuilderModelFactory BuilderModelFactory
    {
      get { return new MsiModelFactory(); }
    }

    public override FileDescription FileDescription
    {
      get 
      {
        return new FileDescription
        {
          FileExtension = "msizip",
          Description = "Msi Zip"
        };
      }
    }

    #endregion
  }

  class MsiViewModelFactory : BuilderViewModelFactory
  {
    public override BuilderViewModel Create(IRibbonManager ribbonManager)
    {
      return new MsiViewModel(ribbonManager);
    }
  }
}

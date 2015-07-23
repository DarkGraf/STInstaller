using System;

using InstallerStudio.Models;
using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;

namespace InstallerStudio.ViewModels
{
  class MsiViewModel : BuilderViewModel
  {
    public MsiViewModel(IRibbonManager ribbonManager) : base(ribbonManager) { }

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

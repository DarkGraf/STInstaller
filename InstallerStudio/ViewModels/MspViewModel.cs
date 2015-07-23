using System;

using InstallerStudio.Models;
using InstallerStudio.ViewModels.Utils;

namespace InstallerStudio.ViewModels
{
  class MspViewModel : BuilderViewModel
  {
    public MspViewModel(IRibbonManager ribbonManager) : base(ribbonManager) { }

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

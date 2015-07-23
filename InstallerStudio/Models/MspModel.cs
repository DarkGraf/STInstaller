using System;

using InstallerStudio.WixElements;
using InstallerStudio.ViewModels;

namespace InstallerStudio.Models
{
  class MspModel : BuilderModel
  {
    #region BuilderModel

    protected override IWixMainEntity CreateMainEntity()
    {
      WixPatch patch = new WixPatch();
      return patch;
    }

    public override CommandMetadata[] GetElementCommands()
    {
      return new CommandMetadata[] { };
    }

    #endregion
  }

  class MspModelFactory : BuilderModelFactory
  {
    public override BuilderModel Create()
    {
      return new MspModel();
    }
  }
}

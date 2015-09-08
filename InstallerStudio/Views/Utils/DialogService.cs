using System;
using System.Windows;
using DevExpress.Xpf.Core;

using InstallerStudio.ViewModels.Utils;
using System.IO;

namespace InstallerStudio.Views.Utils
{
  class DialogService : IDialogService
  {
    Window owner;
    Lazy<OpenFileDialog> openFileLazy;
    Lazy<SaveFileDialog> saveFileLazy;
    Lazy<SettingsDialog> settingsLazy;
    Lazy<MspWizardDialog> mspWizardLazy;

    public DialogService(Window owner)
    {
      this.owner = owner;
      openFileLazy = new Lazy<OpenFileDialog>(() => new OpenFileDialog(owner));
      saveFileLazy = new Lazy<SaveFileDialog>(() => new SaveFileDialog(owner));
      settingsLazy = new Lazy<SettingsDialog>(() => new SettingsDialog(owner));
      mspWizardLazy = new Lazy<MspWizardDialog>(() => new MspWizardDialog(owner));
    }

    #region IDialogService

    public IOpenSaveFileDialog OpenFileDialog
    {
      get { return openFileLazy.Value; }
    }

    public IOpenSaveFileDialog SaveFileDialog
    {
      get { return saveFileLazy.Value; }
    }

    public ISettingsDialog SettingsDialog
    {
      get { return settingsLazy.Value; }
    }

    public IMspWizardDialog MspWizardDialog
    {
      get { return mspWizardLazy.Value; }
    }

    #endregion
  }

  abstract class DialogBase
  {
    protected Window Owner { get; private set; }

    public DialogBase(Window owner)
    {
      Owner = owner;
    }

    public abstract bool? Show();
  }

  abstract class OpenSaveFileDialogBase : DialogBase, IOpenSaveFileDialog
  {
    /// <summary>
    /// Для обеспечения сохранения последнего выбранного 
    /// пути в производных объектах.
    /// </summary>
    protected static string InitialDirectory { get; set; }

    static OpenSaveFileDialogBase()
    {
      InitialDirectory = Environment.CurrentDirectory;
    }

    public OpenSaveFileDialogBase(Window owner) : base(owner) { }

    protected abstract Microsoft.Win32.FileDialog CreateFileDialog();

    #region IOpenSaveFileDialog

    public string Filter { get; set; }

    public int FilterIndex { get; set; }

    public string Title { get; set; }

    public string FileName { get; set; }

    public override bool? Show()
    {
      Microsoft.Win32.FileDialog dialog = CreateFileDialog();
      dialog.InitialDirectory = InitialDirectory;
      dialog.Filter = Filter;
      dialog.FilterIndex = FilterIndex;
      dialog.Title = Title;
      dialog.AddExtension = true;
      dialog.FileName = FileName;
      bool? result = dialog.ShowDialog();
      if (result.GetValueOrDefault())
      {
        InitialDirectory = Path.GetDirectoryName(dialog.FileName);
        FileName = dialog.FileName;
      }
      return result;
    }

    #endregion
  }

  class OpenFileDialog : OpenSaveFileDialogBase, IOpenSaveFileDialog
  {
    public OpenFileDialog(Window owner) : base(owner) { }

    protected override Microsoft.Win32.FileDialog CreateFileDialog()
    {
      return new Microsoft.Win32.OpenFileDialog();
    }
  }

  class SaveFileDialog : OpenSaveFileDialogBase, IOpenSaveFileDialog
  {
    public SaveFileDialog(Window owner) : base(owner) { }

    protected override Microsoft.Win32.FileDialog CreateFileDialog()
    {
      return new Microsoft.Win32.SaveFileDialog();
    }
  }

  class SettingsDialog : DialogBase, ISettingsDialog
  {
    public SettingsDialog(Window owner) : base(owner) { }

    #region ISettingsDialog

    public string WixToolsetPath { get; set; }

    public string CandleFileName { get; set; }

    public string LightFileName { get; set; }

    public string TorchFileName { get; set; }

    public string PyroFileName { get; set; }

    public string UIExtensionFileName { get; set; }

    public override bool? Show()
    {
      SettingsWindow window = new SettingsWindow();
      window.Owner = Owner;
      window.Settings.WixToolsetPath = WixToolsetPath;
      window.Settings.CandleFileName = CandleFileName;
      window.Settings.LightFileName = LightFileName;
      window.Settings.TorchFileName = TorchFileName;
      window.Settings.PyroFileName = PyroFileName;
      window.Settings.UIExtensionFileName = UIExtensionFileName;
      bool? result = window.ShowDialog();
      if (result.GetValueOrDefault())
      {
        WixToolsetPath = window.Settings.WixToolsetPath;
        CandleFileName = window.Settings.CandleFileName;
        LightFileName = window.Settings.LightFileName;
        TorchFileName = window.Settings.TorchFileName;
        PyroFileName = window.Settings.PyroFileName;
        UIExtensionFileName = window.Settings.UIExtensionFileName;
      }
      return result;
    }

    #endregion
  }

  class MspWizardDialog : DialogBase, IMspWizardDialog
  {
    public MspWizardDialog(Window owner) : base(owner) { }

    #region IMspWizardDialog

    public string PathToBaseSource { get; set; }

    public string PathToTargetSource { get; set; }

    public MspWizardContentTypes ContentType { get; set; }

    public override bool? Show()
    {
      MspWizardWindow window = new MspWizardWindow();
      window.Owner = Owner;
      bool? result = window.ShowDialog();
      if (result.GetValueOrDefault())
      {
        PathToBaseSource = window.Settings.PathToBaseSource;
        PathToTargetSource = window.Settings.PathToTargetSource;
        ContentType = window.Settings.ContentType;
      }
      return result;
    }

    #endregion
  }
}
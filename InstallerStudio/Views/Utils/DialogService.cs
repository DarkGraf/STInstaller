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

    public DialogService(Window owner)
    {
      this.owner = owner;
      openFileLazy = new Lazy<OpenFileDialog>(() => new OpenFileDialog(owner));
      saveFileLazy = new Lazy<SaveFileDialog>(() => new SaveFileDialog(owner));
      settingsLazy = new Lazy<SettingsDialog>(() => new SettingsDialog(owner));
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

    public string UIExtensionFileName { get; set; }

    public override bool? Show()
    {
      SettingsWindow window = new SettingsWindow();
      window.Owner = Owner;
      window.Settings.WixToolsetPath = WixToolsetPath;
      window.Settings.CandleFileName = CandleFileName;
      window.Settings.LightFileName = LightFileName;
      window.Settings.UIExtensionFileName = UIExtensionFileName;
      bool? result = window.ShowDialog();
      if (result.GetValueOrDefault())
      {
        WixToolsetPath = window.Settings.WixToolsetPath;
        CandleFileName = window.Settings.CandleFileName;
        LightFileName = window.Settings.LightFileName;
        UIExtensionFileName = window.Settings.UIExtensionFileName;
      }
      return result;
    }

    #endregion
  }
}

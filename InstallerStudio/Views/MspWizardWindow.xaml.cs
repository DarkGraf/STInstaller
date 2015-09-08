using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

using DevExpress.Xpf.Editors;

using InstallerStudio.Common;
using InstallerStudio.ViewModels.Utils;

namespace InstallerStudio.Views
{
  /// <summary>
  /// Логика взаимодействия для MspWizardWindow.xaml
  /// </summary>
  public partial class MspWizardWindow
  {
    public class WizardSettingsInfo : ChangeableObject, IDataErrorInfo
    {
      private string pathToBaseSource;
      private string pathToTargetSource;
      private MspWizardContentTypes contentType;

      public WizardSettingsInfo()
      {
        contentType = MspWizardContentTypes.EachInOne;
      }

      public string PathToBaseSource
      {
        get { return pathToBaseSource; }
        set { SetValue(ref pathToBaseSource, value); }
      }

      public string PathToTargetSource
      {
        get { return pathToTargetSource; }
        set { SetValue(ref pathToTargetSource, value); }
      }

      public MspWizardContentTypes ContentType
      {
        get { return contentType; }
        set { SetValue(ref contentType, value); }
      }

      public bool IsValid
      {
        get
        {
          return string.IsNullOrEmpty(this["PathToBaseSource"]) && string.IsNullOrEmpty(this["PathToTargetSource"]);
        }
      }

      #region IDataErrorInfo

      public string Error
      {
        get { return string.Empty; }
      }

      public string this[string columnName]
      {
        get
        {
          switch (columnName)
          {
            case "PathToBaseSource":
              return File.Exists(PathToBaseSource) ? string.Empty : "Файл не найден";
            case "PathToTargetSource":
              return File.Exists(PathToTargetSource) ? string.Empty : "Файл не найден";
            default:
              return string.Empty;
          }
        }
      }

      #endregion
    }

    // Будем запоминать в статической переменной последний путь.
    static string lastPath = Environment.CurrentDirectory;
    static string ParamPathToBase = "PathToBase";
    static string ParamPathToTarget = "PathToTarget";

    public WizardSettingsInfo Settings { get; set; }

    public ICommand OkCloseCommand { get; private set; }

    public MspWizardWindow()
    {
      Settings = new WizardSettingsInfo();
      OkCloseCommand = new RelayCommand(param => OkClose(), (obj) => { return Settings.IsValid; });
      InitializeComponent();
      edtBase.AddHandler(Button.ClickEvent, new RoutedEventHandler(Button_Click));
      edtTarget.AddHandler(Button.ClickEvent, new RoutedEventHandler(Button_Click));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      string param = e.Source is ButtonEdit ? (string)((ButtonEdit)e.Source).Tag : null;
      if (param != ParamPathToBase && param != ParamPathToTarget)
        return;

      OpenFileDialog dialog = new OpenFileDialog();
      dialog.InitialDirectory = lastPath;
      dialog.Filter = "*.updzip|*.updzip";
      dialog.AddExtension = true;
      if (dialog.ShowDialog().GetValueOrDefault())
      {
        switch (param)
        {
          case "PathToBase":
            Settings.PathToBaseSource = dialog.FileName;
            break;
          case "PathToTarget":
            Settings.PathToTargetSource = dialog.FileName;
            break;
        }

        lastPath = Path.GetDirectoryName(dialog.FileName);
      }
    }

    private void OkClose()
    {
      DialogResult = true;
    }
  }
}

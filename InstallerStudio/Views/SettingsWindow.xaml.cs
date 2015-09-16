using System;
using System.IO;
using System.Windows;
using System.ComponentModel;

using InstallerStudio.Common;

namespace InstallerStudio.Views
{
  /// <summary>
  /// Логика взаимодействия для SettingsWindow.xaml
  /// </summary>
  public partial class SettingsWindow
  {
    public class SettingsInfo : ChangeableObject, IDataErrorInfo
    {
      private string wixToolsetPath = string.Empty;
      private string candleFileName = string.Empty;
      private string lightFileName = string.Empty;
      private string torchFileName = string.Empty;
      private string pyroFileName = string.Empty;
      private string uiExtensionFileName = string.Empty;
      private string suppressIce = string.Empty;

      public string WixToolsetPath
      {
        get { return wixToolsetPath; }
        set 
        { 
          if (SetValue(ref wixToolsetPath, value ?? string.Empty) == PropertyState.Changed)
          {
            // Уведомляем об изменения свойства имена файлов,
            // чтобы прошла проверка на ошибки с новым путем.
            NotifyPropertyChanged("CandleFileName");
            NotifyPropertyChanged("LightFileName");
            NotifyPropertyChanged("TorchFileName");
            NotifyPropertyChanged("PyroFileName");
            NotifyPropertyChanged("UIExtensionFileName");
          }
        }
      }

      public string CandleFileName
      {
        get { return candleFileName; }
        set { SetValue(ref candleFileName, value ?? string.Empty); }
      }

      public string LightFileName
      {
        get { return lightFileName; }
        set { SetValue(ref lightFileName, value ?? string.Empty); }
      }

      public string TorchFileName
      {
        get { return torchFileName; }
        set { SetValue(ref torchFileName, value ?? string.Empty); }
      }

      public string PyroFileName
      {
        get { return pyroFileName; }
        set { SetValue(ref pyroFileName, value ?? string.Empty); }
      }

      public string UIExtensionFileName
      {
        get { return uiExtensionFileName; }
        set { SetValue(ref uiExtensionFileName, value ?? string.Empty); }
      }

      public string SuppressIce
      {
        get { return suppressIce; }
        set { SetValue(ref suppressIce, value ?? string.Empty); }
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
#warning реализовать проверку для SuppressIce.
          switch (columnName)
          {
            case "WixToolsetPath":
              return Directory.Exists(WixToolsetPath) ? string.Empty : "Неправильный путь";
            case "CandleFileName":
              return File.Exists(Path.Combine(WixToolsetPath, CandleFileName)) ? string.Empty : "Неправильный файл компилятора";
            case "LightFileName":
              return File.Exists(Path.Combine(WixToolsetPath, LightFileName)) ? string.Empty : "Неправильный файл компоновщика";
            case "TorchFileName":
              return File.Exists(Path.Combine(WixToolsetPath, TorchFileName)) ? string.Empty : "Неправильный файл утилиты генерации различий";
            case "PyroFileName":
              return File.Exists(Path.Combine(WixToolsetPath, PyroFileName)) ? string.Empty : "Неправильный файл утилита создания патчей";
            case "UIExtensionFileName":
              return File.Exists(Path.Combine(WixToolsetPath, UIExtensionFileName)) ? string.Empty : "Неправильный файл UI-расширения";
            default:
              return string.Empty;
          }
        }
      }

      #endregion
    }

    public SettingsInfo Settings { get; set; }

    public SettingsWindow()
    {
      Settings = new SettingsInfo();
      InitializeComponent();
    }

    private void ButtonOk_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }
  }
}

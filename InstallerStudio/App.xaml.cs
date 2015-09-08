using System;
using System.Windows;
using DevExpress.Xpf.Core;

using NLog;

namespace InstallerStudio
{
  /// <summary>
  /// Логика взаимодействия для App.xaml
  /// </summary>
  public partial class App : Application
  {
    public App()
    {
      ThemeManager.ApplicationThemeName = "Office2010Blue";
      Dispatcher.UnhandledException += Dispatcher_UnhandledException;
    }

    void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Logger log = LogManager.GetCurrentClassLogger();
      log.Fatal(string.Format("Exception type: {0}. Message: {1}", e.Exception.GetType(), e.Exception.Message));

      string error = string.Format("Произошла ошибка приложения.\nИнформация об ошибке внесена в файл логирования.");
      MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      //e.Handled = true;
    }
  }
}

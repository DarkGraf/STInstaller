using System.Windows;
using DevExpress.Xpf.Core;

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
    }
  }
}

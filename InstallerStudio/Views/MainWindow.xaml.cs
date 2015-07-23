using System.Windows;

using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;

namespace InstallerStudio.Views
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    public MainWindow()
    {
      InitializeComponent();      
    }

    private void DXRibbonWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      MainWindow window = sender as MainWindow;
      IDialogServiceSetter setter = window != null ? window.DataContext as IDialogServiceSetter : null;
      if (setter != null)
        setter.DialogService = new DialogService(this);
    }
  }
}

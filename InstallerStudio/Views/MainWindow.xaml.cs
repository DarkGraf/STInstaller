using System;
using System.Windows;

using InstallerStudio.Utils;
using InstallerStudio.ViewModels;
using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;

namespace InstallerStudio.Views
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : IMainView
  {
    public MainWindow()
    {
      InitializeComponent();

      // Вызовем инициализацию после того, как покажем окно.
      Application.Current.Activated += Current_Activated;
    }

    void Current_Activated(object sender, EventArgs e)
    {
      if (DataContext is IMainViewSetter)
      {
        (DataContext as IMainViewSetter).ViewInitialized();
      }
      // Вызовем только один раз.
      Application.Current.Activated -= Current_Activated;
    }

    private void DXRibbonWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      MainWindow window = sender as MainWindow;

      if (window != null)
      {
        IDialogServiceSetter dialogServiceSetter = window.DataContext as IDialogServiceSetter;
        if (dialogServiceSetter != null)
          dialogServiceSetter.DialogService = new DialogService(this);

        IMainViewSetter mainViewSetter = window.DataContext as IMainViewSetter;
        if (mainViewSetter != null)
          mainViewSetter.MainView = this;
      }
    }

    #region IMainView

    public string[] CommandLineArgs
    {
      get { return Environment.GetCommandLineArgs(); }
    }

    public string ApplicationDirectory
    {
      get { return AppDomain.CurrentDomain.BaseDirectory; }
    }

    public void EditEnd()
    {
      // Обновляем все, кроме PropertyGrid (почему-то его не обновляет).
      this.UpdateBindingSources();
      // Обновляем PropertyGrig передачей фокуса главному окну.
      this.Focus();
    }

    #endregion
  }
}

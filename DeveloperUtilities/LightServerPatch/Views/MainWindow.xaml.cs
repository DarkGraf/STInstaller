using System.Threading;
using System.Windows;

using LightServerPatch.ViewModels;

namespace LightServerPatch.Views
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, IMainView
  {
    private SynchronizationContext synchronizationContext;

    public MainWindow()
    {
      synchronizationContext = SynchronizationContext.Current;
      DataContextChanged += MainWindow_DataContextChanged;
      InitializeComponent();
    }

    void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      MainWindow window = sender as MainWindow;
      IMainViewModelListener listener = window != null ? window.DataContext as IMainViewModelListener : null;
      if (listener != null)
        listener.View = this;
    }

    #region IMainView

    public void ShowAndLockMessageWindow()
    {
      SendOrPostCallback callback = delegate
      {
        ProgressMessageWindow.Instance.ShowDialog();
      };
      synchronizationContext.Post(callback, null);
    }

    public void UnlockMessageWindow()
    {
      SendOrPostCallback callback = delegate
      {
        // Теперь разрешаем пользователю закрыть окно.
        ProgressMessageWindow.Instance.AllowClosing = true;
      };
      synchronizationContext.Post(callback, null);
    }

    public void AddMessage(string message, bool isError)
    {
      SendOrPostCallback callback = delegate
      {
        ProgressMessageWindow.Instance.AddMessage(message, isError);
      };
      synchronizationContext.Post(callback, null);
    }

    #endregion
  }
}

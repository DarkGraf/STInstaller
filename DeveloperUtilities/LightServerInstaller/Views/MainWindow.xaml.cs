using System;
using System.Windows;
using System.Windows.Media.Animation;

using LightServerInstaller.ViewModels;

namespace LightServerInstaller.Views
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, IMainView
  {
    public MainWindow()
    {
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

    public void ToActivateOutSpace()
    {
      tabItemMessages.IsSelected = true;
    }

    public void StartProgress()
    {
      Storyboard story = (Storyboard)txtProgress.TryFindResource("storyProgress");
      if (story != null)
      {
        story.Stop();
        story.Begin();
      }
    }

    public void StopProgress()
    {
      Storyboard story = (Storyboard)txtProgress.TryFindResource("storyProgress");
      if (story != null)
        story.Stop();
    }

    #endregion
  }
}

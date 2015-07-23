using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LightServerPatch.Views
{
  /// <summary>
  /// Логика взаимодействия для ProgressMessageWindow.xaml
  /// </summary>
  public partial class ProgressMessageWindow : Window
  {
    private static ProgressMessageWindow instance = null;

    private ProgressMessageWindow()
    {
      InitializeComponent();

      Owner = Application.Current.MainWindow;
      IsVisibleChanged += delegate
      {
        AllowClosing = false;
      };
      btnClose.Click += delegate
      {
        Close();
      };
    }

    public static ProgressMessageWindow Instance
    {
      get
      {
        if (instance == null)
          instance = new ProgressMessageWindow();

        return instance;
      }
    }

    /// <summary>
    /// Разрешает закрытие окна. При открытии сбрасывается.
    /// </summary>
    public bool AllowClosing 
    {
      get { return btnClose.IsEnabled; }
      set { btnClose.IsEnabled = value; }
    }

    public void AddMessage(string message, bool isError)
    {
      TextBlock text = new TextBlock();
      text.TextWrapping = TextWrapping.Wrap;
      text.Text = message;
      if (isError)
        text.Foreground = Brushes.Red;
      stack.Children.Add(text);
      scroll.ScrollToBottom();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);
      e.Cancel = true;
      if (AllowClosing)
      {
        stack.Children.Clear();
        Hide();
      }
    }
  }
}

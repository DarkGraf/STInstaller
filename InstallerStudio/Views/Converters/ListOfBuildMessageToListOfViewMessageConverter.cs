using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

using InstallerStudio.Models;
using InstallerStudio.Views.Controls;
using System.Windows.Media;

namespace InstallerStudio.Views.Converters
{
  public class ListOfBuildMessageToListOfViewMessageConverter : MarkupExtension, IValueConverter
  {
    ObservableCollection<BuildMessage> source;
    ObservableCollection<ViewMessage> destination;
    Brush defaultForeground;

    public ListOfBuildMessageToListOfViewMessageConverter()
    {
      destination = new ObservableCollection<ViewMessage>();
      defaultForeground = GetDefaultForeground();
    }

    /// <summary>
    /// Получим цвет шрифта по умолчанию темы.
    /// </summary>
    Brush GetDefaultForeground()
    {
      Brush brush;
      try
      {
        string themeName = DevExpress.Xpf.Core.ThemeManager.GetThemeName(System.Windows.Application.Current.MainWindow);
        DevExpress.Xpf.Core.BackgroundPanel panel = new DevExpress.Xpf.Core.BackgroundPanel();
        DevExpress.Xpf.Core.ThemeManager.SetThemeName(panel, themeName);
        brush = panel.Foreground;
      }
      catch
      {
        brush = Brushes.Black;
      }
      return brush;
    }

    Brush GetMessageProperties(BuildMessageTypes type)
    {
      Brush brush = Brushes.Black;
      switch (type)
      {
        case BuildMessageTypes.ConsoleReceive:
          // Сообщение полученное с консоли.
          brush = Brushes.DarkGray;
          break;
        case BuildMessageTypes.ConsoleSend:
          // Сообщение посланное консоли.
          brush = Brushes.DarkGray;
          break;
        case BuildMessageTypes.Notification:
          // Уведомление о текущих операциях.
          brush = Brushes.DimGray;
          break;
        case BuildMessageTypes.Information:
          // Важная итоговая информация.
          brush = defaultForeground;
          break;
        case BuildMessageTypes.Error:
          // Сообщение об ошибках.
          brush = Brushes.Red;
          break;
      }
      return brush;
    }

    void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
          foreach (object obj in e.NewItems)
          {
            BuildMessage m = obj as BuildMessage;
            if (m != null)
            {
              destination.Add(new ViewMessage(m.Message, GetMessageProperties(m.Type)));
            }
          }
          break;
        case NotifyCollectionChangedAction.Reset:
          destination.Clear();
          break;
      }
    }

    #region MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #endregion

    #region IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (source != null)
      {
        source.CollectionChanged -= source_CollectionChanged;
      }

      source = value as ObservableCollection<BuildMessage>;
      source.CollectionChanged += source_CollectionChanged;
      return destination;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Обратное преобразование не требуется.
      throw new NotImplementedException();
    }

    #endregion
  }
}

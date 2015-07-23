using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace LightServerInstaller.ViewModels.Converters
{
  class ObservableCollectionOfStringToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      StringBuilder builder = new StringBuilder();
      foreach (string str in (ObservableCollection<string>)value)
        builder.Append(str + Environment.NewLine);
      return builder.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return new ObservableCollection<string>(((string)value).Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
    }
  }
}

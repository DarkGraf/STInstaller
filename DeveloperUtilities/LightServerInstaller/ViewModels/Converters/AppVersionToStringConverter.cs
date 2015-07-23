using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

using LightServerLib.Models;

namespace LightServerInstaller.ViewModels.Converters
{
  class AppVersionToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      object old = value;
      try
      {
        return value.ToString();
      }
      catch
      {
        return Binding.DoNothing;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      object old = value;
      try
      {
        AppVersion version = (string)value;
        return version;
      }
      catch
      {
        // Возвращаем пустую строку, чтобы UI-элемент показал ошибку.
        return "";
      }
    }
  }

}

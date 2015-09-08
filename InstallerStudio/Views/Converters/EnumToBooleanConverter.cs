using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InstallerStudio.Views.Converters
{
  /// <summary>
  /// Универсальный конвертер перечисления в логическое значение.
  /// Для RadioButton.
  /// </summary>
  public class EnumToBooleanConverter : IValueConverter
  {
    #region IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string str = parameter as string;
      if (parameter == null || !Enum.IsDefined(value.GetType(), value))
        return DependencyProperty.UnsetValue;

      object obj = Enum.Parse(value.GetType(), str);
      return obj.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string str = parameter as string;
      if (str == null)
        return DependencyProperty.UnsetValue;

      return Enum.Parse(targetType, str);
    }

    #endregion
  }
}

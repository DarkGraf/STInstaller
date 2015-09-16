using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

using InstallerStudio.WixElements;

namespace InstallerStudio.Views.Converters
{
  public class AppVersionToStringConverter : MarkupExtension, IValueConverter
  {
    public AppVersionToStringConverter() : base() { }

    #region MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #endregion

    #region IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // AppVersion -> string.
      return (value).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // string -> AppVersion.
      try
      {
        return new AppVersion(value.ToString());
      }
      catch
      {
        return DependencyProperty.UnsetValue;
      }
    }

    #endregion
  }
}

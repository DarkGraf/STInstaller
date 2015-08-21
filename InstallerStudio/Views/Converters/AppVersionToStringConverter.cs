using System;
using System.Globalization;
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
      if (value is AppVersion && targetType == typeof(string))
      {
        return (value).ToString();
      }
      else
        throw new ArgumentException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string && targetType == typeof(AppVersion))
      {
        return new AppVersion(value.ToString());
      }
      else
        throw new ArgumentException();
    }

    #endregion
  }
}

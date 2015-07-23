using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

using InstallerStudio.Views.Utils;

namespace InstallerStudio.Views.Converters
{
  // Наследование от MarkupExtension позволяет избежать использование конвертера через ресурсы,
  // теперь его можно использовать как полноценный элемент разметки.
  public class EnumToImageConverter : MarkupExtension, IValueConverter
  {
    public EnumToImageConverter() { }

    #region MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #endregion

    #region IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ImageResourceFactory.CreateImageResource(value.GetType())[(Enum)value];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}

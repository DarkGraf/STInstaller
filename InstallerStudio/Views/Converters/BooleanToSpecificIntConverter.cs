using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace InstallerStudio.Views.Converters
{
  /// <summary>
  /// Конвертация типа bool и конкретного числа типа int.
  /// Работает только обратная конвертация. Возвращает истину, если конвертируемое значение
  /// равно значению указанному в параметрах конвертера. Ложь в противном случае.
  /// </summary>
  public class BooleanToSpecificIntConverter : MarkupExtension, IValueConverter
  {
    public BooleanToSpecificIntConverter() { }

    #region MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #endregion

    #region IValueConverter
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return -1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value.ToString().Equals(parameter.ToString());
    }

    #endregion
  }
}

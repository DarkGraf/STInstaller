using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace InstallerStudio.Views.Converters
{
  public class ListOfStringToStringConverter : MarkupExtension, IValueConverter
  {
    public ListOfStringToStringConverter() { }

    #region MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #endregion

    #region IValueConverter

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      StringBuilder builder = new StringBuilder();
      foreach (string str in (IList<string>)value)
        builder.Append(str + Environment.NewLine);
      return builder.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Если понадобиться обратное преобразование, то надо реализовать нужный тип, 
      // это может быть List<string>, ObservableCollection<string> и т.п.
      throw new NotImplementedException();
    }

    #endregion
  }
}

using System;
using System.Globalization;

namespace InstallerStudio.Utils
{
  /// <summary>
  /// Класс автоформатирования размера.
  /// </summary>
  class SizeAutoFormatting
  {
    /// <summary>
    /// Осуществляет форматирование размера.
    /// </summary>
    /// <param name="size">Размер в байтах.</param>
    /// <returns></returns>
    public static string Format(long size)
    {
      string result;
      int dim = 1024;
      
      if (size < dim)
        result = string.Format("{0} б", size);
      else if (size < dim * dim)
        result = string.Format("{0} Кб", Math.Round((double)size / dim, 1).ToString(CultureInfo.InvariantCulture));
      else if (size < dim * dim * dim)
        result = string.Format("{0} Мб", Math.Round((double)size / (dim * dim), 1).ToString(CultureInfo.InvariantCulture));
      else
        result = string.Format("{0} Гб", Math.Round((double)size / (dim * dim * dim), 1).ToString(CultureInfo.InvariantCulture));

      return result;
    }
  }
}

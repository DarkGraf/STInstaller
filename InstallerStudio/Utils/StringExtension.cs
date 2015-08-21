using System;
using System.IO;

namespace InstallerStudio.Utils
{
  static class StringExtension
  {
    /// <summary>
    /// Добавляет в конце строкового пути разделитель, если его нет и строка не пустая.
    /// </summary>
    public static string IncludeTrailingPathDelimiter(this string str)
    {
      if (str.Length > 0 && str[str.Length - 1] != Path.DirectorySeparatorChar)
        str += Path.DirectorySeparatorChar;
      return str;
    }
  }
}

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

    /// <summary>
    /// Корректирует имя, заменяя все символы, кроме латинских букв и цифр, на их ASCII коды.
    /// Также первым символом выставляет знак подчеркивания.
    /// </summary>
    public static string ReplaceNotLetterAndNotDigitWithASCIICode(this string str)
    {
      for (int i = 0; i < str.Length; i++)
      {
        if (!char.IsDigit(str[i]) && !IsLatinLetter(str[i]))
          str = str.Replace(str[i].ToString(), ((int)str[i]).ToString("X2"));
      }
      return "_" + str;
    }

    /// <summary>
    /// Проверяет, является ли символ латинским.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private static bool IsLatinLetter(char c)
    {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
  }
}

using System;

namespace WixSTActions.Utils
{
  /// <summary>
  /// Вспомогательный класс выполняющий прямую и обратную конвертацию полного имени базы данных.
  /// </summary>
  static class NameViewConverter
  {
    const char Delimiter = '.';

    public static string GetNameView(string serverName, string databaseName)
    {
      return string.Format("{0}{1}{2}", serverName, Delimiter, databaseName);
    }

    public static void ParseNameView(string nameView, out string serverName, out string databaseName)
    {
      string[] str = nameView.Split(Delimiter);
      if (str.Length == 2)
      {
        serverName = str[0];
        databaseName = str[1];
      }
      else
        serverName = databaseName = "";
    }
  }
}

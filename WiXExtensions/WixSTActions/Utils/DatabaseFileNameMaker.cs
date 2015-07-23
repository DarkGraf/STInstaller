using System;
using System.IO;

namespace WixSTActions.Utils
{
  static class DatabaseFileNameMaker
  {
    /// <summary>
    /// Возвращает новое имя файла базы данных на основе имени сервера, имени базы, 
    /// инсталляционного пути и текущего пути файла.
    /// </summary>
    /// <param name="server"></param>
    /// <param name="database"></param>
    /// <param name="path"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string Make(string server, string database, string installPath, string extension)
    {
      // Создаем имена файлов по схеме:
      // ИмяСервера\Экземпляр -> ИмяСервераЭкземплярИмяБазы.
      // ИмяСервера -> ИмяСервераИмяБазы.
      // installPath - инсталляционный путь.
      // extension - расширение.
      string result = server.Replace("\\", "") + database;
      result = Path.Combine(installPath, result);
      result = Path.ChangeExtension(result, extension);
      return result;
    }
  }
}

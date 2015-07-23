using System;
using System.IO;
using System.Linq;
using System.Text;
using WixSTActions.SqlWorker;

namespace WixSTActions.Utils
{
  public class SqlScriptParser : ISqlScriptParser
  {
    public SqlScriptParser(string path)
    {
      using (StreamReader reader = new StreamReader(path, Encoding.GetEncoding(1251)))
      {
        Parse(reader);
      }
    }

    public string[] Queries
    {
      private set;
      get;
    }

    private void Parse(StreamReader reader)
    {
      string str;
      StringBuilder builder = new StringBuilder();

      // Читаем строку и запоминаем ее, но только чтобы она не начиналась
      // с однострочного комментария, так он может отменять комментирование
      // многострочного комментария.
      while ((str = reader.ReadLine()) != null)
      {
        if (str.Trim().StartsWith("--"))
          continue;

        if (builder.Length > 0)
          builder.Append(Environment.NewLine);
        builder.Append(str);
      }

      // Ищем начало и конец многострочного комментария ("/*" и "*/"),
      // меняем его на символ новой строки.
      string query = builder.ToString();
      int start, end, curIndex = 0;
      // Будем выполнять цикл, пока найден хотя бы один из символов открытия или
      // закрытия. Оператор "или" должен быть СТРОГО |, то есть подразумевать
      // вычисления в любом случае двух выражений.
      while ((start = query.IndexOf("/*", curIndex)) != -1 | (end = query.IndexOf("*/", curIndex)) != -1)
      {
        // Если начало стоит после закрытия или отсутствует один из символов,
        // то выбрасывае исключение.
        if (start > end || start == -1 || end == -1)
          throw new InvalidDataException("Ошибка разбора комментариев в SQL-скрипте.");

        query = query.Remove(start, end - start + 2);

        // Это место вставляем переход на новую строку.
        query = query.Insert(start, Environment.NewLine);
        curIndex = start;
      }

      // Далее разбиваем запрос на подзапросы используя комманду "GO", "Go", "gO", "go",
      // также учитываем возможные разные способы ее написания, 
      // используя большой и малый регистр.
      Queries = System.Text.RegularExpressions.Regex.Split(query, @"\bgo\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        .Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }
  }
}

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;

using WixSTActions.Utils;

namespace WixSTActionsTest
{
  [TestClass]
  public class SqlScriptParserTest
  {
    #region Запросы для тестов.

    readonly string query =
@"go
select 1
go

/*
select 2 -- Вместо этого добавиться переход на новую строку.
go
*/

-- Комментарий #1 -- Просто удалится.

select 3 -- Итого впереди 6 переходов и после один.
GO
--/*
  -- Комментарий №2 с пробелами в начале
select 4
--*/
gO
select 5 as Good
Go";

    readonly string[] expected = new string[] 
    {
@"
select 1
",
@"





select 3 -- Итого впереди 6 переходов и после один.
",
@"
select 4
",
@"
select 5 as Good
"
    };

    #endregion

    [TestMethod]
    [TestCategory("Other")]
    public void SqlScriptParserSuccessful()
    {
      using (ShimsContext.Create())
      {
        int i = 0; // Текущий указатель на считываемую строку.
        System.IO.Fakes.ShimStreamReader.ConstructorStringEncoding = (@this, path, encoding) => { };
        System.IO.Fakes.ShimStreamReader.AllInstances.ReadLine = delegate
        {
          string[] str = query.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
          return str.Length > i ? str[i++] : null;
        };

        // Начало теста.
        SqlScriptParser parser = new SqlScriptParser(@"C:\Test.sql");
        Assert.AreEqual(expected.Length, parser.Queries.Length);
        for (int j = 0; j < expected.Length; j++)
          Assert.AreEqual(expected[j], parser.Queries[j]);
      }
    }

    [TestMethod]
    [TestCategory("Other")]
    [ExpectedException(typeof(InvalidDataException))]
    public void SqlScriptParserErrorComment()
    {
      using (ShimsContext.Create())
      {
        int i = 0;
        System.IO.Fakes.ShimStreamReader.ConstructorStringEncoding = (@this, path, encoding) => { };
        System.IO.Fakes.ShimStreamReader.AllInstances.ReadLine = delegate { return i++ == 0 ? "*//*" : null; };

        // Начало теста.
        SqlScriptParser parser = new SqlScriptParser(@"C:\Test.sql");
      }
    }
  }
}

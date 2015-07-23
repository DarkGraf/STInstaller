using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WixSTActions.Utils;

namespace WixSTActionsTest
{
  [TestClass]
  public class DatabaseFileNameMakerTest
  {
    [TestMethod]
    [TestCategory("Other")]
    public void DatabaseFileNameMakerTesting()
    {
      string server = "TESTHOST";
      string database = "NewDatabase";
      string installPath = "C:\\Program Files\\ASPO";
      string extension = "mdf"; // Можно без точки.
      string expected = "C:\\Program Files\\ASPO\\TESTHOSTNewDatabase.mdf";
      string actual;

      actual = DatabaseFileNameMaker.Make(server, database, installPath, extension);
      Assert.AreEqual(expected, actual);

      server = "TESTHOST\\SQL2012";
      extension = ".mdf"; // Можно с точкой.
      expected = "C:\\Program Files\\ASPO\\TESTHOSTSQL2012NewDatabase.mdf";
      actual = DatabaseFileNameMaker.Make(server, database, installPath, extension);
      Assert.AreEqual(expected, actual);
    }
  }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WixSTActions.Utils;

namespace WixSTActionsTest
{
  [TestClass]
  public class NameViewConverterTest
  {
    [TestMethod]
    [TestCategory("Other")]
    public void NameViewConverterTesting()
    {
      // Прямое.
      string nameView = NameViewConverter.GetNameView("ServerName", "DatabaseName");
      Assert.AreEqual("ServerName.DatabaseName", nameView);

      // Обратное.
      string serverName, databaseName;
      NameViewConverter.ParseNameView(nameView, out serverName, out databaseName);
      Assert.AreEqual("ServerName", serverName);
      Assert.AreEqual("DatabaseName", databaseName);
    }
  }
}

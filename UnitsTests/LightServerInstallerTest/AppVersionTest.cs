using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightServerLib.Models;

namespace LightServerInstallerTest
{
  [TestClass]
  public class AppVersionTest
  {
    #region Тесты конструкторов.

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    public void AppVersionCreate()
    {
      AppVersion version = new AppVersion();
      Assert.AreEqual(0, version.Major);
      Assert.AreEqual(0, version.Minor);
      Assert.AreEqual(0, version.Build);
      Assert.AreEqual(0, version.Revision);
    }

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    public void AppVersionCreateFromDigits()
    {
      AppVersion version = new AppVersion(4, 3, 2, 1);
      Assert.AreEqual(4, version.Major);
      Assert.AreEqual(3, version.Minor);
      Assert.AreEqual(2, version.Build);
      Assert.AreEqual(1, version.Revision);
    }

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    public void AppVersionCreateFromString()
    {
      AppVersion version = new AppVersion("4.3.2.1");
      Assert.AreEqual(4, version.Major);
      Assert.AreEqual(3, version.Minor);
      Assert.AreEqual(2, version.Build);
      Assert.AreEqual(1, version.Revision);
    }

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void AppVersionCreateFromNullString()
    {
      AppVersion version = new AppVersion(null);
    }

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    [ExpectedException(typeof(ArgumentException))]
    public void AppVersionCreateFromEmptyString()
    {
      AppVersion version = new AppVersion("");
    }

    [TestMethod]
    [TestCategory("LightServerInstaller")]
    [ExpectedException(typeof(ArgumentException))]
    public void AppVersionCreateFromBadString()
    {
      AppVersion version = new AppVersion("a.b.c.d");
    }

    #endregion

    /// <summary>
    /// Тест переопределенного метода ToString().
    /// </summary>
    [TestMethod]
    [TestCategory("LightServerInstaller")]
    public void AppVersionToString()
    {
      AppVersion version = new AppVersion(4, 3, 2, 1);
      Assert.IsTrue(version.ToString() == "4.3.2.1");
    }

    /// <summary>
    /// Тесты преобразования в строку и обратно.
    /// </summary>
    [TestMethod]
    [TestCategory("LightServerInstaller")]
    public void AppVersionImplicit()
    {
      AppVersion version = new AppVersion();

      // Берем версию из строки.
      version = "4.3.2.1";
      Assert.AreEqual(version.ToString(), "4.3.2.1");

      // Берем строку из версии.
      string versionString = version;
      Assert.AreEqual(versionString, "4.3.2.1");
    }
  }

}

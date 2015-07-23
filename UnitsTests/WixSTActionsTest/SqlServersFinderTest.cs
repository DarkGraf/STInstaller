using System;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WixSTActions.Utils;

namespace WixSTActionsTest
{
  [TestClass]
  public class SqlServersFinderTest
  {
    [TestMethod]
    [TestCategory("Other")]
    public void SqlServersFinderTesting()
    {
      using (ShimsContext.Create())
      {
        string[] expected = new string[] { "TESTCOMP", "TESTCOMP\\SQL2008", "TESTCOMP\\SQL2012" };

        Microsoft.Win32.Fakes.ShimRegistryKey.AllInstances.OpenSubKeyString = delegate { return new Microsoft.Win32.Fakes.ShimRegistryKey(); };
        Microsoft.Win32.Fakes.ShimRegistryKey.AllInstances.GetValueString = delegate { return new string[] { "MSSQLSERVER", "SQL2008", "SQL2012" }; };
        System.Fakes.ShimEnvironment.MachineNameGet = delegate { return "TESTCOMP"; };

        // Когда есть запись в реестре.
        string[] servers = SqlServersFinder.Find();
        Assert.AreEqual(expected.Length, servers.Length);
        for (int i = 0; i < expected.Length; i++)
          Assert.AreEqual(expected[i], servers[i]);

        // Нет записи в реестре.
        Microsoft.Win32.Fakes.ShimRegistryKey.AllInstances.GetValueString = delegate { return null; };
        servers = SqlServersFinder.Find();
        Assert.IsNotNull(servers);
        Assert.AreEqual(0, servers.Length);
      }
    }
  }
}

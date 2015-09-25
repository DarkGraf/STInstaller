using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class SizeAutoFormattingTest
  {
    [TestMethod]
    public void SizeAutoFormattingTesting()
    {
      Assert.AreEqual("0 б", SizeAutoFormatting.Format(0));
      Assert.AreEqual("1 б", SizeAutoFormatting.Format(1));
      Assert.AreEqual("1023 б", SizeAutoFormatting.Format(1023));

      Assert.AreEqual("1 Кб", SizeAutoFormatting.Format(1024));
      // 1500 / 1024 = 1,46484375
      Assert.AreEqual("1.5 Кб", SizeAutoFormatting.Format(1500));
      // 1024 * 1024
      Assert.AreEqual("1 Мб", SizeAutoFormatting.Format(1024 * 1024));
      // 4000000 / 1024 / 1024 = 3,814697265625
      Assert.AreEqual("3.8 Мб", SizeAutoFormatting.Format(4000000));
      // 1024 * 1024 * 1024
      Assert.AreEqual("1 Гб", SizeAutoFormatting.Format(1024 * 1024 * 1024));
      // 3131375616 / 1024 / 1024 / 1024 = 2,91632080078125
      Assert.AreEqual("2.9 Гб", SizeAutoFormatting.Format(3131375616));
    }
  }
}
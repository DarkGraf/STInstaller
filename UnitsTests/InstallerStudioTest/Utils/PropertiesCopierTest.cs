using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class PropertiesCopierTest
  {
    public class Source
    {
      private string propY;

      public string PropA { get; set; }
      public string PropB { get; set; }
      public string PropC { get; set; }

      public string PropX { get; set; }
      public string PropY { set { propY = value; } }
      public float PropZ { get; set; }
    }

    public class Dest
    {
      private string propX;

      public string PropA { get; set; }
      public string PropB { get; set; }
      public string PropD { get; set; }

      public string PropX { get { return propX; } }
      public string PropY { get; set; }
      public string PropZ { get; set; }
    }

    [TestMethod]
    public void PropertiesCopierTesting()
    {
      Source source = new Source { PropA = "A", PropB = "B", PropC = "C", PropX = "X", PropY = "Y", PropZ = 1 };
      Dest dest = new Dest();

      // Укажем правильные свойства.
      PropertiesCopier.Copy(dest, source, new string[] { "A", "B" });
      Assert.AreEqual("A", dest.PropA);
      Assert.AreEqual("B", dest.PropB);
      Assert.AreEqual(null, dest.PropD);
      Assert.AreEqual(null, dest.PropX);
      Assert.AreEqual(null, dest.PropY);
      Assert.AreEqual(null, dest.PropZ);
    }
  }
}

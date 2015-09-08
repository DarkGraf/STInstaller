using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;

using InstallerStudio.WixElements;
using InstallerStudio.Utils;

namespace InstallerStudioTest.WixElements
{
  [TestClass]
  public class WixMD5Test
  {
    [TestMethod]
    public void WixMD5WithoutFile()
    {
      using (ShimsContext.Create())
      {
        // Компонент создает Guid сам, вмешаемся.
        System.Fakes.ShimGuid.NewGuid = () => { return Guid.Parse("53773F52-67A0-475E-8622-A2C57E7E6B7B"); };

        WixComponentElement componentOne = new WixComponentElement();
        WixMD5ElementHash hashOne = componentOne.GetMD5();

        Assert.AreEqual(componentOne.GetType().Name, hashOne.Type);
        Assert.AreEqual(componentOne.Id, hashOne.Id);
        Assert.IsNotNull(hashOne.Hash);
        // Должна быть длина 32 символа.
        Assert.AreEqual(32, hashOne.Hash.Length);
        Assert.AreEqual("2b0596edb4097b702f610cf86302233e", hashOne.Hash);
        Assert.IsNull(hashOne.Files);

        WixComponentElement componentTwo = new WixComponentElement();
        WixMD5ElementHash hashTwo = componentTwo.GetMD5();
        Assert.AreEqual(hashOne.Hash, hashTwo.Hash);
      }
    }

    [TestMethod]
    public void WixMD5WithFile()
    {
      using (ShimsContext.Create())
      {
        // Компонент создает Guid сам, вмешаемся.
        System.Fakes.ShimGuid.NewGuid = () => { return Guid.Parse("53773F52-67A0-475E-8622-A2C57E7E6B7B"); };
        
        // Создадим директорию не привязанную к исполняющей программе.
        string dir = "TestDirectory"; 
        Directory.CreateDirectory(dir);

        // Создадим файлы имитирующие файлы базы данных.
        string mdfFile = dir + "\\" + "File.mdf";
        string ldfFile = dir + "\\" + "File.ldf";
        File.WriteAllText(mdfFile, "Data");
        File.WriteAllText(ldfFile, "Log");

        WixDbComponentElement component = new WixDbComponentElement();
        component.MdfFile = mdfFile;
        component.LdfFile = ldfFile;

        WixMD5ElementHash hash = component.GetMD5(dir);

        Directory.Delete(dir, true);

        // Хеши для файлов:
        // ce0be71e33226e4c1db2bcea5959f16b - File.ldf
        // f6068daa29dbb05a7ead1e3b5a48bbee - File.mdf
        Assert.IsNotNull(hash.Files);
        Assert.AreEqual(2, hash.Files.Count);
        Assert.AreEqual("ce0be71e33226e4c1db2bcea5959f16b", hash.Files["File.ldf"]);
        Assert.AreEqual("f6068daa29dbb05a7ead1e3b5a48bbee", hash.Files["File.mdf"]);
      }
    }

    /// <summary>
    /// Вычисление MD5 для IWixElement с разными дочерними элементами.
    /// </summary>
    [TestMethod]
    public void WixMD5WithChilds()
    {
      WixFeatureElement featureOne = new WixFeatureElement();
      featureOne.Items.Add(new WixComponentElement());
      XmlSaverLoader.Save(featureOne, "Test.xml");

      WixFeatureElement featureTwo = XmlSaverLoader.Load<WixFeatureElement>("Test.xml");
      featureTwo.Items.Add(new WixComponentElement());

      WixMD5ElementHash hashOne = featureOne.GetMD5();
      WixMD5ElementHash hashTwo = featureTwo.GetMD5();

      Assert.AreNotEqual(hashOne.Hash, hashTwo.Hash);

      File.Delete("Test.xml");
    }
  }
}

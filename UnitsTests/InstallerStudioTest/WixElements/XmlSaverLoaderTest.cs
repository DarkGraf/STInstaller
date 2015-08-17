using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;
using InstallerStudio.WixElements;

namespace InstallerStudioTest.WixElements
{
  // Здесь тестируется функциональность XmlSaverLoader на базе WixFeatureElement.
  // Подробные тесты сохранения WixFeatureElement в файле WixElementsTest.cs. 
  [TestClass]
  public class XmlSaverLoaderTest
  {
    /// <summary>
    /// Сохранение и загрузка WixFeatureElement.
    /// </summary>
    [TestMethod]
    public void XmlSaverLoaderWixFeatureElementSaveAndLoad()
    {
      string fileName = "Test.xml";

      if (File.Exists(fileName))
        File.Delete(fileName);

      WixFeatureElement featureForSave = new WixFeatureElement();
      featureForSave.Id = "Id1";
      featureForSave.Title = "Title1";
      featureForSave.Description = "Description1";

      WixFeatureElement featureNested = new WixFeatureElement();
      featureNested.Id = "Id2";
      featureNested.Title = "Title2";
      featureNested.Description = "Description2";

      featureForSave.Items.Add(featureNested);

      XmlSaverLoader.Save<WixFeatureElement>(featureForSave, fileName);
     
      Assert.IsTrue(File.Exists(fileName));

      WixFeatureElement featureForLoad = XmlSaverLoader.Load<WixFeatureElement>(fileName);

      if (File.Exists(fileName))
        File.Delete(fileName);

      Assert.AreEqual(featureForSave.Id, featureForLoad.Id);
      Assert.AreEqual(featureForSave.Title, featureForLoad.Title);
      Assert.AreEqual(featureForSave.Description, featureForLoad.Description);
    }

    /// <summary>
    /// Загрузка с указанием целевого типа. В качестве обобщенного типа указывается
    /// базовый тип.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void XmlSaverLoaderWixFeatureElementLoadWithTargetType()
    {
      string fileName = "Test.xml";

      if (File.Exists(fileName))
        File.Delete(fileName);

      try
      {
        WixFeatureElement actual = new WixFeatureElement();
        actual.Id = "Id1";

        XmlSaverLoader.Save<WixFeatureElement>(actual, fileName);

        // Укажем правильный тип.
        IWixElement expected = XmlSaverLoader.Load<IWixElement>(fileName, typeof(WixFeatureElement));

        Assert.IsInstanceOfType(expected, typeof(WixFeatureElement));
        Assert.AreEqual(((WixFeatureElement)expected).Id, actual.Id);

        // Укажем неправильный тип.
        // Должен быть выброс исключения.
        expected = XmlSaverLoader.Load<IWixElement>(fileName, typeof(object));        
      }
      finally
      {
        if (File.Exists(fileName))
          File.Delete(fileName);
      }
    }
  }
}

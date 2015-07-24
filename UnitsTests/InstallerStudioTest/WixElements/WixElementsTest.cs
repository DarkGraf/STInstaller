using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.WixElements;
using InstallerStudio.Views.Utils;

namespace InstallerStudioTest.WixElements
{
  // Базовый класс для тестов.
  class WixElementTestBase : WixElementBase
  {
    public override ElementsImagesTypes ImageType
    {
      get { throw new NotImplementedException(); }
    }

    public override string ShortTypeName
    {
      get { throw new NotImplementedException(); }
    }
  }

  [TestClass]
  public class WixElementsCreateTest
  {
    class WixChildA : WixElementTestBase { }

    class WixChildB : WixElementTestBase { }

    class WixParent : WixElementTestBase
    {
      protected override IEnumerable<Type> AllowedTypesOfChildren
      {
        get { return new Type[] { typeof(WixChildA), typeof(WixChildB) }; }
      }
    }

    [TestMethod]
    public void WixElementsCreatingSuccessful()
    {
      WixParent elements = new WixParent();

      elements.Id = "Parent";
      Assert.AreEqual(elements.Id, "Parent");

      // Добавляем разрешенные.
      elements.Items.Add(new WixChildA { Id = "ChildA1" });
      elements.Items.Add(new WixChildB { Id = "ChildB1" });
      
      Assert.AreEqual(elements.Items.Count, 2);
      foreach (string str in new string[] { "ChildA1", "ChildB1" })
        Assert.IsNotNull(elements.Items.FirstOrDefault(v => v.Id == str));
    }

    [TestMethod]
    [ExpectedException(typeof(WixElementBase.WrongChildTypeException))]
    public void WixElementsCreatingError()
    {
      WixParent elements = new WixParent();

      // Добавляем не разрешенную.
      // Должно выбросится исключение.
      elements.Items.Add(new WixParent { Id = "Wrong" });
    }
  }

  [TestClass]
  public class WixFileElementTest
  {
    string oldFileName;
    string oldDirectory;
    string actualFileName;
    string actualDirectory;

    /// <summary>
    /// Тестирование события интерфейса IFileSupport у WixFileElement.
    /// </summary>
    [TestMethod]
    public void WixFileElementFileSupport()
    {
      WixFileElement element = new WixFileElement();
      element.FileChanged += element_FileChanged;
      
      // Меняем Path, он является исходным путем.
      element.FileName = "File.txt";
      Assert.AreEqual(null, oldFileName);
      Assert.AreEqual("File.txt", actualFileName);
      Assert.AreEqual(null, oldDirectory);
      Assert.AreEqual(null, actualDirectory);

      // Меняем InstallDirectory, он является исходным путем.
      element.InstallDirectory = "C:\\Temp";
      Assert.AreEqual("File.txt", oldFileName);
      Assert.AreEqual("File.txt", actualFileName);
      Assert.AreEqual(null, oldDirectory);
      Assert.AreEqual("C:\\Temp", actualDirectory);
    }

    private void element_FileChanged(object sender, FileSupportEventArgs e)
    {
      oldFileName = e.OldFileName;
      oldDirectory = e.OldDirectory;
      actualFileName = e.ActualFileName;
      actualDirectory = e.ActualDirectory;
    }
  }
}

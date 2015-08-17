using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.WixElements;
using InstallerStudio.Views.Utils;
using InstallerStudio.Utils;

namespace InstallerStudioTest.WixElements
{
  // Базовый класс для тестов.
  [DataContract(Namespace = StringResources.Namespace)]
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

  // Создание и добавление разрешенных и запрещенных подъэлементов.
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

  /// <summary>
  /// Тестирование события интерфейса IFileSupport у WixFileElement.
  /// </summary>
  [TestClass]
  public class WixElementsFileSupportTest
  {
    string oldFileName;
    string oldDirectory;
    string actualFileName;
    string actualDirectory;
    string rawFileName;

    [TestMethod]
    public void WixElementsFileSupport()
    {
      WixFileElement element = new WixFileElement();
      element.FileChanged += element_FileChanged;
      
      // Меняем FileName, он является исходным путем.
      element.FileName = "D:\\File.txt";
      Assert.AreEqual(null, oldFileName);
      Assert.AreEqual("File.txt", actualFileName);
      Assert.AreEqual(null, oldDirectory);
      Assert.AreEqual(null, actualDirectory);
      Assert.AreEqual("D:\\File.txt", rawFileName);
      // Проверка директорий.
      Assert.AreEqual("", element.GetInstallDirectories()[0]);

      // Меняем InstallDirectory, он является исходным путем.
      element.InstallDirectory = "C:\\Temp";
      Assert.AreEqual("File.txt", oldFileName);
      Assert.AreEqual("File.txt", actualFileName);
      Assert.AreEqual(null, oldDirectory);
      Assert.AreEqual("C:\\Temp", actualDirectory);
      Assert.AreEqual("File.txt", rawFileName);
      // Проверка директорий.
      Assert.AreEqual("C:\\Temp", element.GetInstallDirectories()[0]);
    }

    private void element_FileChanged(object sender, FileSupportEventArgs e)
    {
      oldFileName = e.OldFileName;
      oldDirectory = e.OldDirectory;
      actualFileName = e.ActualFileName;
      actualDirectory = e.ActualDirectory;
      rawFileName = e.RawFileName;
    }
  }

  /// <summary>
  /// Сериализация и десериализация элемента.
  /// </summary>
  [TestClass]
  public class WixElementsSerializeAndDeserialize
  {
    [DataContract(Namespace = StringResources.Namespace)]
    class WixA : WixElementTestBase 
    {
      private Type[] allowedTypesOfChildren;

      protected override void Initialize()
      {
        base.Initialize();
        allowedTypesOfChildren = new Type[] { typeof(WixA) };
      }

      protected override IEnumerable<Type> AllowedTypesOfChildren
      {
        get { return allowedTypesOfChildren; }
      }

      public IEnumerable<Type> AllowedTypesOfChildrenTest
      {
        get { return AllowedTypesOfChildren; }
      }
    }

    static string fileName = "test.xml";

    [TestInitialize]
    [TestCleanup]
    public void TestInitializeAndCleanup()
    {
      if (File.Exists(fileName))
        File.Delete(fileName);
    }

    [TestMethod]
    public void WixElementsSerializeAndDeserializeAllowedTypesOfChildren()
    {
      // При десериализации DataContractSerializer не вызывается конструктор
      // по умолчанию, поэтому тестируем данную ситуацию.
      WixA a = new WixA();
      XmlSaverLoader.Save<WixA>(a, fileName);
      a = XmlSaverLoader.Load<WixA>(fileName);

      Assert.IsNotNull(a.AllowedTypesOfChildrenTest);
      Assert.AreEqual(1, a.AllowedTypesOfChildrenTest.Count());
      Assert.AreEqual(a.AllowedTypesOfChildrenTest.First(), typeof(WixA));
    }

    [TestMethod]
    public void WixElementsSerializeAndDeserializeAddAfterDeserialize()
    {
      // После десериализации добавим элемент. Таким образом проверим что коллекция
      // при десерриализации инстанцировалась нужным типом.
      WixA a = new WixA();
      XmlSaverLoader.Save<WixA>(a, fileName);
      a = XmlSaverLoader.Load<WixA>(fileName);
      
      a.Items.Add(new WixA());
      Assert.AreEqual(1, a.Items.Count());
    }
  }
}

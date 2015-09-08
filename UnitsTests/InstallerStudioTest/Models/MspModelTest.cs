using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;

using InstallerStudio.Models;
using InstallerStudio.WixElements;
using InstallerStudio.Utils;

namespace InstallerStudioTest.Models
{
  [TestClass]
  public class MspModelTest
  {
    /// <summary>
    /// Проверка корневого элемента.
    /// </summary>
    [TestMethod]
    public void MspModelRootElement()
    {
      BuilderModel model = new MspModel();
      Assert.IsInstanceOfType(model.RootItem, typeof(WixPatchRootElement));
    }
  }

  [TestClass]
  public class MspModelCreationTypesTest
  {
    #region Вспомогательные члены.

    string oldWixout = "old.wixout";
    string newWixout = "new.wixout";

    string oldUpdzip = "old.updzip";
    string newUpdzip = "new.updzip";

    /// <summary>
    /// Файл для добавления к WixFileElement.
    /// </summary>
    string exeFile = "File.exe";

    [TestInitialize]
    public void TestInitialize()
    {
      File.WriteAllText(oldWixout, oldWixout);
      File.WriteAllText(newWixout, newWixout);
      File.Create(exeFile).Dispose();
    }

    [TestCleanup]
    public void TestCleanup()
    {
      if (File.Exists(oldWixout))
        File.Delete(oldWixout);
      if (File.Exists(newWixout))
        File.Delete(newWixout);

      if (File.Exists(oldUpdzip))
        File.Delete(oldUpdzip);
      if (File.Exists(newUpdzip))
        File.Delete(newUpdzip);

      if (File.Exists(exeFile))
        File.Delete(exeFile);
    }

    private void CreateProductUpdateInfos()
    {
      WixProduct oldProduct;
      WixProduct newProduct;

      // Создадим продукт старой версии. 
      // Продукт oldProduct содержит:
      //   Feature1
      //      Component11
      //      Component12
      //   Feature2
      //      Component21
      oldProduct = new WixProduct();
      WixFeatureElement feature = new WixFeatureElement { Id = "Feature1" };
      feature.Items.Add(new WixComponentElement { Id = "Component11" });
      feature.Items.Add(new WixComponentElement { Id = "Component12" });
      oldProduct.RootElement.Items.Add(feature);
      feature = new WixFeatureElement { Id = "Feature2" };
      feature.Items.Add(new WixComponentElement { Id = "Component21" });
      oldProduct.RootElement.Items.Add(feature);

      // Сохраним его и загрузим в продукт новой версии.
      // Изменим уже существующий и добавим один компонент.
      // Продукт newProduct содержит:
      //   Feature1
      //      Component11
      //      Component12 (изменён)
      //        File121
      //   Feature2
      //      Component21
      //      Component22 (добавлен)
      XmlSaverLoader.Save(oldProduct, "Product.xml");
      newProduct = XmlSaverLoader.Load<WixProduct>("Product.xml");
      newProduct.RootElement.Items[0].Items[1].Items.Add(new WixFileElement { Id = "File121", FileName = exeFile });
      newProduct.RootElement.Items[1].Items.Add(new WixComponentElement { Id = "Component22" });

      // Удалим файл, больше не нужен.
      File.Delete("Product.xml");

      WixProductUpdateInfo oldInfo = WixProductUpdateInfo.Create(oldProduct, oldWixout, "");
      WixProductUpdateInfo newInfo = WixProductUpdateInfo.Create(newProduct, newWixout, "");

      // При создании wixout удалится.
      oldInfo.Save(oldUpdzip, true);
      newInfo.Save(newUpdzip, true);
    }

    #endregion

    /// <summary>
    /// Тестовая модель для тестирования директорий.
    /// </summary>
    class TestMspModel : MspModel
    {
      public string TestOldDirectory { get { return Path.Combine(FileStore.StoreDirectory, BaseDirectory); } }
      public string TestNewDirectory { get { return Path.Combine(FileStore.StoreDirectory, TargetDirectory); } }
    }

    /// <summary>
    /// Проверка создания директори и распаковки файлов.
    /// </summary>
    [TestMethod]
    public void MspModelCreationTypesCheckDirectoryAndUnpacking()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      TestMspModel model = new TestMspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.AllInOne));

      // Файлы должны быть во временной директории.
      Assert.IsTrue(File.Exists(Path.Combine(model.TestOldDirectory, oldWixout)));
      Assert.IsFalse(File.Exists(Path.Combine(model.TestOldDirectory, oldUpdzip)));
      Assert.IsTrue(File.Exists(Path.Combine(model.TestNewDirectory, newWixout)));
      Assert.IsFalse(File.Exists(Path.Combine(model.TestNewDirectory, newUpdzip)));
    }

    /// <summary>
    /// Проверка компонент для обновления.
    /// </summary>
    [TestMethod]
    public void MspModelCreationTypesCheckUpdateComponents()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      MspModel model = new MspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.AllInOne));

      Assert.AreEqual(2, model.UpdateComponents.Length);
      foreach (string id in new string[] { "Component12", "Component22" })
      {
        Assert.IsTrue(model.UpdateComponents.Select(v => v.Id).Contains(id));
      }
    }

    /// <summary>
    /// Создание модели по типу все в одном, на основе файлов updzip.
    /// </summary>
    [TestMethod]
    public void MspModelCreationTypesAllInOne()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      MspModel model = new MspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.AllInOne));

      // Patch должен быть один.
      Assert.AreEqual(1, model.RootItem.Items.Count);
      Assert.AreEqual(typeof(WixPatchElement), model.RootItem.Items[0].GetType());
      // Должно быть два PatchComponent.
      Assert.AreEqual(2, model.RootItem.Items[0].Items.Count);
      Assert.AreEqual(typeof(WixPatchComponentElement), model.RootItem.Items[0].Items[0].GetType());
      Assert.AreEqual(typeof(WixPatchComponentElement), model.RootItem.Items[0].Items[1].GetType());
    }

    /// <summary>
    /// Создание модели по типу каждый компонент отдельно, на основе файлов updzip.
    /// </summary>
    [TestMethod]
    public void MspModelCreationTypesEachInOne()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      MspModel model = new MspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.EachInOne));

      // Patch должено быть два.
      Assert.AreEqual(2, model.RootItem.Items.Count);
      // В каждом Patch один Component.
      for (int i = 0; i < 2; i++)
      {
        Assert.AreEqual(typeof(WixPatchElement), model.RootItem.Items[i].GetType());
        Assert.AreEqual(1, model.RootItem.Items[i].Items.Count);
        Assert.AreEqual(typeof(WixPatchComponentElement), model.RootItem.Items[i].Items[0].GetType());
      }
    }

    /// <summary>
    /// Создание пустой модели.
    /// </summary>
    [TestMethod]
    public void MspModelCreationTypesEmpty()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      MspModel model = new MspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.Empty));

      Assert.AreEqual(0, model.RootItem.Items.Count);
    }

    /// <summary>
    /// Загрузка тестовой модели из файла и проверка ее состояния.
    /// </summary>
    [TestMethod]
    public void MspModelSaveAndLoad()
    {
      CreateProductUpdateInfos();

      // Создаем модель.
      TestMspModel model = new TestMspModel();
      model.Load(new MspModelLoadingParameters(oldUpdzip, newUpdzip, MspCreationTypes.AllInOne));

      model.Save("TestModel.mspzip");
      model.Dispose();

      model = new TestMspModel();
      model.Load("TestModel.mspzip");

      // Файлы должны быть во временной директории.
      Assert.IsTrue(File.Exists(Path.Combine(model.TestOldDirectory, oldWixout)));
      Assert.IsFalse(File.Exists(Path.Combine(model.TestOldDirectory, oldUpdzip)));
      Assert.IsTrue(File.Exists(Path.Combine(model.TestNewDirectory, newWixout)));
      Assert.IsFalse(File.Exists(Path.Combine(model.TestNewDirectory, newUpdzip)));

      // Проверка компонент для обновления.
      Assert.AreEqual(2, model.UpdateComponents.Length);
      foreach (string id in new string[] { "Component12", "Component22" })
      {
        Assert.IsTrue(model.UpdateComponents.Select(v => v.Id).Contains(id));
      }

      // Patch должен быть один.
      Assert.AreEqual(1, model.RootItem.Items.Count);
      Assert.AreEqual(typeof(WixPatchElement), model.RootItem.Items[0].GetType());
      // Должно быть два PatchComponent.
      Assert.AreEqual(2, model.RootItem.Items[0].Items.Count);
      Assert.AreEqual(typeof(WixPatchComponentElement), model.RootItem.Items[0].Items[0].GetType());
      Assert.AreEqual(typeof(WixPatchComponentElement), model.RootItem.Items[0].Items[1].GetType());

      File.Delete("TestModel.mspzip");
    }
  }
}

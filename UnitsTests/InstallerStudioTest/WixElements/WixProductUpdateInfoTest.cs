using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.WixElements;

namespace InstallerStudioTest.WixElements
{
  [TestClass]
  public class WixProductUpdateInfoTest
  {
    string fileName = "Test.zip";
    string wixoutFileName = "File.wixout";

    [TestInitialize]
    public void TestInitialize()
    {
      TestCleanup();
    }

    [TestCleanup]
    public void TestCleanup()
    {
      // Очистка.
      if (File.Exists(fileName))
        File.Delete(fileName);
      if (File.Exists(wixoutFileName))
        File.Delete(wixoutFileName);
    }

    /// <summary>
    /// Создание, сохранение и загрузка информации для обновления
    /// с дополнительными файлами.
    /// </summary>
    [TestMethod]
    public void WixProductUpdateInfoCreateSaveAndLoad()
    {
      WixProduct product = new WixProduct();
      product.Name = "Name";
      product.Manufacturer = "Manufacturer";
      product.Version = "1.2.3.4";
      WixProductUpdateInfo updateInfo = WixProductUpdateInfo.Create(product, wixoutFileName, "");

      Assert.IsNotNull(updateInfo);
      Assert.AreEqual(product.Id, updateInfo.Id);
      Assert.AreEqual(product.Name, updateInfo.Name);
      Assert.AreEqual(product.Manufacturer, updateInfo.Manufacturer);
      Assert.AreEqual(product.Version, updateInfo.Version);
      Assert.AreEqual(wixoutFileName, updateInfo.WixoutFileName);

      // Создадим wixout.
      File.WriteAllText(wixoutFileName, wixoutFileName);

      // Сохраним.
      updateInfo.Save(fileName);
      Assert.IsTrue(File.Exists(fileName));
      // Файл должен быть.
      Assert.IsTrue(File.Exists(wixoutFileName));

      // Удалим wixout.
      File.Delete(wixoutFileName);

      // Загрузим с файла и сравним.
      updateInfo = WixProductUpdateInfo.Load(fileName);
      Assert.IsNotNull(updateInfo);
      Assert.AreEqual(product.Id, updateInfo.Id);
      Assert.AreEqual(product.Name, updateInfo.Name);
      Assert.AreEqual(product.Manufacturer, updateInfo.Manufacturer);
      Assert.AreEqual(product.Version, updateInfo.Version);
      Assert.AreEqual(wixoutFileName, updateInfo.WixoutFileName);
      Assert.IsTrue(File.Exists(wixoutFileName));
      Assert.AreEqual(wixoutFileName, File.ReadAllText(wixoutFileName));
    }

    /// <summary>
    /// Автоматическое удаление файла wixout.
    /// </summary>
    [TestMethod]
    public void WixProductUpdateInfoAutoDelete()
    {
      WixProduct product = new WixProduct();
      WixProductUpdateInfo updateInfo = WixProductUpdateInfo.Create(product, wixoutFileName, "");

      // Создадим wixout.
      File.WriteAllText(wixoutFileName, wixoutFileName);

      // Сохраним.
      updateInfo.Save(fileName, true);

      // Файла не должно быть.
      Assert.IsFalse(File.Exists(wixoutFileName));
    }

    /// <summary>
    /// Указание полного пути у файла wixout.
    /// Должно сохраниться только имя.
    /// </summary>
    [TestMethod]
    public void WixProductUpdateInfoWixoutFullFileName()
    {
      WixProduct product = new WixProduct();
      WixProductUpdateInfo updateInfo = WixProductUpdateInfo.Create(product, Path.GetFullPath(wixoutFileName), "");

      // Создадим wixout.
      File.WriteAllText(wixoutFileName, wixoutFileName);

      // Сохраним.
      updateInfo.Save(fileName);

      File.Delete(wixoutFileName);

      // Загрузим.
      updateInfo = WixProductUpdateInfo.Load(fileName);
      Assert.AreEqual(wixoutFileName, updateInfo.WixoutFileName);
    }

    /// <summary>
    /// Распаковка в директории.
    /// </summary>
    [TestMethod]
    public void WixProductUpdateInfoLoadToDirectory()
    {
      string dir = "TestDir";

      WixProduct product = new WixProduct();
      WixProductUpdateInfo updateInfo = WixProductUpdateInfo.Create(product, wixoutFileName, "");

      // Создадим wixout.
      File.WriteAllText(wixoutFileName, wixoutFileName);

      // Создадим диреторию.
      Directory.CreateDirectory(dir);

      // Сохраним.
      updateInfo.Save(dir + "\\" + fileName);

      // Загрузим.
      updateInfo = WixProductUpdateInfo.Load(dir + "\\" + fileName);
      Assert.IsTrue(File.Exists(dir + "\\" + wixoutFileName));

      Directory.Delete(dir, true);
    }
  }
}

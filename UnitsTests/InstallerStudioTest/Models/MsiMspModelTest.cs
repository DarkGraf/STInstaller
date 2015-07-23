using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Models;
using InstallerStudio.WixElements;

namespace InstallerStudioTest.Models
{
  [TestClass]
  public class MsiMspModelTest
  {
    /// <summary>
    /// Проверка корневого элемента.
    /// </summary>
    [TestMethod]
    public void MsiModelRootElement()
    {
      BuilderModel model = new MsiModel();
      Assert.IsInstanceOfType(model.RootItem, typeof(WixFeatureElement));
    }

    /// <summary>
    /// Проверка корневого элемента.
    /// </summary>
    [TestMethod]
    public void MspModelRootElement()
    {
      BuilderModel model = new MspModel();
      Assert.IsInstanceOfType(model.RootItem, typeof(WixPatchFamilyElement));
    }

    /// <summary>
    /// Проверка получения метаданных команды.
    /// </summary>
    [TestMethod]
    public void MsiModelGetElementCommands()
    {
      BuilderModel model = new MsiModel();
      CommandMetadata[] metadata = model.GetElementCommands();
      Assert.IsNotNull(metadata);
      Assert.IsTrue(metadata.Length > 0);
    }

    /// <summary>
    /// Проверка выбранного элемента при создании.
    /// </summary>
    [TestMethod]
    public void MsiModelSelectedItemCreated()
    {
      BuilderModel model = new MsiModel();
      Assert.IsNull(model.SelectedItem);
    }

    /// <summary>
    /// Проверка выбранного элемента после инициализации добавленным элементом.
    /// </summary>
    [TestMethod]
    public void MsiModelSelectedItemInitializedSuccessful()
    {
      BuilderModel model = new MsiModel();
      IWixElement element = new WixFeatureElement();
      model.Items.Add(element);
      model.SelectedItem = element;
      Assert.AreEqual(element, model.SelectedItem);

      element = new WixFeatureElement();
      model.Items[0].Items.Add(element);
      model.SelectedItem = element;
      Assert.AreEqual(element, model.SelectedItem);
    }

    /// <summary>
    /// Проверка выбранного элемента после инициализации не добавленным элементом.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void MsiModelSelectedItemInitializedWrong()
    {
      BuilderModel model = new MsiModel();
      model.Items.Add(new WixFeatureElement());
      IWixElement element = new WixFeatureElement();
      model.SelectedItem = element;
      Assert.Fail();
    }

    /// <summary>
    /// Тест добавления элементов.
    /// </summary>
    [TestMethod]
    public void MsiModelAddItems()
    {
      MsiModel model = new MsiModel();
      // Запомним текущее количество.
      int count = model.RootItem.Items.Count;

      // Добавляем Feature к корневому элементу.
      IWixElement feature = model.AddItem(typeof(WixFeatureElement));
      Assert.AreEqual(++count, model.RootItem.Items.Count);
      Assert.IsNotNull(feature);
      Assert.AreEqual(feature, model.RootItem.Items[count - 1]);
      Assert.AreEqual(feature, model.SelectedItem);

      // Добавим компонент к корневому элементу.
      model.SelectedItem = null;
      IWixElement componetFirst = model.AddItem(typeof(WixComponentElement));
      Assert.AreEqual(++count, model.RootItem.Items.Count);
      Assert.IsNotNull(componetFirst);
      Assert.AreEqual(componetFirst, model.RootItem.Items[count - 1]);
      Assert.AreEqual(componetFirst, model.SelectedItem);

      // Добавим компонент к созданной Feature.
      model.SelectedItem = feature;
      IWixElement componetSecond = model.AddItem(typeof(WixComponentElement));
      Assert.AreEqual(1, feature.Items.Count);
      Assert.IsNotNull(componetSecond);
      Assert.AreEqual(componetSecond, feature.Items[0]);
      Assert.AreEqual(componetSecond, model.SelectedItem);

      // Добавим компонент к корневому элементу.
      model.SelectedItem = null;
      IWixElement componetThird = model.AddItem(typeof(WixComponentElement));
      Assert.AreEqual(++count, model.RootItem.Items.Count);
      Assert.IsNotNull(componetThird);
      Assert.AreEqual(componetThird, model.RootItem.Items[count - 1]);
      Assert.AreEqual(componetThird, model.SelectedItem);
    }

    [TestMethod]
    public void MsiModelDeleteItem()
    {
      MsiModel model = new MsiModel();
      // Запомним текущее количество.
      int count = model.RootItem.Items.Count;

      // Добавляем Feature к корневому элементу.
      IWixElement feature = model.AddItem(typeof(WixFeatureElement));
      Assert.AreEqual(++count, model.RootItem.Items.Count);
      Assert.AreEqual(feature, model.SelectedItem);

      // Добавляем Feature к корневому элементу.
      model.SelectedItem = null;
      feature = model.AddItem(typeof(WixFeatureElement));
      Assert.AreEqual(++count, model.RootItem.Items.Count);
      Assert.AreEqual(feature, model.SelectedItem);

      // Удаляем выделенный элемент.
      model.RemoveSelectedItem();
      Assert.AreEqual(--count, model.RootItem.Items.Count);
      Assert.IsNull(model.SelectedItem);
    }
  }
}

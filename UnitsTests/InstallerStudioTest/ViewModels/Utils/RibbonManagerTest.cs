using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.ViewModels.Utils;

namespace InstallerStudioTest.ViewModels.Utils
{
  [TestClass]
  public class RibbonManagerTest
  {
    [TestMethod]
    public void RibbonManagerDefaultCategory()
    {
      IRibbonManager manager = new RibbonManager();

      // По умолчанию категория должна быть.
      Assert.IsNotNull(manager.DefaultCategory);
      Assert.AreEqual(1, manager.Categories.Count);
      Assert.IsInstanceOfType(manager.DefaultCategory, typeof(RibbonDefaultCategory));
    }

    [TestMethod]
    public void RibbonManagerSimple()
    {
      IRibbonManager manager = new RibbonManager();

      IRibbonCategory customCategory = manager.Add("Custom Category");
      IRibbonPage page = customCategory.Add("Page");
      IRibbonGroup group = page.Add("Group");
      IRibbonButton button = group.Add("Button", null, RibbonButtonType.Large);

      Assert.AreEqual(2, manager.Categories.Count);
      Assert.AreEqual(1, customCategory.Pages.Count);
      Assert.AreEqual(1, page.Groups.Count);
      Assert.AreEqual(1, group.Buttons.Count);

      Assert.IsInstanceOfType(manager.Categories[0], typeof(RibbonDefaultCategory));
      Assert.IsInstanceOfType(manager.Categories[1], typeof(RibbonCustomCategory));
    }

    [TestMethod]
    public void RibbonManagerWithTransactions()
    {
      IRibbonManager manager = new RibbonManager();

      IRibbonPage page1 = manager.DefaultCategory.Add("Page1");
      IRibbonGroup group1 = page1.Add("Group1");
      IRibbonButton button1 = group1.Add("Button1", null, RibbonButtonType.Large);

      Assert.AreEqual(1, manager.Categories.Count);
      Assert.AreEqual(1, manager.DefaultCategory.Pages.Count);
      Assert.AreEqual(1, page1.Groups.Count);
      Assert.AreEqual(1, group1.Buttons.Count);

      // Начинаем транзакцию.
      manager.BeginTransaction("Level 1");

      IRibbonCategory category2 = manager.Add("Category2");
      IRibbonPage page2 = manager.DefaultCategory.Add("Page2");
      IRibbonGroup group2 = page1.Add("Group2");
      IRibbonButton button2 = group1.Add("Button2", null, RibbonButtonType.Large);

      Assert.AreEqual(2, manager.Categories.Count);
      Assert.AreEqual(2, manager.DefaultCategory.Pages.Count);
      Assert.AreEqual(2, page1.Groups.Count);
      Assert.AreEqual(2, group1.Buttons.Count);

      // Отменяем транзакцию.
      manager.RollbackTransaction("Level 1");

      Assert.AreEqual(1, manager.Categories.Count);
      Assert.AreEqual(1, manager.DefaultCategory.Pages.Count);
      Assert.AreEqual(1, page1.Groups.Count);
      Assert.AreEqual(1, group1.Buttons.Count);
    }
  }

  [TestClass]
  public class RibbonTransactionTest
  {
    [TestMethod]
    public void RibbonTransactionTesting()
    {
      ObservableCollection<string> collection = new ObservableCollection<string>();
      RibbonTransaction transaction = new RibbonTransaction(collection);

      // Добавляем без транзакции.
      collection.Add("aaa");
      collection.Add("bbb");
      Assert.AreEqual(2, collection.Count);
      foreach (string str in new string[] { "aaa", "bbb" })
        collection.Contains(str);

      // Начинаем транзакцию первого уровня.
      transaction.Begin("Level1");

      collection.Add("ccc");
      collection.Add("ddd");
      Assert.AreEqual(4, collection.Count);
      foreach (string str in new string[] { "aaa", "bbb", "ccc", "ddd" })
        collection.Contains(str);

      // Начинаем транзакцию второго уровня.
      transaction.Begin("Level2");

      collection.Add("eee");
      Assert.AreEqual(5, collection.Count);
      foreach (string str in new string[] { "aaa", "bbb", "ccc", "ddd", "eee" })
        collection.Contains(str);

      // Отменяем транзакцию второго уровня
      transaction.Rollback("Level2");

      Assert.AreEqual(4, collection.Count);
      foreach (string str in new string[] { "aaa", "bbb", "ccc", "ddd" })
        collection.Contains(str);

      // Отменяем транзакцию первого уровня

      transaction.Rollback("Level1");
      Assert.AreEqual(2, collection.Count);
      foreach (string str in new string[] { "aaa", "bbb" })
        collection.Contains(str);
    }
  }
}

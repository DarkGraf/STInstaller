using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class ZipFileStoreTest
  {
    [TestMethod]
    public void ZipFileStoreSave()
    {
      IFileStore store = new ZipFileStore();

      try
      {
        // Создаем и добавляем в хранилище файл.
        File.WriteAllText("TestPi.txt", "3.1415926");
        store.AddFile("TestPi.txt", "TestPi.txt");
        File.WriteAllText("TestE.txt", "2.718281");
        store.AddFile("TestE.txt", "Common\\Obj\\TestE.txt");

        store.Save("Test.zip");

        Assert.IsTrue(File.Exists("Test.zip"));
      }
      finally
      {
        if (File.Exists("TestPi.txt"))
          File.Delete("TestPi.txt");
        if (File.Exists("TestE.txt"))
          File.Delete("TestE.txt");
        if (File.Exists("Test.zip"))
          File.Delete("Test.zip");
      }
    }

    [TestMethod]
    public void ZipFileStoreOpen()
    {
      IFileStore store = new ZipFileStore();

      try
      {
        // Создаем и добавляем в хранилище файл.
        File.WriteAllText("TestPi.txt", "3.1415926");
        store.AddFile("TestPi.txt", "TestPi.txt");
        File.WriteAllText("TestE.txt", "2.718281");
        store.AddFile("TestE.txt", "Common\\Obj\\TestE.txt");
      }
      finally
      {
        if (File.Exists("TestPi.txt"))
          File.Delete("TestPi.txt");
        if (File.Exists("TestE.txt"))
          File.Delete("TestE.txt");
      }

      try
      {
        store.Save("Test.zip");
        store = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        store = new ZipFileStore("Test.zip");
        Assert.IsTrue(File.Exists(store.StoreDirectory + "\\TestPi.txt"));
        Assert.IsTrue(File.Exists(store.StoreDirectory + "\\Common\\Obj\\TestE.txt"));
        Assert.AreEqual(2, store.Files.Count);
        Assert.IsNotNull(store.Files.FirstOrDefault(v => v == "TestPi.txt"));
        Assert.IsNotNull(store.Files.FirstOrDefault(v => v == "Common\\Obj\\TestE.txt"));
      }
      finally
      {
        if (File.Exists("Test.zip"))
          File.Delete("Test.zip");
      }
    }
  }
}

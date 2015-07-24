using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;
using InstallerStudio.WixElements;
using InstallerStudio.Models;

namespace InstallerStudioTest.Models
{
  [TestClass]
  public class FileStoreSynchronizerTest
  {
    [TestMethod]
    public void FileStoreSynchronizerAdd()
    {
      IFileStore store = new ZipFileStore();
      FileSupportEventArgs e;

      try
      {
        // Для добавления в хранилище, передаем файл с полным путем.
        e = new FileSupportEventArgs(null, null, Path.Combine(Environment.CurrentDirectory, "TestPi.txt"), "SystemFolder");
        File.WriteAllText("TestPi.txt", "3.1415926");
        FileStoreSynchronizer.Synchronize(store, e);
        Assert.AreEqual(1, store.Files.Count);
        Assert.AreEqual("TestPi.txt", store.Files[0]);
      }
      finally
      {
        if (File.Exists("TestPi.txt"))
          File.Delete("TestPi.txt");
      }
    }
  }
}

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
    #region Вспомогательные элементы.

    static string fileNamePi = "TestPi.txt";
    static string fileNameE = "TestE.txt";
    static string pathPi = Path.Combine(Environment.CurrentDirectory, fileNamePi);
    static string pathE = Path.Combine(Environment.CurrentDirectory, fileNameE);
    static string contentPi = "3.1415926";
    static string contentE = "2.718281";
    IFileStore store = new ZipFileStore();

    [TestCleanup]
    public void TestCleanup()
    {
      if (File.Exists(fileNamePi))
        File.Delete(fileNamePi);
      if (File.Exists(fileNameE))
        File.Delete(fileNameE);
    }

    #endregion

    /// <summary>
    /// Добавление нового файла с полным путем.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerAddFullPath()
    {
      File.WriteAllText(pathPi, contentPi);

      // Для добавления в хранилище, передаем файл с полным путем. 
      // Установочная директория отсутствует.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, fileNamePi, null, pathPi);
        
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.AreEqual(fileNamePi, store.Files[0]);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileNamePi)));
    }

    /// <summary>
    /// Добавление сначала директории, потом файла с полным путем.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerAddDirectoryAndFullPath()
    {
      File.WriteAllText(pathPi, contentPi);

      string directory = "SystemFolder";

      // Иммитируем сначала добавление директории.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, null, directory, null);

      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(0, store.Files.Count);

      // Добавляем файл.
      e = new FileSupportEventArgs(null, directory, fileNamePi, directory, pathPi);

      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.AreEqual(Path.Combine(directory, fileNamePi), store.Files[0]);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, directory, fileNamePi)));
    }

    /// <summary>
    /// Удаление файла.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerDeleting()
    {
      File.WriteAllText(pathPi, contentPi);

      // Добавим файл.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, fileNamePi, null, pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      // Удалим файл.
      e = new FileSupportEventArgs(fileNamePi, null,  null, null, null);
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(0, store.Files.Count);
      Assert.IsFalse(File.Exists(Path.Combine(store.StoreDirectory, fileNamePi)));
    }

    /// <summary>
    /// Переименование файла.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerRename()
    {
      File.WriteAllText(pathPi, contentPi);

      // Добавим файл.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, fileNamePi, null, pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      // Переименуем файл.
      e = new FileSupportEventArgs(fileNamePi, null, fileNameE, null, fileNameE);
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileNameE)));
    }

    /// <summary>
    /// Перемещение в другой каталог.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerMove()
    {
      File.WriteAllText(pathPi, contentPi);

      // Добавим файл.
      FileSupportEventArgs e = new FileSupportEventArgs(null, "SystemFolder", fileNamePi, "SystemFolder", pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      // Переместим в другой каталог.
      e = new FileSupportEventArgs(fileNamePi, "SystemFolder", fileNamePi, "ProductFolder", fileNamePi);
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, "ProductFolder", fileNamePi)));
    }

    /// <summary>
    /// Замена файла с тем же именем.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerReplaceSameName()
    {
      File.WriteAllText(pathPi, contentPi);

      // Добавим файл.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, fileNamePi, null, pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      // Заменим файл другим содержимым.
      File.WriteAllText(pathPi, contentE);
      e = new FileSupportEventArgs(fileNamePi, null, fileNamePi, null, pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileNamePi)));
      Assert.AreEqual(contentE, File.ReadAllText(Path.Combine(store.StoreDirectory, fileNamePi)));
    }

    /// <summary>
    /// Замена файла с другим именем.
    /// </summary>
    [TestMethod]
    public void FileStoreSynchronizerReplaceOtherName()
    {
      File.WriteAllText(pathPi, contentPi);

      // Добавим файл.
      FileSupportEventArgs e = new FileSupportEventArgs(null, null, fileNamePi, null, pathPi);
      FileStoreSynchronizer.Synchronize(store, e);
      // Заменим файл другим.
      File.WriteAllText(pathE, contentE);
      e = new FileSupportEventArgs(fileNamePi, null, fileNameE, null, pathE);
      FileStoreSynchronizer.Synchronize(store, e);
      Assert.AreEqual(1, store.Files.Count);
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileNameE)));
      Assert.AreEqual(contentE, File.ReadAllText(Path.Combine(store.StoreDirectory, fileNameE)));
    }
  }
}

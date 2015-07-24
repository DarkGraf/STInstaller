using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;
using System.Collections.Generic;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class TempFileStoreTest
  {
    /// <summary>
    /// Создание и удаления временной директории.
    /// </summary>
    [TestMethod]
    public void TempFileStoreCreateAndDestroy()
    {
      IFileStore store = new TempFileStore();
      Assert.IsFalse(string.IsNullOrWhiteSpace(store.StoreDirectory));
      Assert.IsTrue(Directory.Exists(store.StoreDirectory));

      string storeDirectory = store.StoreDirectory;
      store = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Assert.IsFalse(Directory.Exists(storeDirectory));
    }

    [TestMethod]
    public void TempFileStoreAdd()
    {
      IFileStore store = new TempFileStore();

      Assert.AreEqual(0, store.Files.Count);

      // Переменная для делегата.
      int count = 0;
      // Делегат для теста
      Action<string, string, string> action = (path, relativePath, content) =>
        {
          string fileName;

          // Создаем и добавляем в хранилище файл.
          File.WriteAllText(path, content);
          store.AddFile(path, relativePath);

          // Проверяем количество файлов в хранилище.
          Assert.AreEqual(++count, store.Files.Count);
          // В коллекции последний элемент должен быть с новым путем.
          Assert.AreEqual(relativePath, store.Files[count - 1]);
          // Во временном хранилище должен быть добавленный файл.
          Assert.IsTrue(File.Exists(fileName = Path.Combine(store.StoreDirectory, relativePath)));
          // Его содержимое должно быть такое же, как и у оригинального файла.
          Assert.AreEqual(content, File.ReadAllText(fileName));

          // Удалим исходный файл.
          if (File.Exists(path))
            File.Delete(path);
        };

      // Добавляем первый файл.
      action("TestPi.txt", "TestPi.txt", "3.1415926");

      // Добавляем второй файл.
      action(Path.Combine(Environment.CurrentDirectory, "TestE.txt"), "Common\\Obj\\TestE.txt", "2.718281");
      
      // Добавим третий файл прямо в хранилище.
      action(Path.Combine(store.StoreDirectory, "TestPhi.txt"), "TestPhi.txt", "1.618034");
    }

    [TestMethod]
    public void TempFileStoreDelete()
    {
      IFileStore store = new TempFileStore();

      string fileName = "TestPi.txt";

      // Создаем и добавляем в хранилище файл.
      File.WriteAllText(fileName, "3.1415926");
      store.AddFile(fileName, fileName);

      // Во временном хранилище должен быть добавленный файл.
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileName)));

      // Удаляем файл из хранилища.
      store.DeleteFile(fileName);

      // Проверяем количество файлов в хранилище.
      Assert.AreEqual(0, store.Files.Count);
      // Во временном хранилище не должно быть добавленного файла.
      Assert.IsFalse(File.Exists(Path.Combine(store.StoreDirectory, fileName)));

      // Удалим исходный файл.
      if (File.Exists(fileName))
        File.Delete(fileName);
    }

    [TestMethod]
    public void TempFileStoreReplace()
    {
      IFileStore store = new TempFileStore();

      string fileName = "TestPi.txt";

      // Создаем и добавляем в хранилище файл.
      File.WriteAllText(fileName, "3.1415926");
      store.AddFile(fileName, fileName);

      // Во временном хранилище должен быть добавленный файл.
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileName)));

      // Создадим новый файл.
      File.WriteAllText(fileName, "Pi=3.1415926");

      // Заменяем файл в хранилище.
      store.ReplaceFile(fileName, fileName);

      // Проверяем количество файлов в хранилище.
      Assert.AreEqual(1, store.Files.Count);
      // В коллекции последний элемент должен быть тот же.
      Assert.AreEqual(fileName, store.Files[0]);
      // Во временном хранилище должен быть добавленный файл.
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, fileName)));
      // Проверим содержимое.
      Assert.AreEqual("Pi=3.1415926", File.ReadAllText(Path.Combine(store.StoreDirectory, fileName)));

      // Удалим исходный файл.
      if (File.Exists(fileName))
        File.Delete(fileName);
    }

    [TestMethod]
    public void TempFileStoreStateTest()
    {
      IFileStore store = new TempFileStore();
      Assert.AreEqual(FileStoreState.Changed, store.State);

      store.Save("");
      Assert.AreEqual(FileStoreState.Saved, store.State);

      string fileName = "TestPi.txt";

      // Создаем и добавляем в хранилище файл.
      File.WriteAllText(fileName, "3.1415926");
      store.AddFile(fileName, fileName);
      Assert.AreEqual(FileStoreState.Changed, store.State);

      if (File.Exists(fileName))
        File.Delete(fileName);
    }

    /// <summary>
    /// Удаление временных директорий в архиве, когда в них нет файлов.
    /// </summary>
    [TestMethod]
    public void TempFileStoreDeleteEmptyDirectory()
    {
      IFileStore store = new TempFileStore();

      var files = new[] 
      { 
        new { ActualPath = "Test1.txt", RelativePath = "Test1.txt" },
        new { ActualPath = "Test2.txt", RelativePath = "Dir1\\Test2.txt" },
        new { ActualPath = "Test3.txt", RelativePath = "Dir1\\Dir2\\Dir3\\Test3.txt" },
        new { ActualPath = "Test4.txt", RelativePath = "Dir1\\Dir2\\Test4.txt" }
      };

      try
      {

        // Создаем файлы и добавляем во временную директорию.
        foreach (var file in files)
        {
          File.WriteAllText(file.ActualPath, "");
          store.AddFile(file.ActualPath, file.RelativePath);
        }

        for (int i = 3; i >= 0; i--)
        {
          store.DeleteFile(files[i].RelativePath);

          string directory = store.StoreDirectory;
          if (i == 3)
          {
            Assert.IsTrue(Directory.Exists(directory + "\\Dir1\\Dir2\\Dir3"));
          }
          else if (i == 2)
          {
            Assert.IsFalse(Directory.Exists(directory + "\\Dir1\\Dir2"));
            Assert.IsTrue(Directory.Exists(directory + "\\Dir1"));
          }
          else if (i == 1)
          {
            Assert.IsFalse(Directory.Exists(directory + "\\Dir1"));
            Assert.IsTrue(Directory.Exists(store.StoreDirectory));
          }
          else if (i == 0)
          {
            Assert.IsTrue(Directory.Exists(store.StoreDirectory));
          }
        }
      }
      finally
      {
        foreach (var file in files)
          if (File.Exists(file.ActualPath))
            File.Delete(file.ActualPath);
      }
    }

    /// <summary>
    /// Перемещения файла внутри хранилища.
    /// </summary>
    [TestMethod]
    public void TempFileStoreMove()
    {
      IFileStore store = new TempFileStore();

      string fileName = "TestPi.txt";

      // Создаем и добавляем в хранилище файл.
      File.WriteAllText(fileName, "3.1415926");
      store.AddFile(fileName, fileName);

      // Перемещаем в каталог внутри хранилища.
      store.MoveFile("TestPi.txt", "1\\TestPi.txt");

      Assert.AreEqual(FileStoreState.Changed, store.State);
      Assert.IsFalse(File.Exists(Path.Combine(store.StoreDirectory, "TestPi.txt")));
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, "1\\TestPi.txt")));
      Assert.AreEqual(1, store.Files.Count);
      Assert.AreEqual("1\\TestPi.txt", store.Files[0]);

      // Перемещаем в другой каталог внутри хранилища.
      store.MoveFile("1\\TestPi.txt", "2\\TestPi.txt");

      Assert.IsFalse(Directory.Exists(Path.Combine(store.StoreDirectory, "1")));
      Assert.IsTrue(File.Exists(Path.Combine(store.StoreDirectory, "2\\TestPi.txt")));
    }
  }
}

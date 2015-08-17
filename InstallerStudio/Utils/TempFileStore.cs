using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace InstallerStudio.Utils
{
  class TempFileStore : TempFileStoreBase, IFileStore
  {
    private List<string> files;
    private IFileWorker FileWorker;

    /// <summary>
    /// Создает экземпляр класса TempFileStore.
    /// </summary>
    /// <param name="silentWork">Признак работы без диалога.</param>
    public TempFileStore(bool silentWork = true) : base()
    {
      FileWorker = silentWork ? (IFileWorker)new SilentFileWorker() : (IFileWorker)new InteractiveFileWorker();

      files = new List<string>();
      State = FileStoreState.Changed;
    }

    /// <summary>
    /// Складывает временный путь и имя передаваемого файла.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    protected string ConvertToTempPath(string relativePath)
    {
      return Path.Combine(StoreDirectory, relativePath);
    }

    /// <summary>
    /// Проверка и создание директории во временном каталоге.
    /// </summary>
    /// <param name="relativePath"></param>
    protected void CheckAndCreateDirectory(string relativePath)
    {
      // Если в параметре присутствует относительная директория для временного хранилища,
      // и директория физически отсутствует, то создадим её.
      string relativeDirectory = Path.GetDirectoryName(relativePath);
      if (!string.IsNullOrEmpty(relativeDirectory) && !Directory.Exists(relativeDirectory = Path.Combine(StoreDirectory, relativeDirectory)))
      {
        Directory.CreateDirectory(relativeDirectory);
      }
    }

    protected void DeleteDirectoryIfEmpty(string relativeDirectory)
    {
      // Проверяем, если директория, где находился файл пустая, то удаляем (кроме корневой).
      string directory = Path.Combine(StoreDirectory, relativeDirectory);
      while (directory != StoreDirectory)
      {
        if (Directory.EnumerateFileSystemEntries(directory).Count() != 0)
          break;
        Directory.Delete(directory);
        directory = Path.GetDirectoryName(directory);
      }
    }

    #region IFilesStore

    public IReadOnlyList<string> Files
    {
      get { return files.AsReadOnly(); }
    }

    public virtual void Save(string path)
    {
      State = FileStoreState.Saved;
    }

    public void AddFile(string path, string relativePath)
    {
      // Файл должен физически существовать и не быть в коллекции.
      if (FileWorker.Exists(path) && files.FirstOrDefault(v => v == relativePath) == null)
      {
        CheckAndCreateDirectory(relativePath);

        files.Add(relativePath);
        // Обрабатываем ситуацию, когда пользователь сам создал файл в хранилище
        // и теперь его добавляет. В этом случае добавления в коллекцию достаточно,
        // файл не копируем.
        if (path != ConvertToTempPath(relativePath))
          FileWorker.Copy(path, ConvertToTempPath(relativePath));
        State = FileStoreState.Changed;
      }
    }

    public void DeleteFile(string relativePath)
    {
      // Файл должен физически существовать и быть в коллекции.
      if (FileWorker.Exists(ConvertToTempPath(relativePath)) && files.FirstOrDefault(v => v == relativePath) != null)
      {
        files.Remove(relativePath);
        FileWorker.Delete(ConvertToTempPath(relativePath));

        // Удаляем директорию, если она пустая.
        DeleteDirectoryIfEmpty(Path.GetDirectoryName(relativePath));

        State = FileStoreState.Changed;
      }
    }

    public void ReplaceFile(string relativePath, string path)
    {
      // Файл должен физически существовать и быть в коллекции.
      if (FileWorker.Exists(path) && FileWorker.Exists(ConvertToTempPath(relativePath)) && files.FirstOrDefault(v => v == relativePath) != null)
      {
        // Не дадим заменить самого себя.
        if (path != ConvertToTempPath(relativePath))
        {
          FileWorker.Copy(path, ConvertToTempPath(relativePath), true);
          State = FileStoreState.Changed;
        }
      }
    }

    public void MoveFile(string oldRelativePath, string newRelativePath)
    {
      string oldPath = ConvertToTempPath(oldRelativePath);
      string newPath = ConvertToTempPath(newRelativePath);
      if (FileWorker.Exists(oldPath) && files.FirstOrDefault(v => v == oldRelativePath) != null
        && files.FirstOrDefault(v => v == newRelativePath) == null)
      {
        CheckAndCreateDirectory(newRelativePath);
        FileWorker.Move(oldPath, newPath);
        DeleteDirectoryIfEmpty(Path.GetDirectoryName(oldRelativePath));

        files.Remove(oldRelativePath);
        files.Add(newRelativePath);

        State = FileStoreState.Changed;
      }
    }

    public FileStoreState State { get; private set; }

    #endregion
  }

  interface IFileWorker
  {
    bool Exists(string path);
    void Copy(string sourceFileName, string destFileName);
    void Copy(string sourceFileName, string destFileName, bool overwrite);
    void Move(string sourceFileName, string destFileName);
    void Delete(string path);
  }

  /// <summary>
  /// Работа с файлами без диалоговых окон.
  /// </summary>
  class SilentFileWorker : IFileWorker
  {
    public bool Exists(string path)
    {
      return File.Exists(path);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
      File.Copy(sourceFileName, destFileName);
    }

    public void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
      File.Copy(sourceFileName, destFileName, overwrite);
    }

    public void Move(string sourceFileName, string destFileName)
    {
      File.Move(sourceFileName, destFileName);
    }

    public void Delete(string path)
    {
      File.Delete(path);
    }
  }

  /// <summary>
  /// Работа с файлами с диалоговыми оконами.
  /// Требует подключение сборки Microsoft.VisualBasic.dll.
  /// </summary>
  class InteractiveFileWorker : IFileWorker
  {
    public bool Exists(string path)
    {
      return FileSystem.FileExists(path);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
      FileSystem.CopyFile(sourceFileName, destFileName, UIOption.AllDialogs);
    }

    public void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
      // Чтобы не запрашивать пользователя о замене файла, удалим его.
      if (FileSystem.FileExists(destFileName))
        FileSystem.DeleteFile(destFileName);
      FileSystem.CopyFile(sourceFileName, destFileName, UIOption.AllDialogs);
    }

    public void Move(string sourceFileName, string destFileName)
    {
      FileSystem.MoveFile(sourceFileName, destFileName);
    }

    public void Delete(string path)
    {
      FileSystem.DeleteFile(path);
    }
  }
}

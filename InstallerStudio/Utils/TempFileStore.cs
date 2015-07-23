using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InstallerStudio.Utils
{
  class TempFileStore : IFileStore
  {
    private List<string> files;

    public TempFileStore()
    {
      StoreDirectory = Path.Combine(Path.GetTempPath(), "ST" + Path.GetRandomFileName());
      Directory.CreateDirectory(StoreDirectory);
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

    #region IFilesStore

    public string StoreDirectory { get; private set; }

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
      if (File.Exists(path) && files.FirstOrDefault(v => v == relativePath) == null)
      {
        CheckAndCreateDirectory(relativePath);

        files.Add(relativePath);
        // Обрабатываем ситуацию, когда пользователь сам создал файл в хранилище
        // и теперь его добавляет. В этом случае добавления в коллекцию достаточно,
        // файл не копируем.
        if (path != ConvertToTempPath(relativePath))
          File.Copy(path, ConvertToTempPath(relativePath));
        State = FileStoreState.Changed;
      }
    }

    public void DeleteFile(string relativePath)
    {
      // Файл должен физически существовать и быть в коллекции.
      if (File.Exists(ConvertToTempPath(relativePath)) && files.FirstOrDefault(v => v == relativePath) != null)
      {
        files.Remove(relativePath);
        File.Delete(ConvertToTempPath(relativePath));

        // Проверяем, если директория, где находился файл пустая, то удаляем (кроме корневой).
        string directory = Path.GetDirectoryName(ConvertToTempPath(relativePath));
        while (directory != StoreDirectory)
        {
          if (Directory.EnumerateFileSystemEntries(directory).Count() != 0)
            break;
          Directory.Delete(directory);
          directory = Path.GetDirectoryName(directory);
        }

        State = FileStoreState.Changed;
      }
    }

    public void ReplaceFile(string relativePath, string path)
    {
      // Файл должен физически существовать и быть в коллекции.
      if (File.Exists(ConvertToTempPath(relativePath)) && files.FirstOrDefault(v => v == relativePath) != null)
      {
        // Не дадим заменить самого себя.
        if (path != ConvertToTempPath(relativePath))
        {
          File.Copy(path, ConvertToTempPath(relativePath), true);
          State = FileStoreState.Changed;
        }
      }
    }

    public FileStoreState State { get; private set; }

    #endregion

    #region IDisposable
    
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Очистка объекта.

    bool disposed = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing">Если true, то освободить управляемые ресурсы.</param>
    private void Dispose(bool disposing)
    {
      if (!disposed)
      {
        if (disposing)
        {
          // Освободить управляемые ресурсы.
        }

        // Освободить неуправляемые ресурсы.
        if (Directory.Exists(StoreDirectory))
          Directory.Delete(StoreDirectory, true);
      }
      disposed = true;
    }

    ~TempFileStore()
    {
      Dispose(false);
    }


    #endregion
  }
}

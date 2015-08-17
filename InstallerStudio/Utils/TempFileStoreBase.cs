using System;
using System.IO;

namespace InstallerStudio.Utils
{
  class TempFileStoreBase : IDisposable
  {
    public TempFileStoreBase()
    {
      StoreDirectory = Path.Combine(Path.GetTempPath(), "ST" + Path.GetRandomFileName());
      Directory.CreateDirectory(StoreDirectory);
    }

    public string StoreDirectory { get; private set; }

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

    ~TempFileStoreBase()
    {
      Dispose(false);
    }


    #endregion
  }
}

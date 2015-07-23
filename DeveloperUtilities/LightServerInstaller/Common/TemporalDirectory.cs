using System;
using System.IO;

namespace LightServerInstaller.Common
{
  class TemporalDirectory : IDisposable
  {
    public TemporalDirectory()
    {
      TempDirectory = Path.Combine(ParentDirectory, PrefixDirectory + Path.GetRandomFileName());
      Directory.CreateDirectory(TempDirectory);
    }

    public string TempDirectory { get; private set; }

    protected virtual string ParentDirectory
    {
      get { return Path.GetTempPath(); }
    }

    protected virtual string PrefixDirectory
    {
      get { return string.Empty; }
    }

    #region IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Очистка объекта

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
        if (Directory.Exists(TempDirectory))
          Directory.Delete(TempDirectory, true);
      }
      disposed = true;
    }

    ~TemporalDirectory()
    {
      Dispose(false);
    }

    #endregion
  }

}

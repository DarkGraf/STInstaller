using System;
using System.IO;
using System.IO.Compression;

namespace InstallerStudio.Utils
{
  class ZipFileStore : TempFileStore
  {
    public ZipFileStore(bool silentWork = true) : base(silentWork) { }

    public ZipFileStore(string path, bool silentWork = true) : base(silentWork) 
    {
      using (ZipArchive archive = ZipFile.OpenRead(path))
      {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
          // Использовать напрямую метод entry.ExtractToFile(StoreDirectory) нельзя,
          // выбрасывается исключение UnauthorizedAccessException.
          using (Stream stream = entry.Open())
          {
            CheckAndCreateDirectory(entry.FullName);
            using (FileStream fileStream = new FileStream(ConvertToTempPath(entry.FullName), FileMode.Create))
            {
              stream.CopyTo(fileStream);
            }
          }
          AddFile(ConvertToTempPath(entry.FullName), entry.FullName);
        }
      } 
    }

    #region TempFileStore

    public override void Save(string path)
    {
      base.Save(path);

      if (File.Exists(path))
        File.Delete(path);
      ZipFile.CreateFromDirectory(StoreDirectory, path);
    }

    #endregion
  }
}

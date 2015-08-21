using System;
using System.IO;
using System.Linq;

using InstallerStudio.WixElements;

namespace InstallerStudio.Utils
{
  static class FileStoreSynchronizer
  {
    public static void Synchronize(IFileStore store, FileSupportEventArgs e)
    {
      // Если не известно имя файла, выходим.
      if (string.IsNullOrEmpty(e.ActualFileName) && string.IsNullOrEmpty(e.OldFileName))
        return;

      // Формируем путь к файлу в хранилище с учетом устанавливаемой директории.
      // Если имя файла не известно, оно не должно использоваться ниже.
      string actualRelativePath = e.ActualFileName != null ? Path.Combine(e.ActualDirectory ?? "", e.ActualFileName) : null;
      string oldRelativePath = e.OldFileName != null ? Path.Combine(e.OldDirectory ?? "", e.OldFileName) : null;

      // За один вызов может измениться либо имя файла, либо директория.

      // Новый файл.
      // Если в "сырых данных" указано имя файла с полным путем и старое имя пустое, то значит это новый файл.
      if (Path.IsPathRooted(e.RawFileName) && string.IsNullOrEmpty(e.OldFileName) && File.Exists(e.RawFileName))
      {
        store.AddFile(e.RawFileName, actualRelativePath);
      }
      // Удаление файла.
      // Если актуальное имя пустое, а старое есть в хранилище, удалим файл.
      else if (string.IsNullOrEmpty(e.ActualFileName) && store.Files.Contains(oldRelativePath))
      {
        store.DeleteFile(oldRelativePath);
      }
      // Переименование файла.
      else if (!string.IsNullOrEmpty(e.ActualFileName) && !string.IsNullOrEmpty(e.OldFileName)
        && e.ActualFileName != e.OldFileName && e.ActualFileName == e.RawFileName)
      {
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
      // Перемещение файла в другой каталог.
      else if (e.ActualFileName == e.OldFileName && e.ActualDirectory != e.OldDirectory && e.ActualFileName == e.RawFileName)
      {
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
      // Замена на файл с тем же именем.
      else if (Path.IsPathRooted(e.RawFileName) && !string.IsNullOrEmpty(e.OldFileName) && !string.IsNullOrEmpty(e.ActualFileName)
        && e.ActualDirectory == e.OldDirectory && File.Exists(e.RawFileName))
      {
#warning Если ввести несуществующий файл, потом заменить на существующий, то файла не будет в хранилище, не заменится.
        // Заменяем файл, имя остается тем же.
        store.ReplaceFile(oldRelativePath, e.RawFileName);
        // Меняем имя файла.
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
    }
  }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;

using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Класс предназначен для сохранения и загрузки информации необходимой
  /// для формирования патча экземпляра класса WixProduct.
  /// Содержит хеши каждого IWixElement.
  /// </summary>
  [DataContract(Namespace = StringResources.Namespace)]
  class WixProductUpdateInfo
  {
    public const string FilenameExtension = "updzip";

    /// <summary>
    /// Имя файла в архиве содержащий информацию обновления.
    /// </summary>
    private const string InfoXml = "Info.xml";
    /// <summary>
    /// Имя файла в архиве содержащий wixout.
    /// </summary>
    private const string InfoWixout = "Info.wixout";

    // Создаем экземпляр через фабричный метод.
    protected WixProductUpdateInfo() { }

    [DataMember]
    public Guid Id { get; protected set; }

    [DataMember]
    public string Name { get; protected set; }

    [DataMember]
    public string Manufacturer { get; protected set; }

    [DataMember]
    public AppVersion Version { get; protected set; }

    [DataMember]
    public WixMD5ElementHash[] Hashes { get; protected set; }

    /// <summary>
    /// Связанный файл wixout для построения msi.
    /// </summary>
    [DataMember]
    public string WixoutFileName { get; protected set; }

    /// <summary>
    /// Создает объект. Фабричный метод.
    /// </summary>
    public static WixProductUpdateInfo Create(WixProduct product, string wixoutFileName, string baseDirectory)
    {
      WixMD5ElementHash[] hashes = product.RootElement.Items.Descendants().Select(v => v.GetMD5(baseDirectory)).ToArray();

      return new WixProductUpdateInfo()
      {
        Id = product.Id,
        Name = product.Name,
        Manufacturer = product.Manufacturer,
        Version = product.Version,
        WixoutFileName = wixoutFileName,
        Hashes = hashes
      };
    }

    #region Методы сохранения и загрузки.

    /// <summary>
    /// Сохраняет текущий объект в архивный файл с дополнительным файлами.
    /// </summary>
    public void Save(string fileName, bool deleteWixoutFile = false)
    {
      // Файл сохраняем, обновим WixoutFileName
      // указав там только имя файла.
      string pathToWixoutFileName = WixoutFileName;
      WixoutFileName = Path.GetFileName(WixoutFileName);

      using (MemoryStream memoryStream = new MemoryStream())
      {
        using (ZipArchive zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
          ZipArchiveEntry entry = zip.CreateEntry(InfoXml);
          using (Stream entryStream = entry.Open())
          {
            XmlSaverLoader.Save<WixProductUpdateInfo>(this, entryStream);
          }

          // Сохраним wixout.
          entry = zip.CreateEntry(InfoWixout);
          using (FileStream fileStream = new FileStream(pathToWixoutFileName, FileMode.Open))
          using (Stream entryStream = entry.Open())
          {
            fileStream.CopyTo(entryStream);
          }
        }

        using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
        {
          memoryStream.Seek(0, SeekOrigin.Begin);
          memoryStream.CopyTo(fileStream);
        }
      }

      // Если выставлен флаг удаления wixout файла, то удалим его.
      if (deleteWixoutFile)
        File.Delete(pathToWixoutFileName);
    }

    public static WixProductUpdateInfo Load(string fileName)
    {
      WixProductUpdateInfo result;
      using (FileStream zipStream = new FileStream(fileName, FileMode.Open))
      {
        using (ZipArchive zip = new ZipArchive(zipStream))
        {
          ZipArchiveEntry entry = zip.GetEntry(InfoXml);
          using (Stream entryStream = entry.Open())
          {
            result = XmlSaverLoader.Load<WixProductUpdateInfo>(entryStream);
          }

          // Сохранять файл будем в базовую директорию, т. е.
          // директория где расположен файл, а не в текущую директорию приложения.
          string baseDirectory = Path.GetDirectoryName(fileName);
          entry = zip.GetEntry(InfoWixout);
          using (Stream entryStream = entry.Open())
          using (FileStream fileStream = new FileStream(Path.Combine(baseDirectory, result.WixoutFileName), FileMode.Create))
          {
            entryStream.CopyTo(fileStream);
          }
        }
      }

      return result;
    }

    #endregion
  }
}

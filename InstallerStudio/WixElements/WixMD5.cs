using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Содержит хеши элементов и файлов (если они есть).
  /// </summary>
  [DataContract(Namespace = StringResources.Namespace)]
  class WixMD5ElementHash
  {
    private string mainHash;

    public WixMD5ElementHash(Type type, string id, string hash, Dictionary<string, string> filesHashes)
    {
      Type = type.Name;
      Id = id;
      Hash = hash;
      Files = filesHashes;
    }

    [DataMember]
    public string Type { get; private set; }
    [DataMember]
    public string Id { get; private set; }
    [DataMember]
    public string Hash { get; private set; }
    [DataMember]
    public Dictionary<string, string> Files { get; private set; }

    /// <summary>
    /// Основной хеш. Учитывает хеши файлов.
    /// </summary>
    public string MainHash 
    { 
      get
      { 
        // В конструктор этот код помещать нельзя, так как он (конструктор)
        // не вызовется при десериализации.
        if (mainHash == null)
        {
          mainHash = Hash;
          if (Files != null)
          {
            // Отсортируем для однозначного формирования в разные моменты времени.
            foreach (var f in Files.OrderBy(v => v.Key))
            {
              mainHash += f.Value;
            }
          }
        }

        return mainHash;
      }
    }

    #region Object

    public override bool Equals(object obj)
    {
      return obj is WixMD5ElementHash && (obj as WixMD5ElementHash).MainHash == MainHash;
    }

    public override int GetHashCode()
    {
      return MainHash.GetHashCode();
    }

    #endregion
  }

  /// <summary>
  /// Класс позволяет получать для каждого IWixElement его хеш-код.
  /// Реализовано как расширение IWixElement.
  /// </summary>
  static class WixMD5
  {
    private static string GetString(byte[] bytes)
    {
      StringBuilder builder = new StringBuilder();
      foreach (byte b in bytes)
      {
        builder.Append(b.ToString("x2"));
      }

      return builder.ToString();
    }

    public static WixMD5ElementHash GetMD5(this IWixElement element, string baseDirectory = "")
    {
      MD5 md5 = MD5.Create();
      byte[] bytes;
      string hash = null;
      Dictionary<string, string> filesHashes = null;

      // Сохраним объект в поток.
      using (MemoryStream stream = new MemoryStream())
      {
        XmlSaverLoader.Save(element, stream);
        stream.Seek(0, SeekOrigin.Begin);
        bytes = md5.ComputeHash(stream);
        hash = GetString(bytes);
      }

      // Если используется поддержка файлов, то посчитаем хеши для файлов.
      IFileSupport fileSupport = element as IFileSupport;
      if (fileSupport != null)
      {
        filesHashes = new Dictionary<string, string>();
        // Проверяем чтобы имя файла было не равно нулю. Это может быть у необязательных параметров,
        // например у иконки.
        foreach (string file in fileSupport.GetFilesWithRelativePath().Where(v => v != null))
        {
          using (FileStream fileStream = new FileStream(Path.Combine(baseDirectory ?? "", file), FileMode.Open))
          {
            bytes = md5.ComputeHash(fileStream);
            filesHashes.Add(file, GetString(bytes));
          }
        }
      }

      return new WixMD5ElementHash(element.GetType(), element.Id, hash, filesHashes);
    }
  }
}

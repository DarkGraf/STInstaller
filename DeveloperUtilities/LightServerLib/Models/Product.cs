using System;
using System.IO;
using System.Xml.Serialization;

namespace LightServerLib.Models
{
  public interface IProduct
  {
    string FileName { get; }

    Guid Id { get; set; }
    Guid UpgradeCode { get; set; }
    string Name { get; set; }
    string Manufacturer { get; set; }
    AppVersion Version { get; set; }

    string MdfFile { get; set; }
    string LdfFile { get; set; }
    string SpDllFile { get; set; }
    string SpIniFile { get; set; }
    string SpSqlFile { get; set; }
    string SqlFile { get; set; }
    string PluginDllFile { get; set; }
  }

  public class Product : IProduct
  {
    /// <summary>
    /// Текущее имя файла, под которым сохранен документ.
    /// </summary>
    [XmlIgnore]
    public string FileName { get; private set; }

    public Guid Id { get; set; }
    public Guid UpgradeCode { get; set; }
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public AppVersion Version { get; set; }

    public string MdfFile { get; set; }
    public string LdfFile { get; set; }
    public string SpDllFile { get; set; }
    public string SpIniFile { get; set; }
    public string SpSqlFile { get; set; }
    public string SqlFile { get; set; }
    public string PluginDllFile { get; set; }

    public Product()
    {
      Id = Guid.NewGuid();
      UpgradeCode = Guid.NewGuid();
      Name = string.Empty;
      Manufacturer = string.Empty;
      Version = new AppVersion(1, 0, 0, 0);

      MdfFile = string.Empty;
      LdfFile = string.Empty;
      SpDllFile = string.Empty;
      SpIniFile = string.Empty;
      SpSqlFile = string.Empty;
      SqlFile = string.Empty;
      PluginDllFile = string.Empty;
    }

    public void Save(string fileName)
    {
      FileName = fileName;
      using (StreamWriter writer = new StreamWriter(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(typeof(Product));
        serializer.Serialize(writer, this);
      }
    }

    public static Product Load(string fileName)
    {
      using (StreamReader reader = new StreamReader(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(typeof(Product));
        Product product = serializer.Deserialize(reader) as Product;
        product.FileName = fileName;
        return product;
      }
    }
  }
}

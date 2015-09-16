using System;
using System.IO;
using System.Runtime.Serialization;

namespace InstallerStudio.Utils
{
  public interface ISettingsInfo
  {
    string WixToolsetPath { get; set; }
    string CandleFileName { get; set; }
    string LightFileName { get; set; }
    string TorchFileName { get; set; }
    string PyroFileName { get; set; }
    string UIExtensionFileName { get; set; }
    string SuppressIce { get; set; }
  }

  class SettingsManager
  {
    [DataContract]
    class SettingsInfo : ISettingsInfo
    {
      [DataMember]
      public string WixToolsetPath { get; set; }
      [DataMember]
      public string CandleFileName { get; set; }
      [DataMember]
      public string LightFileName { get; set; }
      [DataMember]
      public string TorchFileName { get; set; }
      [DataMember]
      public string PyroFileName { get; set; }
      [DataMember]
      public string UIExtensionFileName { get; set; }
      [DataMember]
      public string SuppressIce { get; set; }

      [OnDeserializing]
      void OnDeserializing(StreamingContext context)
      {
        // Метод необходим для поддержки уже установленных версий при добавлении новых настроек.
        // Перед десериализации сохраненных настроек вызовется сначала это метод,
        // здесь заполняем поля по умолчанию, затем сереализованные поля старой версии
        // перепишут изменненые пользователем поля, а новые поля сохранят значения по умолчанию.
        SettingsInfo info = CreateDefault();
        SettingsInfoCopier.Copy(this, info);
      }

      public static SettingsInfo CreateDefault()
      {
        SettingsInfo info = new SettingsInfo();
        info.WixToolsetPath = "C:\\Program Files\\WiX Toolset\\bin";
        info.CandleFileName = "candle.exe";
        info.LightFileName = "light.exe";
        info.TorchFileName = "torch.exe";
        info.PyroFileName = "pyro.exe";
        info.UIExtensionFileName = "wixUIExtension.dll";
        info.SuppressIce = "ICE38;ICE43;ICE57";
        return info;
      }

      public static SettingsInfo CreateFrom(ISettingsInfo info)
      {
        SettingsInfo result = new SettingsInfo();
        SettingsInfoCopier.Copy(result, info);
        return result;
      }
    }

    internal static string Directory = "";

    internal static string FileName 
    { 
      get { return Path.Combine(Directory, "Settings.xml"); } 
    }

    public SettingsManager()
    {
      if (!string.IsNullOrEmpty(Directory) && !System.IO.Directory.Exists(Directory))
        System.IO.Directory.CreateDirectory(Directory);
    }

    public ISettingsInfo Load()
    {
      if (!File.Exists(FileName))
      {
        XmlSaverLoader.Save<SettingsInfo>(SettingsInfo.CreateDefault(), FileName);
      }

      return XmlSaverLoader.Load<SettingsInfo>(FileName);
    }

    public void Save(ISettingsInfo info)
    {
      XmlSaverLoader.Save<SettingsInfo>(SettingsInfo.CreateFrom(info), FileName);
    }
  }
}

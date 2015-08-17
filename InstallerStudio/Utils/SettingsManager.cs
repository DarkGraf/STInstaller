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

      public static SettingsInfo CreateDefault()
      {
        SettingsInfo info = new SettingsInfo();
        info.WixToolsetPath = "C:\\Program Files\\WiX Toolset\\bin";
        info.CandleFileName = "candle.exe";
        info.LightFileName = "light.exe";
        return info;
      }

      public static SettingsInfo CreateFrom(ISettingsInfo info)
      {
        return new SettingsInfo
        {
          WixToolsetPath = info.WixToolsetPath,
          CandleFileName = info.CandleFileName,
          LightFileName = info.LightFileName
        };
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

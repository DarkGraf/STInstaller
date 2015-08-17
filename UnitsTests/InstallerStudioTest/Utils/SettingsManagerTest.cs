using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class SettingsManagerTest
  {
    #region Инициализация и очистка теста.
    
    [TestInitialize]
    public void TestInitialize()
    {
      // Если файл существует, удалим.
      if (File.Exists(SettingsManager.FileName))
        File.Delete(SettingsManager.FileName);
    }

    [TestCleanup]
    public void TestCleanup()
    {
      // Если файл существует, удалим.
      if (File.Exists(SettingsManager.FileName))
        File.Delete(SettingsManager.FileName);
    }

    #endregion

    /// <summary>
    /// Получение настроек по умолчанию (при отсутствии файла).
    /// </summary>
    [TestMethod]
    public void SettingsManagerDefaultValue()
    {
      SettingsManager settings = new SettingsManager();
      ISettingsInfo info = settings.Load();
      Assert.IsTrue(File.Exists(SettingsManager.FileName));
      Assert.AreEqual("C:\\Program Files\\WiX Toolset\\bin", info.WixToolsetPath);
      Assert.AreEqual("candle.exe", info.CandleFileName);
      Assert.AreEqual("light.exe", info.LightFileName);
    }

    [TestMethod]
    public void SettingsManagerSaveAndLoad()
    {
      SettingsManager settings = new SettingsManager();

      // Загрузим и изменим значение.
      ISettingsInfo info = settings.Load();
      info.WixToolsetPath = "C:\\Program Files\\WiX Toolset v3.9\\bin";
      info.CandleFileName = "Candle.exe";
      info.LightFileName = "Light.exe";
      settings.Save(info);

      // Загрузим заново и проверим.
      info = settings.Load();
      Assert.AreEqual("C:\\Program Files\\WiX Toolset v3.9\\bin", info.WixToolsetPath);
      Assert.AreEqual("Candle.exe", info.CandleFileName);
      Assert.AreEqual("Light.exe", info.LightFileName);
    }

    [TestMethod]
    public void SettingsManagerDirectoryCreateCheck()
    {
      string directory = "DirTest";

      if (Directory.Exists(directory))
        Directory.Delete(directory, true);

      SettingsManager.Directory = directory;
      SettingsManager settings = new SettingsManager();
      settings.Load();
      Assert.IsTrue(Directory.Exists(directory));
      Assert.IsTrue(File.Exists(SettingsManager.FileName));

      if (Directory.Exists(directory))
        Directory.Delete(directory, true);
    }
  }
}

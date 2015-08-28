using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using System.Xml.Linq;
using System.Xml.Serialization;

using LightServerLib.Models;
using LightServerLib.Common;

namespace LightServerPatch.Models
{
  public class PatchInfo
  {
    public string CurrentWixout { get; set; }
    public string CurrentXml { get; set; }
    public string NewWixout { get; set; }
    public string NewXml { get; set; }
    public string OutDirectory { get; set; }

    public PatchInfo()
    {
      CurrentWixout = CurrentXml = NewWixout = NewXml = string.Empty;
    }

    public void Save(string fileName)
    {
      using (StreamWriter writer = new StreamWriter(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(typeof(PatchInfo));
        serializer.Serialize(writer, this);
      }
    }

    public static PatchInfo Load(string fileName)
    {
      using (StreamReader reader = new StreamReader(fileName))
      {
        XmlSerializer serializer = new XmlSerializer(typeof(PatchInfo));
        return serializer.Deserialize(reader) as PatchInfo;
      }
    }
  }

  enum MainModelMessageType { Info, Error }

  class MainModelEventArgs : EventArgs
  {
    public string Message { get; private set; }
    public string MessageSource { get; private set; }
    public MainModelMessageType Type { get; private set; }

    public MainModelEventArgs(string message, string messageSource, MainModelMessageType type)
    {
      Message = message;
      MessageSource = messageSource;
      Type = type;
    }
  }

  class MainModel
  {
    string settingsFileName = "PatchInfo.xml";

    public PatchInfo PatchInfo { get; private set; }

    #region Событие содержащее информацию о построении.

    /// <summary>
    /// Событие содержащее информацию о построении.
    /// </summary>
    public event EventHandler<MainModelEventArgs> SendMessage;

    private void OnSendMessage(MainModelEventArgs e)
    {
      if (SendMessage != null)
        SendMessage(this, e);
    }

    #endregion

    public MainModel()
    {
      if (File.Exists(settingsFileName))
        PatchInfo = PatchInfo.Load(settingsFileName);
      else
        PatchInfo = new PatchInfo();
    }

    public void Build()
    {
      // Загружаем из ресурсов.
      Uri uri = new Uri(@"pack://application:,,,/Template/Patch.wxs");
      StreamResourceInfo info = Application.GetResourceStream(uri);
      Stream stream = info.Stream;
      XElement xmlWix = XElement.Load(stream);

      // Загружаем исходные файлы использовавшиеся для построения MSI.
      Product currentProduct = Product.Load(PatchInfo.CurrentXml);
      Product newProduct = Product.Load(PatchInfo.NewXml);

      // Заполняем файл для построения патча.
      XElement xmlPatch = xmlWix.GetNode("Patch");
      xmlPatch.Attribute("Comments").Value = string.Format(@"Патч для ""{0}"" версии {1}", currentProduct.Name, currentProduct.Version);
      xmlPatch.Attribute("Description").Value = string.Format(@"Обновления ""{0}"" до версии {1}", newProduct.Name, newProduct.Version);
      xmlPatch.Attribute("DisplayName").Value = string.Format(@"Патч ""{0}""", newProduct.Name);
      xmlPatch.Attribute("Manufacturer").Value = newProduct.Manufacturer;
      xmlPatch.Attribute("TargetProductName").Value = newProduct.Name;
      XElement xmlPatchFamily = xmlPatch.GetNode("PatchFamily");
      xmlPatchFamily.Attribute("Version").Value = currentProduct.Version.ToString();
      xmlPatchFamily.Attribute("ProductCode").Value = currentProduct.Id.ToString();

      // Создаем директорию.
      string directoryName = string.Format("{0} {1}.{2}.{3} Патч (обновление до {4}.{5}.{6})", 
        newProduct.Name, 
        currentProduct.Version.Major, currentProduct.Version.Minor, currentProduct.Version.Build,
        newProduct.Version.Major, newProduct.Version.Minor, newProduct.Version.Build);
      directoryName = Path.Combine(Path.GetFullPath(PatchInfo.OutDirectory), directoryName);
      if (Directory.Exists(directoryName))
        Directory.Delete(directoryName, true);
      Directory.CreateDirectory(directoryName);

      // Сохраняем файл построения патча.
      string wxsFileName = Path.Combine(directoryName, "Patch.wxs");
      xmlWix.Save(wxsFileName);

      ProcessRunner processRunner = new ProcessRunner(new Process());
      string parameters;

      string pathToWix = ConfigurationManager.AppSettings["wixPath"].IncludeTrailingPathDelimiter(); ;

      // torch -p -xi "Сервер АСПО 1.0.1\Сервер АСПО.wixout" "Сервер АСПО 1.0.2\Сервер АСПО.wixout" -out Patch\Differences.wixmst
      string wixmstFileName = Path.ChangeExtension(wxsFileName, ".wixmst");
      parameters = string.Format(@"-p -xi ""{0}"" ""{1}"" -out ""{2}""", 
        PatchInfo.CurrentWixout, PatchInfo.NewWixout, wixmstFileName);
      processRunner.Start(Path.Combine(pathToWix, "torch"), parameters);
      FormatAndSendProcessMessage(processRunner.Output, "Torch", processRunner.HasError);
      if (processRunner.HasError)
        return;

      // candle Patch\Patch.wxs -out Patch\
      parameters = string.Format(@"""{0}"" -out ""{1}\""", wxsFileName, directoryName.IncludeTrailingPathDelimiter());
      processRunner.Start(Path.Combine(pathToWix, "candle"), parameters);
      FormatAndSendProcessMessage(processRunner.Output, "Candle", processRunner.HasError);
      if (processRunner.HasError)
        return;

      // light Patch\Patch.wixobj -out Patch\Patch.wixmsp
      string wixobjFileName = Path.ChangeExtension(wxsFileName, ".wixobj");
      string wixmspFileName = Path.ChangeExtension(wxsFileName, ".wixmsp");
      parameters = string.Format(@"""{0}"" -out ""{1}""", wixobjFileName, wixmspFileName);
      processRunner.Start(Path.Combine(pathToWix, "light"), parameters);
      FormatAndSendProcessMessage(processRunner.Output, "Light", processRunner.HasError);
      if (processRunner.HasError)
        return;

      // pyro Patch\Patch.wixmsp -t MyPatch Patch\Differences.wixmst -out Patch\Patch.msp
      string mspFileName = Path.ChangeExtension(wxsFileName, ".msp");
      parameters = string.Format(@"""{0}"" -t AspoPatch ""{1}"" -out ""{2}""", wixmspFileName, wixmstFileName, mspFileName);
      processRunner.Start(Path.Combine(pathToWix, "pyro"), parameters);
      FormatAndSendProcessMessage(processRunner.Output, "Pyro", processRunner.HasError);
      if (processRunner.HasError)
        return;

      foreach (string fileName in new string[] { wxsFileName, wixmstFileName, wixobjFileName, wixmspFileName })
        if (File.Exists(fileName))
          File.Delete(fileName);      

      // В конце сохраним последние настройки.
      PatchInfo.Save(settingsFileName);

      OnSendMessage(new MainModelEventArgs("Файл " + mspFileName + " построен.", "LightServerPatch", MainModelMessageType.Info));
    }

    private void FormatAndSendProcessMessage(IEnumerable<string> messages, string sourceMessage, bool hasError)
    {
      MainModelEventArgs args = new MainModelEventArgs(
        string.Join(Environment.NewLine, messages.ToArray()),
        sourceMessage,
        hasError ? MainModelMessageType.Error : MainModelMessageType.Info);
      OnSendMessage(args);
    }
  }
}

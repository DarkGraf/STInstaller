using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using LightServerLib.Models;
using LightServerLib.Common;
using LightServerInstaller.Common;

namespace LightServerInstaller.Models
{
  class MsiBuilder : IDisposable
  {
    /// <summary>
    /// Компилятор/препроцессор — получает объектные модули по исходным XML-документам.
    /// </summary>
    public const string candleFileName = "candle.exe";
    /// <summary>
    /// Компоновщик — собирает готовый инсталляционный пакет из объектных модулей и других ресурсов.
    /// </summary>
    public const string lightFileName = "light.exe";

    public List<string> lastOutput = new List<string>();

    struct SourceFile
    {
      public string File { get; set; }
      public bool RequiresBuild { get; set; }
      public bool IsExtension { get; set; }
    }

    IProduct product;
    IProcessRunner processRunner;

    string wixPath;
    string templatePath;
    TemporalDirectory tempDirectory;

    SourceFile[] sourceFiles;

    public MsiBuilder(IProduct product, IProcessRunner processRunner)
    {
      this.product = product;
      this.processRunner = processRunner;

      wixPath = ConfigurationManager.AppSettings["wixPath"].IncludeTrailingPathDelimiter();
      templatePath = Path.GetFullPath(ConfigurationManager.AppSettings["templatePath"]);
      tempDirectory = new TemporalDirectory();      
    }

    public void Build()
    {
      lastOutput.Clear();

      CreateSourceFiles();
      // Копирование файлов во временную директорию.
      CopyingFiles();
      // Заполняем исходные файлы текущей информацией.
      FillSourceFiles();

      BuildWixObj();
      lastOutput.AddRange(processRunner.Output);

      if (!processRunner.HasError)
      {
        BuildMsiOrWixout(false);
        lastOutput.AddRange(processRunner.Output);
      }

      if (!processRunner.HasError)
      {
        BuildMsiOrWixout(true);
        lastOutput.AddRange(processRunner.Output);
      }
    }

    public bool HasLastError
    {
      get { return processRunner.HasError; }
    }

    public IEnumerable<string> LastOutput
    {
      get { return lastOutput; }
    }

    private void CreateSourceFiles()
    {
      sourceFiles = new SourceFile[]
      {
        new SourceFile { File = "Components.wxs", RequiresBuild = true, IsExtension = false },
        new SourceFile { File = "Directories.wxs", RequiresBuild = true, IsExtension = false },
        new SourceFile { File = "Features.wxs",RequiresBuild = true, IsExtension = false },
        new SourceFile { File = "Product.wxs", RequiresBuild = true, IsExtension = false },

        new SourceFile { File = "ComponentsClient.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "ComponentsServer.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "FeaturesClient.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "FeaturesServer.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "ProductClient.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "ProductServer.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "Variables.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "VariablesClient.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "VariablesServer.wxi", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "Resources\\Banner.bmp", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "Resources\\Dialog.bmp", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "Resources\\Help.ico", RequiresBuild = false, IsExtension = false },
        new SourceFile { File = "Resources\\License.rtf", RequiresBuild = false, IsExtension = false },

        new SourceFile { File = "WixSTExtension.dll", RequiresBuild = false, IsExtension = true }
      };
    }

    private void CopyingFiles()
    {
      foreach (SourceFile file in sourceFiles)
      {
        // Проверяем директорию. Если её нет, создаем.
        string dir = Path.Combine(tempDirectory.TempDirectory, Path.GetDirectoryName(file.File));
        if (!Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        File.Copy(Path.Combine(templatePath, file.File), Path.Combine(tempDirectory.TempDirectory, file.File));
      }
    }

    private void FillSourceFiles()
    {
      string fileName;
      XProcessingInstruction[] instructions;

      // Меняем информацию в ProductServer.wxi.
      fileName = Path.Combine(tempDirectory.TempDirectory, "ProductServer.wxi");
      XElement xmlProductServer = XElement.Load(fileName);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "MdfFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.MdfFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "LdfFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.LdfFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "SqlFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.SqlFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "InstallXpFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.SpSqlFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "ExtednedProceduresDllFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.SpDllFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "ExtednedProceduresIniFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.SpIniFile);
      xmlProductServer.GetNode("Binary", new XAttribute("Id", "PluginDemoFile")).Attribute("SourceFile").Value = Path.GetFullPath(product.PluginDllFile);
      xmlProductServer.Save(fileName);

      // Меняем информацию в Variables.wxi.
      fileName = Path.Combine(tempDirectory.TempDirectory, "Variables.wxi");
      XElement xmlVariables = XElement.Load(fileName);
      instructions = (from n in xmlVariables.Nodes()
                      where n is XProcessingInstruction
                      select n).Cast<XProcessingInstruction>().ToArray();
      instructions.First(v => v.Data.StartsWith("MajorProductVersion")).Data = string.Format(@"MajorProductVersion = ""{0}.{1}""", product.Version.Major, product.Version.Minor);
      instructions.First(v => v.Data.StartsWith("MinorProductVersion")).Data = string.Format(@"MinorProductVersion = ""{0}""", product.Version.Build);
      xmlVariables.Save(fileName);

      // Меняем информацию в VariablesServer.wxi.
      fileName = Path.Combine(tempDirectory.TempDirectory, "VariablesServer.wxi");
      XElement xmlVariablesServer = XElement.Load(fileName);
      instructions = (from n in xmlVariablesServer.Nodes()
                      where n is XProcessingInstruction
                      select n).Cast<XProcessingInstruction>().ToArray();
      instructions.First(v => v.Data.StartsWith("ProductId")).Data = string.Format(@"ProductId = ""{0}""", product.Id);
      instructions.First(v => v.Data.StartsWith("ProductName")).Data = string.Format(@"ProductName = ""{0}""", product.Name);
      instructions.First(v => v.Data.StartsWith("ProductManufacturer")).Data = string.Format(@"ProductManufacturer = ""{0}""", product.Manufacturer);
      instructions.First(v => v.Data.StartsWith("ProductUpgradeCode")).Data = string.Format(@"ProductUpgradeCode = ""{0}""", product.UpgradeCode);
      xmlVariablesServer.Save(fileName);
    }

    private void BuildWixObj()
    {
      StringBuilder parameters = new StringBuilder();

      parameters.Append(string.Format(@"-out ""{0}\"" ", tempDirectory.TempDirectory.IncludeTrailingPathDelimiter()));
      parameters.Append(@"-arch x86 ");

      parameters.Append(string.Format(@"-ext ""{0}"" ", Path.Combine(tempDirectory.TempDirectory, "WixSTExtension.dll")));

      foreach (SourceFile file in sourceFiles.Where(f => f.RequiresBuild))
        parameters.Append(string.Format(@"""{0}"" ", Path.Combine(tempDirectory.TempDirectory, file.File)));

      processRunner.Start(Path.Combine(wixPath, candleFileName), parameters.ToString());
    }

    private void BuildMsiOrWixout(bool buildMsi)
    {
      string msiFileName = Path.GetDirectoryName(product.FileName);
      msiFileName = Path.Combine(msiFileName,
        string.Format("{0} {1}.{2}.{3}", product.Name, product.Version.Major, product.Version.Minor, product.Version.Build),
        buildMsi ? string.Format("{0}.msi", product.Name) : string.Format("{0}.wixout", product.Name));

      StringBuilder parameters = new StringBuilder();
      parameters.Append(string.Format(@"{0}-out ""{1}"" ", (buildMsi ? "" : "-bf -xo "), msiFileName));
      parameters.Append(string.Format(@"-ext ""{0}"" ", Path.Combine(tempDirectory.TempDirectory, "WixSTExtension.dll")));
      parameters.Append(string.Format(@"-ext ""{0}WixUIExtension.dll"" ", wixPath));
      parameters.Append(@"-cultures:ru-RU;en-US ");

      foreach (SourceFile file in sourceFiles.Where(f => f.RequiresBuild))
        parameters.Append(string.Format(@"""{0}"" ", Path.Combine(tempDirectory.TempDirectory, Path.ChangeExtension(file.File, ".wixobj"))));

      processRunner.Start(Path.Combine(wixPath, lightFileName), parameters.ToString());
    }

    #region IDisposable

    public void Dispose()
    {
      tempDirectory.Dispose();
    }

    #endregion
  }
}

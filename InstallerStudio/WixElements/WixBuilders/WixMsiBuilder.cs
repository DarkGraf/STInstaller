using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

using InstallerStudio.Utils;
using InstallerStudio.WixElements;

namespace InstallerStudio.WixElements.WixBuilders
{
  enum MsiTemplateTypes
  {
    Component,
    Directory,
    Feature,
    Product,
    Variable
  }

  class WixMsiBuilder : WixBuilderBase
  {
    private readonly IDictionary<MsiTemplateTypes, string> templates;
    private readonly IList<string> resourcesTemplates;
    private readonly WixProduct product;

    public WixMsiBuilder(WixProduct product)
    {
      this.product = product;

      templates = new Dictionary<MsiTemplateTypes, string>
      {
        { MsiTemplateTypes.Component, "Components.wxs" },
        { MsiTemplateTypes.Directory, "Directories.wxs" },
        { MsiTemplateTypes.Feature, "Features.wxs" },
        { MsiTemplateTypes.Product, "Product.wxs" },
        { MsiTemplateTypes.Variable, "Variables.wxi" }
      };

      resourcesTemplates = new List<string>
      {
        "Resources/Banner.bmp",
        "Resources/Dialog.bmp",
        "Resources/Help.ico",
        "Resources/License.rtf"
      };
    }

    #region Обработка шаблонов.

    private void ProcessingTemplateComponent(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Component]);
      DeleteDevelopmentInfo(path);
    }

    private void ProcessingTemplateDirectory(IBuildContext context, CancellationTokenSource cts)
    {

    }

    private void ProcessingTemplateFeature(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Feature]);
      DeleteDevelopmentInfo(path);
    }

    void ProcessingTemplateProduct(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Product]);
      DeleteDevelopmentInfo(path);

      XElement xmlWix = XElement.Load(path);

      // Получаем секцию Product.
      XElement xmlProduct = xmlWix.Elements().First(v => v.Name.LocalName == "Product");

      // Указываем режим работы интерфейса.
      // Произведем поиск по всем WixElements и если есть элемент
      // с типом WixDbComponentElement, то это серверная установка,
      // иначе установка клиента.
      bool isServerInstallation = product.RootElement.Items.Descendants().OfType<WixDbComponentElement>().Count() > 0;
      xmlProduct.Add(new XComment("Указываем режим работы интерфейса."));
      xmlProduct.Add(new XElement(XmlNameSpaceST + "UIType",
        new XAttribute("Id", "MainUI"),
        new XAttribute("Type", isServerInstallation ? "Server" : "Client")));
      
      // Загрузка файлов.

      // Mef-плагины.

      xmlWix.Save(path);
    }

    void ProcessingTemplateVariable(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Variable]);
      DeleteDevelopmentInfo(path);

      XElement xmlVariable = XElement.Load(path);

      // Меняем версию.
      XProcessingInstruction major = xmlVariable.GetXProcessingInstructionDataStartsWith("define", "MajorProductVersion = ");
      major.Data = string.Format("MajorProductVersion = \"{0}.{1}\"", product.Version.Major, product.Version.Minor);
      XProcessingInstruction minor = xmlVariable.GetXProcessingInstructionDataStartsWith("define", "MinorProductVersion = ");
      minor.Data = string.Format("MinorProductVersion = \"{0}\"", product.Version.Build);

      // Заполняем переменные препроцессора WIX.
      XNode[] nodes = new XNode[]
      {
        new XComment("Описание продукта."),
        new XProcessingInstruction("define", string.Format("ProductId = \"{0}\"", product.Id)),
        new XProcessingInstruction("define", string.Format("ProductName = \"{0}\"", product.Name)),
        new XProcessingInstruction("define", string.Format("ProductManufacturer = \"{0}\"", product.Manufacturer)),
        new XProcessingInstruction("define", "ProductVersion = \"$(var.MajorProductVersion).$(var.MinorProductVersion)\""),
        new XProcessingInstruction("define", string.Format("ProductUpgradeCode = \"{0}\"", product.UpgradeCode)),

        new XComment("Свойства msi-пакета."),
        new XProcessingInstruction("define", "PackageId = \"*\""),
        new XProcessingInstruction("define", string.Format("PackageDescription = \"{0}\"", product.PackageDescription)),
        new XProcessingInstruction("define", "PackageManufacturer = $(var.ProductManufacturer)"),
        new XProcessingInstruction("define", string.Format("PackageComments = \"{0}\"", product.PackageComments)),

        new XComment("Другие свойства."),
        new XProcessingInstruction("define", string.Format("InstallLocationFamilyFolder = \"{0}\"", product.InstallLocationFamilyFolder)),
        new XProcessingInstruction("define", string.Format("InstallLocationProductFolder = \"{0} $(var.MajorProductVersion)\"", product.InstallLocationProductFolder)),
        new XProcessingInstruction("define", "ProgramMenuFamilyDirName = $(var.InstallLocationFamilyFolder)"),
        new XProcessingInstruction("define", string.Format("ProgramMenuFamilyDirComponentGuid = \"{0}\"", product.ProgramMenuFamilyDirComponentGuid)),
        new XProcessingInstruction("define", "ProgramMenuProductDirName = $(var.InstallLocationProductFolder)"),
        new XProcessingInstruction("define", string.Format("ProgramMenuProductDirComponentGuid = \"{0}\"", product.ProgramMenuProductDirComponentGuid)),
        new XProcessingInstruction("define", string.Format("ProgramMenuReinstallComponentGuid = \"{0}\"", product.ProgramMenuReinstallComponentGuid))
      };

      // Необходимо добавить после ключа реестра.
      XNode first = xmlVariable.GetXProcessingInstructionDataStartsWith("define", "MainRegistryPath =");
      foreach (XNode node in nodes)
      {
        first.AddAfterSelf(first = node);
      }      

      xmlVariable.Save(path);
    }

    private void RunningCandle(IBuildContext context, CancellationTokenSource cts)
    {
      context.BuildMessageWriteLine("Запуск компилятора Candle.exe (получение объектных модулей по исходным XML-документам).");

      // Путь к candle.
      string candleFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.CandleFileName);

      // Команда для выполнения.
      StringBuilder command = new StringBuilder();
      // Если указываем путь в кавычках, то надо указать дополнительно обратный слеш,
      // это требование WIX.
      command.Append(string.Format("-out \"{0}\\\"", StoreDirectory.IncludeTrailingPathDelimiter()));
      command.Append(" -arch x86");
      command.Append(string.Format(" -ext \"{0}\"", Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.UIExtensionFileName)));
      command.Append(string.Format(" -ext \"{0}\"", Path.Combine(Environment.CurrentDirectory, "WixSTExtension.dll")));
      
      // Берем файлы с расширением .wxs.
      foreach (var name in templates.Where(v => v.Value.EndsWith(".wxs")))
      {
        command.Append(string.Format(" \"{0}\"", Path.Combine(StoreDirectory, name.Value)));
      }

      string strCommand = command.ToString();
      context.BuildMessageWriteLine(candleFileName + " " + strCommand);
      IProcessRunner runner = CreateProcessRunner();
      runner.OutputMessageReceived += (s, e) =>
        {
          context.BuildMessageWriteLine(e.Message);
        };
      runner.Start(candleFileName, strCommand);
      if (runner.HasError)
        throw new Exception("Запуск Candle.exe завершился с ошибкой.");
    }

    private void RunningLight(IBuildContext context, CancellationTokenSource cts)
    {
      context.BuildMessageWriteLine("Запуск компоновщика Light.exe (сборка инсталляционного пакета из объектных модулей и других ресурсов).");

      // Путь к light.
      string lightFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.LightFileName);

      // Конечное имя. Меняем пробелы на подчеркивание.
      string outFileName = string.Format("{0}_v{1}.{2}", 
        product.Name.Replace(' ', '_'),
        product.Version.Major, product.Version.Minor);
      outFileName = Path.Combine(Path.GetDirectoryName(context.ProjectFileName), outFileName);

      // Команда для выполнения.
      StringBuilder command = new StringBuilder();
      command.Append(string.Format("-out \"{0}.{1}\"", outFileName, "msi"));
      command.Append(string.Format(" -pdbout \"{0}.{1}\"", outFileName, "wixpdb"));
      command.Append(" -cultures:null");
      command.Append(string.Format(" -ext \"{0}\"", Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.UIExtensionFileName)));
      command.Append(string.Format(" -ext \"{0}\"", Path.Combine(Environment.CurrentDirectory, "WixSTExtension.dll")));
      command.Append(" -cultures:ru-RU;en-US");
      // Берем файлы с расширением .wxs и меняем на .wixobj.
      foreach (var name in templates.Where(v => v.Value.EndsWith(".wxs")).Select(v => Path.ChangeExtension(v.Value, ".wixobj")))
      {
        command.Append(@" " + Path.Combine(StoreDirectory, name));
      }

      string strCommand = command.ToString();
      context.BuildMessageWriteLine(lightFileName + " " + strCommand);
      IProcessRunner runner = CreateProcessRunner();
      runner.OutputMessageReceived += (s, e) =>
      {
        context.BuildMessageWriteLine(e.Message);
      };
      runner.Start(lightFileName, strCommand);
      if (runner.HasError)
        throw new Exception("Запуск Light.exe завершился с ошибкой.");
    }

    #endregion

    #region WixBuilderBase

    protected override string[] GetTemplateFileNames()
    {
      string commonPath = "MsiTemplate/";
      return templates.Select(v => Path.Combine(commonPath, v.Value)).
        Union(resourcesTemplates.Select(v => Path.Combine(commonPath, v))).ToArray();
    }

    protected override void ProcessingTemplates(IBuildContext context, CancellationTokenSource cts)
    {
      ProcessingTemplateComponent(context, cts);
      ProcessingTemplateDirectory(context, cts);
      ProcessingTemplateFeature(context, cts);
      ProcessingTemplateProduct(context, cts);
      ProcessingTemplateVariable(context, cts);
    }

    protected override void CompilationAndBuild(IBuildContext context, CancellationTokenSource cts)
    {
      RunningCandle(context, cts);
      RunningLight(context, cts);
    }

    #endregion
  }
}

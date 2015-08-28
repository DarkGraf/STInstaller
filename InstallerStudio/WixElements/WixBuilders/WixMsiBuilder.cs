using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

using InstallerStudio.Utils;
using InstallerStudio.WixElements;
using InstallerStudio.Models;

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

    private void ProcessingTemplateSimpleComponent(IBuildContext context, CancellationTokenSource cts, XElement xmlFragment, WixComponentElement component)
    {
      // Добавляем файлы. В WIX в одном компоненте может быть несколько файлов, но
      // мы будем использовать для каждого файла отдельный компонент. Так не будет
      // проблем с директориями, потому-что пользователь может указать для
      // каждого файла разные директории установки. И что более важно, согласно ТЗ,
      // будем в дальнейшем формировать обновления для каждого файла.
      foreach (WixFileElement file in component.Items.OfType<WixFileElement>())
      {
        XElement xmlFile;
        // Словарь для иконок.
        Dictionary<string, string> icons = new Dictionary<string, string>();
        string installDirectory = MsiModel.FormatInstallDirectory(file.InstallDirectory);
        string pathToFile = Path.Combine(context.SourceStoreDirectory, file.InstallDirectory, file.FileName);

        XElement xmlDirectory = new XElement(XmlNameSpaceWIX + "DirectoryRef",
          new XAttribute("Id", installDirectory),
          new XElement(XmlNameSpaceWIX + "Component",
            new XAttribute("Id", component.Id),
            new XAttribute("Guid", component.Guid),
            xmlFile = new XElement(XmlNameSpaceWIX + "File",
              new XAttribute("Id", file.Id),
              new XAttribute("Name", file.FileName),
              new XAttribute("Source", pathToFile),
              new XAttribute("KeyPath", "yes"))));

        // Если у файла указаны ярлыки, добавим.
        foreach (WixShortcutElement shortcut in file.Items.OfType<WixShortcutElement>())
        {
          // Обязательные атрибуты.
          XElement xmlShortcut = new XElement(XmlNameSpaceWIX + "Shortcut",
            new XAttribute("Id", shortcut.Id),
            new XAttribute("Name", shortcut.Name),
            new XAttribute("WorkingDirectory", installDirectory),
            new XAttribute("Directory", MsiModel.FormatInstallDirectory(shortcut.Directory)),
            new XAttribute("Advertise", "yes"));

          // Не обязательные атрибуты.
          if (!string.IsNullOrWhiteSpace(shortcut.Arguments))
            xmlShortcut.Add(new XAttribute("Arguments", shortcut.Arguments));
          if (!string.IsNullOrWhiteSpace(shortcut.Description))
            xmlShortcut.Add(new XAttribute("Description", shortcut.Description));

          // Для иконки смотрим, если строка пустая и целевой файл содержит иконку,
          // то указываем сам файл.
          // Если строка не пустая, то указываем содержащийся в ней файл.
          if (string.IsNullOrWhiteSpace(shortcut.Icon))
          {
            if (IconHelper.ExtractIcon(pathToFile, 0, false) != null)
            {
              xmlShortcut.Add(new XAttribute("Icon", file.FileName));
              icons[file.FileName] = pathToFile;
            }
          }
          else
          {
            xmlShortcut.Add(new XAttribute("Icon", shortcut.Icon));
            icons[shortcut.Icon] = Path.Combine(context.SourceStoreDirectory, shortcut.Icon);
          }

          xmlFile.Add(xmlShortcut);
        }

        xmlFragment.Add(xmlDirectory);
        // Теперь сформируем теги для иконок.
        foreach (var icon in icons)
        {
          xmlFragment.Add(new XElement(XmlNameSpaceWIX + "Icon",
            new XAttribute("Id", icon.Key),
            new XAttribute("SourceFile", icon.Value)));
        }
      }
    }

    private void ProcessingTemplateDbComponent(IBuildContext context, CancellationTokenSource cts, XElement xmlFragment, WixDbComponentElement component)
    {
      Dictionary<string, string> binaries = new Dictionary<string, string>();

      string formatedMdfFile = component.MdfFile.ReplaceNotLetterAndNotDigitWithASCIICode();
      string formatedLdfFile = component.LdfFile.ReplaceNotLetterAndNotDigitWithASCIICode();

      XElement xmlComponent;
      // Компонент базы данных должен быть один во всем пакете.
      // Устанавливаем его по умолчанию в директорию продукта ProductFolder/DBINSTALLLOCATION.
      // В дальнейшем пользователь сможет сменить директорию.
      XElement xmlDirectory = new XElement(XmlNameSpaceWIX + "DirectoryRef",
        new XAttribute("Id", "ProductFolder"),
        new XElement(XmlNameSpaceWIX + "Directory",
          new XAttribute("Id", "DBINSTALLLOCATION"),
          new XAttribute("Name", "DataBases"),
          xmlComponent = new XElement(XmlNameSpaceWIX + "Component",
            new XAttribute("Id", component.Id),
            new XAttribute("Guid", component.Guid),
            new XComment("Принудительно создаем каталог."),
            new XElement(XmlNameSpaceWIX + "CreateFolder"),
            new XComment("Ключевой компонент."),
            new XElement(XmlNameSpaceWIX + "RegistryValue",
              new XAttribute("Root", "HKCU"),
              new XAttribute("Key", "$(var.MainRegistryPath)"),
              new XAttribute("Name", component.Id),
              new XAttribute("Type", "integer"),
              new XAttribute("Value", "1"),
              new XAttribute("KeyPath", "yes")),
            new XElement(XmlNameSpaceST + "SqlTemplateFiles",
              new XAttribute("Id", "Template" + component.Id),
              new XAttribute("MdfFileBinaryKey", formatedMdfFile),
              new XAttribute("LdfFileBinaryKey", formatedLdfFile)))));

      binaries[formatedMdfFile] = Path.Combine(context.SourceStoreDirectory, component.MdfFile);
      binaries[formatedLdfFile] = Path.Combine(context.SourceStoreDirectory, component.LdfFile);

      xmlFragment.Add(xmlDirectory);

      // Если есть скрипты, добавляем.
      foreach (WixSqlScriptElement script in component.Items.OfType<WixSqlScriptElement>())
      {
        string formatedFile = script.Script.ReplaceNotLetterAndNotDigitWithASCIICode();
        xmlComponent.Add(new XElement(XmlNameSpaceST + "SqlScriptFile",
          new XAttribute("Id", script.Id),
          new XAttribute("BinaryKey", formatedFile),
          new XAttribute("ExecuteOnInstall", script.ExecuteOnInstall ? "yes" : "no"),
          new XAttribute("ExecuteOnReinstall", script.ExecuteOnReinstall ? "yes" : "no"),
          new XAttribute("ExecuteOnUninstall", script.ExecuteOnUninstall ? "yes" : "no"),
          new XAttribute("Sequence", script.Sequence)));
        binaries[formatedFile] = Path.Combine(context.SourceStoreDirectory, script.Script);
      }

      // Если есть хранимые процедуры, добавляем.
      foreach (WixSqlExtentedProceduresElement procedure in component.Items.OfType<WixSqlExtentedProceduresElement>())
      {
        string formatedFile = procedure.FileName.ReplaceNotLetterAndNotDigitWithASCIICode();
        xmlComponent.Add(new XElement(XmlNameSpaceST + "SqlExtendedProcedures",
          new XAttribute("Id", procedure.Id),
          new XAttribute("Name", procedure.FileName),
          new XAttribute("BinaryKey", formatedFile)));
        binaries[formatedFile] = Path.Combine(context.SourceStoreDirectory, procedure.FileName);
      }
      
      // Добавляем бинарные файлы.
      foreach (var binary in binaries)
      {
        xmlFragment.Add(new XElement(XmlNameSpaceWIX + "Binary",
          new XAttribute("Id", binary.Key),
          new XAttribute("SourceFile", binary.Value)));
      }
    }

    private void ProcessingTemplateComponent(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Component]);
      DeleteDevelopmentInfo(path);

      XElement xmlWix = XElement.Load(path);
      // Получаем секцию Fragment, в ней вложенные секции DirectoryRef для каждого компонента.
      XElement xmlFragment = xmlWix.GetXElement("Fragment");

      // Получаем не предопределенные компоненты.
      foreach (WixComponentElement component in product.RootElement.Items.Descendants().
        OfType<WixComponentElement>().Where(v => !v.IsPredefined))
      {
        if (typeof(WixComponentElement) == component.GetType())
          ProcessingTemplateSimpleComponent(context, cts, xmlFragment, component);
        else if (typeof(WixDbComponentElement) == component.GetType())
          ProcessingTemplateDbComponent(context, cts, xmlFragment, component as WixDbComponentElement);
      }
      
      xmlWix.Save(path);
    }

    private void ProcessingTemplateDirectory(IBuildContext context, CancellationTokenSource cts)
    {

    }

    private void ProcessingTemplateFeature(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Feature]);
      DeleteDevelopmentInfo(path);

      XElement xmlWix = XElement.Load(path);

      // Получаем секцию Fragment, в нем должна быть только секции Feature с Id = "RootFeature".
      XElement xmlFragment = xmlWix.GetXElement("Fragment");
      XElement xmlFeature = xmlFragment.GetXElement("Feature", new XAttribute("Id", "RootFeature"));
      
      // Стек для обхода дерева элементов Feature.
      Stack<KeyValuePair<WixFeatureElement, XElement>> stack = new Stack<KeyValuePair<WixFeatureElement, XElement>>();

      // Проверим информацию для построения.
      if (!(product.RootElement is WixFeatureElement))
        throw new Exception("WixProduct.RootElement не является WixFeatureElement");

      // Заполняем стек первым уровнем.
      stack.Push(new KeyValuePair<WixFeatureElement, XElement>(product.RootElement as WixFeatureElement, xmlFeature));

      // Обходим дерево.
      // Первая Feature должна быть обязательно как в продукте, так и в xml.
      // Далее может быть Feature или Component.
      while (stack.Count > 0)
      {
        var pair = stack.Pop();
        WixFeatureElement parent = pair.Key;
        XElement xmlParent = pair.Value;
        // Добавляем детей. Если нет соответствуюшего элемента в xml, то создадим.
        foreach (var child in parent.Items.Where(v => v is WixFeatureElement || v is WixComponentElement))
        {
          XElement xmlChild = null;

          if (child is WixFeatureElement)
          {
            WixFeatureElement childFeature = child as WixFeatureElement;
            xmlChild = xmlParent.GetXElement("Feature", new XAttribute("Id", child.Id));
            // Нет в xml.
            if (xmlChild == null)
            {
              xmlChild = new XElement(XmlNameSpaceWIX + "Feature",
                new XAttribute("Id", childFeature.Id),
                new XAttribute("Level", "1"), // По умолчанию и так 1, для будущего использования.
                new XAttribute("AllowAdvertise", "no"), // Отключаем установку по первому обращению (Installation-On-Demand).
                new XAttribute("InstallDefault", "local"));

              // ConfigurableDirectory используем только если Feature содержит
              // в дочерних элементах компонент WixDbComponentElement.
              if (child.Items.OfType<WixDbComponentElement>().Count() > 0)
                xmlChild.Add(new XAttribute("ConfigurableDirectory", "DBINSTALLLOCATION"));

              // Необязательные поля.
              if (!string.IsNullOrWhiteSpace(childFeature.Title))
                xmlChild.Add(new XAttribute("Title", childFeature.Title));
              if (!string.IsNullOrWhiteSpace(childFeature.Description))
                xmlChild.Add(new XAttribute("Description", childFeature.Description));
              if (!string.IsNullOrWhiteSpace(childFeature.Display))
                xmlChild.Add(new XAttribute("Display", childFeature.Display.ToLower())); // Нужно в нижнем регистре.
              if (!string.IsNullOrWhiteSpace(childFeature.Absent))
                xmlChild.Add(new XAttribute("Absent", childFeature.Absent.ToLower())); // Нужно в нижнем регистре.

              xmlParent.Add(xmlChild);
            }

            stack.Push(new KeyValuePair<WixFeatureElement, XElement>(childFeature, xmlChild));
          }

          if (child is WixComponentElement)
          {
            xmlChild = xmlParent.GetXElement("ComponentRef", new XAttribute("Id", child.Id));
            // Нет в xml.
            if (xmlChild == null)
            {
              xmlChild = new XElement(XmlNameSpaceWIX + "ComponentRef",
                new XAttribute("Id", child.Id));
              xmlParent.Add(xmlChild);
            }
          }
        }
      }

      xmlWix.Save(path);
    }

    void ProcessingTemplateProduct(IBuildContext context, CancellationTokenSource cts)
    {
      string path = Path.Combine(StoreDirectory, templates[MsiTemplateTypes.Product]);
      DeleteDevelopmentInfo(path);

      XElement xmlWix = XElement.Load(path);

      // Получаем секцию Product.
      XElement xmlProduct = xmlWix.GetXElement("Product");

      // Указываем режим работы интерфейса.
      // Произведем поиск по всем WixElements и если есть элемент
      // с типом WixDbComponentElement, то это серверная установка,
      // иначе установка клиента.
      bool isServerInstallation = product.RootElement.Items.Descendants().OfType<WixDbComponentElement>().Count() > 0;
      xmlProduct.Add(new XComment("Указываем режим работы интерфейса."));
      xmlProduct.Add(new XElement(XmlNameSpaceST + "UIType",
        new XAttribute("Id", "MainUI"),
        new XAttribute("Type", isServerInstallation ? "Server" : "Client")));
      
      // Mef-плагины.
      // Содержаться в коневой Feature.
      foreach (WixMefPluginElement plugin in product.RootElement.Items.OfType<WixMefPluginElement>())
      {
        string formatedFile = plugin.FileName.ReplaceNotLetterAndNotDigitWithASCIICode();

        xmlProduct.Add(new XElement(XmlNameSpaceST + "MefPlugin",
          new XAttribute("Id", plugin.Id),
          new XAttribute("BinaryKey", formatedFile)));

        xmlProduct.Add(new XElement(XmlNameSpaceWIX + "Binary",
          new XAttribute("Id", formatedFile),
          new XAttribute("SourceFile", Path.Combine(context.SourceStoreDirectory, plugin.FileName))));
      }

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
      // Добавим к имени файла директорию.
      string outDirectory = string.Format("{0} v{1}", product.Name, product.Version);
      outFileName = Path.Combine(Path.GetDirectoryName(context.ProjectFileName), outDirectory, outFileName);

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
      else
        context.BuildMessageWriteLine(string.Format("Создан файл: {0}.{1}", outFileName, "msi"));
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

using InstallerStudio.Models;
using InstallerStudio.Utils;

namespace InstallerStudio.WixElements.WixBuilders
{
  class WixMspBuilder : WixBuilderBase
  {
    private readonly WixPatchProduct product;
    private string mspTemplate = "Patch.wxs";
    /// <summary>
    /// Исходные файлы.
    /// </summary>
    private readonly IList<string> wxsFiles;

    public WixMspBuilder(WixPatchProduct product)
    {
      this.product = product;
      wxsFiles = new List<string>();
    }

    private string InternalCompilationAndBuild(IBuildContext context, CancellationTokenSource cts, string wxsFile)
    {
      // Команда для выполнения.
      StringBuilder command = new StringBuilder();

      context.BuildMessageWriteLine("Компиляция и сборка файла: " + wxsFile, BuildMessageTypes.Notification);

      context.BuildMessageWriteLine(TorchDescription, BuildMessageTypes.Notification);
      // torch -p -xi "Сервер АСПО 1.0.1\Сервер АСПО.wixout" "Сервер АСПО 1.0.2\Сервер АСПО.wixout" -out Patch\Differences.wixmst
      string wixmstFileName = Path.ChangeExtension(wxsFile, ".wixmst");
      command.Append("-p -xi");
      command.Append(string.Format(" \"{0}\"", Path.Combine(context.SourceStoreDirectory, product.BasePath)));
      command.Append(string.Format(" \"{0}\"", Path.Combine(context.SourceStoreDirectory, product.TargetPath)));
      command.Append(string.Format(" -out \"{0}\"", wixmstFileName));
      string torchFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.TorchFileName);
      RunCommand(context, torchFileName, command.ToString());

      command.Clear();
      context.BuildMessageWriteLine(CandleDescription, BuildMessageTypes.Notification);
      // candle Patch\Patch.wxs -out Patch\
      command.Append(string.Format("\"{0}\"", wxsFile));
      command.Append(string.Format(" -out \"{0}\\\"", Path.GetDirectoryName(wxsFile).IncludeTrailingPathDelimiter()));
      string candleFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.CandleFileName);
      RunCommand(context, candleFileName, command.ToString());

      string wixmspFileName;
      command.Clear();
      context.BuildMessageWriteLine(LightDescription, BuildMessageTypes.Notification);
      // light Patch\Patch.wixobj -out Patch\Patch.wixmsp
      command.Append(string.Format("\"{0}\"", Path.ChangeExtension(wxsFile, ".wixobj")));
      command.Append(string.Format(" -out \"{0}\"", wixmspFileName = Path.ChangeExtension(wxsFile, ".wixmsp")));
      string lightFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.LightFileName);
      RunCommand(context, lightFileName, command.ToString());

      string mspFileName;
      command.Clear();
      context.BuildMessageWriteLine(PyroDescription, BuildMessageTypes.Notification);
      // pyro Patch\Patch.wixmsp -t MyPatch Patch\Differences.wixmst -out Patch\Patch.msp
      command.Append(string.Format("\"{0}\"", wixmspFileName));
#warning AspoPatch Вставить в продукт.
      command.Append(string.Format(" -t AspoPatch \"{0}\"", wixmstFileName));
      command.Append(string.Format(" -out \"{0}\"", mspFileName = Path.ChangeExtension(wxsFile, ".msp")));
      string pyroFileName = Path.Combine(context.ApplicationSettings.WixToolsetPath, context.ApplicationSettings.PyroFileName);
      RunCommand(context, pyroFileName, command.ToString());

      context.BuildMessageWriteLine("Файл " + mspFileName + " построен.", BuildMessageTypes.Notification);
      return mspFileName;
    }

    #region WixBuilderBase

    protected override string[] GetTemplateFileNames()
    {
      return new string[] { "MspTemplate/" + mspTemplate };
    }

    protected override void ProcessingTemplates(IBuildContext context, CancellationTokenSource cts)
    {
      wxsFiles.Clear();

      string pathToTemplate = Path.Combine(StoreDirectory, mspTemplate);
      DeleteDevelopmentInfo(pathToTemplate);

      foreach (IWixElement element in product.RootElement.Items)
      {
        // Создадим имя файла: Имя_Продукта_v1.2.3.4_Имя_Патча_v1.2.3.5.wxs.
        string name = string.Format("{0}_v{1}.{2}.{3}.{4}_{5}_v{6}.{7}.{8}.{9}.wxs",
          product.TargetName.Replace(' ', '_'),
          product.BaseVersion.Major,
          product.BaseVersion.Minor,
          product.BaseVersion.Build,
          product.BaseVersion.Revision,
          element.Id,
          product.TargetVersion.Major,
          product.TargetVersion.Minor,
          product.TargetVersion.Build,
          product.TargetVersion.Revision);

        // Создаем в отдельной директории файл wxs для каждого Patch.
        string path = Path.Combine(StoreDirectory, element.Id);
        Directory.CreateDirectory(path);
        path = Path.Combine(path, name);
        File.Copy(pathToTemplate, path);

        // Дочерние элементы WixPatchComponentElement;
        var children = element.Items.OfType<WixPatchComponentElement>();

        // Заполним содержимым.
        XElement xmlWix = XElement.Load(path);
        XElement xmlPatch = xmlWix.GetXElement("Patch");
        // Сформируем список компонент.
        string componentsDesc = "";
        foreach (WixPatchComponentElement c in children)
          componentsDesc += (componentsDesc == "" ? "" : ", ") + c.Id;
        string description = string.Format(@"Патч ""{0}"" версии {1}. Обновление до версии {2} компонент: {3}",
          product.BaseName, product.BaseVersion, product.TargetVersion, componentsDesc);
        xmlPatch.Attribute("Comments").Value = description;
        xmlPatch.Attribute("Description").Value = description;
        xmlPatch.Attribute("DisplayName").Value = string.Format(@"Патч ""{0}""", product.TargetName);
        xmlPatch.Attribute("Manufacturer").Value = product.TargetManufacturer;
        xmlPatch.Attribute("TargetProductName").Value = product.TargetName;

        XElement xmlPatchFamily = xmlPatch.GetXElement("PatchFamily");
        // Важно, указываем новую версию. Эта версия ни как не связана с версией MSI,
        // только для msp. Но она должна быть больше чем у ранних MSP. Будем использовать
        // версию нового продукта.
        xmlPatchFamily.Attribute("Version").Value = product.TargetVersion;
        xmlPatchFamily.Attribute("ProductCode").Value = product.TargetId.ToString();

        // Добавляем компоненты.
        // Если указаны все компоненты, то ничего не добавляем. Патч сформируется полным.
        // Если компоненты указаны не все, то добавим их.
        bool isFull = children.Select(v => v.Id).OrderBy(v => v).
          SequenceEqual(product.UpdateComponents.Select(v => v.Id).OrderBy(v => v));
        if (!isFull)
        {
          foreach (WixPatchComponentElement component in children)
          {
            XElement xmlComponentRef = new XElement(XmlNameSpaceWIX + "ComponentRef",
              new XAttribute("Id", component.Id));
            xmlPatchFamily.Add(xmlComponentRef);
          }
        }

        xmlWix.Save(path);

        // Добавим в список
        wxsFiles.Add(path);
        context.BuildMessageWriteLine(string.Format("Сформирован {0}.", path), BuildMessageTypes.Notification);
      }
    }

    protected override void CompilationAndBuild(IBuildContext context, CancellationTokenSource cts)
    {
      List<string> msiFiles = new List<string>();

      foreach (string wxsFile in wxsFiles)
      {
        msiFiles.Add(InternalCompilationAndBuild(context, cts, wxsFile));
      }

      string outDir = string.Format("{0} v{1}.{2}.{3}.{4} Patches v{5}.{6}.{7}.{8}", 
        product.BaseName,
        product.BaseVersion.Major, 
        product.BaseVersion.Minor, 
        product.BaseVersion.Build,
        product.BaseVersion.Revision,
        product.TargetVersion.Major,
        product.TargetVersion.Minor,
        product.TargetVersion.Build,
        product.TargetVersion.Revision);

      outDir = Path.Combine(Path.GetDirectoryName(context.ProjectFileName), outDir);

      if (Directory.Exists(outDir))
        Directory.Delete(outDir, true);
      Directory.CreateDirectory(outDir);

      context.BuildMessageWriteLine("Построенные MSP-файлы:", BuildMessageTypes.Information);

      foreach (string msiFile in msiFiles)
      {
        string outFile = Path.Combine(outDir, Path.GetFileName(msiFile));
        File.Copy(msiFile, outFile);
        context.BuildMessageWriteLine(outFile, BuildMessageTypes.Information);
      }
    }

    #endregion
  }
}

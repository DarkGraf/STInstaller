using System;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

using Microsoft.Tools.WindowsInstallerXml;

namespace WixSTExtension
{
  public class STCompiler : CompilerExtension
  {
    internal const int SqlExecuteOnInstall = 1;
    internal const int SqlExecuteOnReinstall = 2;
    internal const int SqlExecuteOnUninstall = 4;

    private XmlSchema schema;

    public STCompiler()
    {
      schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "WixSTExtension.STSchema.xsd");
    }

    #region CompilerExtension

    public override XmlSchema Schema
    {
      get { return schema; }
    }

    public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, 
      XmlElement parentElement, XmlElement element, params string[] contextValues)
    {
      switch (parentElement.LocalName)
      {
        case "Product":
          switch (element.LocalName)
          {
            case "UIType":
              ParseUITypeElement(element);
              break;
            case "MefPlugin":
              ParseMefPluginElement(element);
              break;
            default:
              Core.UnexpectedElement(parentElement, element);
              break;
          }
          break;
        case "Component":
          string componentId = contextValues[0];
          switch (element.LocalName)
          {
            case "SqlTemplateFiles":
              ParseSqlTemplateFilesElement(element, componentId);
              break;
            case "SqlScriptFile":
              ParseSqlScriptFileElement(element, componentId);
              break;
            case "SqlExtendedProcedures":
              ParseSqlExtendedProcedures(element, componentId);
              break;
            default:
              Core.UnexpectedElement(parentElement, element);
              break;
          }
          break;
        default:
          Core.UnexpectedElement(parentElement, element);
          break;
      }
    }

    #endregion

    private void ParseUITypeElement(XmlNode node)
    {
      SourceLineNumberCollection sourceLineNumber = Preprocessor.GetSourceLineNumbers(node);

      string id = "";
      string type = "";

      // Получаем значения атрибутов элемента.
      foreach (XmlAttribute attribute in node.Attributes)
      {
        if (attribute.NamespaceURI.Length == 0 || attribute.NamespaceURI == schema.TargetNamespace)
        {
          switch (attribute.LocalName)
          {
            case "Id":
              // Метод GetAttributeIdentifierValue реализует дополнительные проверки символов для идентификатора.
              id = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              break;
            case "Type":
              type = Core.GetAttributeValue(sourceLineNumber, attribute);
              break;
            default:
              Core.UnexpectedAttribute(sourceLineNumber, attribute);
              break;
          }
        }
        else
          Core.UnsupportedExtensionAttribute(sourceLineNumber, attribute);
      }

      // Проверяем на заполнение элементов.
      if (string.IsNullOrEmpty(id))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Id"));
      if (string.IsNullOrEmpty(type))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Type"));

      // Пишем в таблицу.
      if (!Core.EncounteredError)
      {
        Row row = Core.CreateRow(sourceLineNumber, "UIType");
        row[0] = id;
        row[1] = type;
      }

      // Создаем связь между wixlib и msi.
      Core.CreateWixSimpleReferenceRow(sourceLineNumber, "CustomAction", "InstallerInitializeUI");
    }

    // Запрещаем использование нескольких элементов SqlTemplateFiles.
    private int sqlTemplateFilesElementsCount = 0;

    private void ParseSqlTemplateFilesElement(XmlNode node, string componentId)
    {
      SourceLineNumberCollection sourceLineNumber = Preprocessor.GetSourceLineNumbers(node);

      if (++sqlTemplateFilesElementsCount > 1)
        Core.OnMessage(WixErrors.PreprocessorError(sourceLineNumber, "Multiple declaration of an element SqlTemplateFiles."));

      string id = "";
      string mdfFileBinaryKey = "";
      string ldfFileBinaryKey = "";

      // Получаем значения атрибутов элемента.
      foreach (XmlAttribute attribute in node.Attributes)
      {
        if (attribute.NamespaceURI.Length == 0 || attribute.NamespaceURI == schema.TargetNamespace)
        {
          switch (attribute.LocalName)
          {
            case "Id":              
              id = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              break;
            case "MdfFileBinaryKey":
              mdfFileBinaryKey = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              Core.CreateWixSimpleReferenceRow(sourceLineNumber, "Binary", mdfFileBinaryKey);
              break;
            case "LdfFileBinaryKey":
              ldfFileBinaryKey = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              Core.CreateWixSimpleReferenceRow(sourceLineNumber, "Binary", ldfFileBinaryKey);
              break;
            default:
              Core.UnexpectedAttribute(sourceLineNumber, attribute);
              break;
          }
        }
        else
          Core.UnsupportedExtensionAttribute(sourceLineNumber, attribute);
      }

      // Проверяем на заполнение элементов.
      if (string.IsNullOrEmpty(id))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Id"));
      if (string.IsNullOrEmpty(mdfFileBinaryKey))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "MdfFileBinaryKey"));
      if (string.IsNullOrEmpty(ldfFileBinaryKey))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "LdfFileBinaryKey"));

      // Пишем в таблицу.
      if (!Core.EncounteredError)
      {
        Row row = Core.CreateRow(sourceLineNumber, "SqlTemplateFiles");
        row[0] = id;
        row[1] = componentId;
        row[2] = mdfFileBinaryKey;
        row[3] = ldfFileBinaryKey;
      }

      // Создаем связь между wixlib и msi.
      Core.CreateWixSimpleReferenceRow(sourceLineNumber, "CustomAction", "RestoringDataBaseImmediate");
    }

    private void ParseSqlScriptFileElement(XmlNode node, string componentId)
    {
      SourceLineNumberCollection sourceLineNumber = Preprocessor.GetSourceLineNumbers(node);

      string script = null;
      string binaryKey = null;
      int sequence = CompilerCore.IntegerNotSet;
      int flags = 0;

      // Получаем значения атрибутов элемента.
      foreach (XmlAttribute attribute in node.Attributes)
      {
        if (attribute.NamespaceURI.Length == 0 || attribute.NamespaceURI == schema.TargetNamespace)
        {
          switch (attribute.LocalName)
          {
            case "Id":
              script = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              break;
            case "BinaryKey":
              binaryKey = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              Core.CreateWixSimpleReferenceRow(sourceLineNumber, "Binary", binaryKey);
              break;
            case "Sequence":
              sequence = Core.GetAttributeIntegerValue(sourceLineNumber, attribute, 1, short.MaxValue);
              break;
            case "ExecuteOnInstall":
              if (Core.GetAttributeYesNoValue(sourceLineNumber, attribute) == YesNoType.Yes)
                flags |= SqlExecuteOnInstall;
              break;
            case "ExecuteOnReinstall":
              if (Core.GetAttributeYesNoValue(sourceLineNumber, attribute) == YesNoType.Yes)
                flags |= SqlExecuteOnReinstall;
              break;
            case "ExecuteOnUninstall":
              if (Core.GetAttributeYesNoValue(sourceLineNumber, attribute) == YesNoType.Yes)
                flags |= SqlExecuteOnUninstall;
              break;
            default:
              Core.UnexpectedAttribute(sourceLineNumber, attribute);
              break;
          }
        }
        else
          Core.UnsupportedExtensionAttribute(sourceLineNumber, attribute);
      }

      // Проверяем на заполнение элементов.
      if (string.IsNullOrEmpty(script))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Id"));
      if (string.IsNullOrEmpty(binaryKey))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "BinaryKey"));
      if (flags == 0)
        Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumber, node.Name, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));

      // Пишем в таблицу.
      if (!Core.EncounteredError)
      {
        Row row = Core.CreateRow(sourceLineNumber, "SqlScriptFile");
        row[0] = script;
        row[1] = componentId;
        row[2] = binaryKey;
        if (CompilerCore.IntegerNotSet != sequence)
          row[3] = sequence;
        row[4] = flags;
      }

      // Создаем связь между wixlib и msi.
      Core.CreateWixSimpleReferenceRow(sourceLineNumber, "CustomAction", "RunSqlScriptImmediate");
    }

    private void ParseSqlExtendedProcedures(XmlNode node, string componentId)
    {
      SourceLineNumberCollection sourceLineNumber = Preprocessor.GetSourceLineNumbers(node);

      string id = "";
      string name = "";
      string binaryKey = "";

      // Получаем значения атрибутов элемента.
      foreach (XmlAttribute attribute in node.Attributes)
      {
        if (attribute.NamespaceURI.Length == 0 || attribute.NamespaceURI == schema.TargetNamespace)
        {
          switch (attribute.LocalName)
          {
            case "Id":
              id = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              break;
            case "Name":
              name = Core.GetAttributeLongFilename(sourceLineNumber, attribute, false);
              break;
            case "BinaryKey":
              binaryKey = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              Core.CreateWixSimpleReferenceRow(sourceLineNumber, "Binary", binaryKey);
              break;
            default:
              Core.UnexpectedAttribute(sourceLineNumber, attribute);
              break;
          }
        }
        else
          Core.UnsupportedExtensionAttribute(sourceLineNumber, attribute);
      }

      // Проверяем на заполнение элементов.
      if (string.IsNullOrEmpty(id))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Id"));
      if (string.IsNullOrEmpty(name))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Name"));
      if (string.IsNullOrEmpty(binaryKey))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "BinaryKey"));

      // Пишем в таблицу.
      if (!Core.EncounteredError)
      {
        Row row = Core.CreateRow(sourceLineNumber, "SqlExtendedProcedures");
        row[0] = id;
        row[1] = componentId;
        row[2] = binaryKey;
        row[3] = name;
      }

      // Создаем связь между wixlib и msi.
      Core.CreateWixSimpleReferenceRow(sourceLineNumber, "CustomAction", "InstallingExtendedProceduresImmediate");
    }

    private void ParseMefPluginElement(XmlNode node)
    {
      SourceLineNumberCollection sourceLineNumber = Preprocessor.GetSourceLineNumbers(node);

      string id = "";
      string plugin = "";
      int sequence = CompilerCore.IntegerNotSet;

      // Получаем значения атрибутов элемента.
      foreach (XmlAttribute attribute in node.Attributes)
      {
        if (attribute.NamespaceURI.Length == 0 || attribute.NamespaceURI == schema.TargetNamespace)
        {
          switch (attribute.LocalName)
          {
            case "Id":
              id = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              break;
            case "BinaryKey":
              plugin = Core.GetAttributeIdentifierValue(sourceLineNumber, attribute);
              Core.CreateWixSimpleReferenceRow(sourceLineNumber, "Binary", plugin);
              break;
            case "Sequence":
              sequence = Core.GetAttributeIntegerValue(sourceLineNumber, attribute, 1, short.MaxValue);
              break;
            default:
              Core.UnexpectedAttribute(sourceLineNumber, attribute);
              break;
          }
        }
        else
          Core.UnsupportedExtensionAttribute(sourceLineNumber, attribute);
      }

      // Проверяем на заполнение элементов.
      if (string.IsNullOrEmpty(id))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "Id"));
      if (string.IsNullOrEmpty(plugin))
        Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumber, node.Name, "BinaryKey"));

      // Пишем в таблицу.
      if (!Core.EncounteredError)
      {
        Row row = Core.CreateRow(sourceLineNumber, "MefPlugin");
        row[0] = id;
        row[1] = plugin;
        if (CompilerCore.IntegerNotSet != sequence)
          row[2] = sequence;
      }

      // Создаем связь между wixlib и msi.
      Core.CreateWixSimpleReferenceRow(sourceLineNumber, "CustomAction", "AfterInstallInitializeImmediate");
    }
  }
}
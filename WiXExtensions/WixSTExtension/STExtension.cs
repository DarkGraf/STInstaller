using System;
using System.Reflection;

using Microsoft.Tools.WindowsInstallerXml;

namespace WixSTExtension
{
  public class STExtension : WixExtension
  {
    private CompilerExtension compilerExtension;
    private TableDefinitionCollection tableDefinition;
    private Library library;

    public override CompilerExtension CompilerExtension
    {
      get
      {
        if (compilerExtension == null)
          compilerExtension = new STCompiler();

        return compilerExtension;
      }
    }

    public override TableDefinitionCollection TableDefinitions
    {
      get
      {
        if (tableDefinition == null)
          tableDefinition = LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(), "WixSTExtension.TableDefinitions.xml");

        return tableDefinition;
      }
    }

    public override Library GetLibrary(TableDefinitionCollection tableDefinitions)
    {
      if (library == null)
        library = LoadLibraryHelper(Assembly.GetExecutingAssembly(), "WixSTExtension.WixSTLibrary.wixlib", tableDefinitions);

      return library;
    }
  }
}

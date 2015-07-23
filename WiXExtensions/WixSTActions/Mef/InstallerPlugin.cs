using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.Mef
{
  /// <summary>
  /// Имена CustomAction.
  /// </summary>
  // При добавлении новых действий необходимо сохранить нумерацию для поддержки 
  // предыдущих версий.
  public enum CustomActionNames
  {
    AfterInstallInitializeImmediate = 1,
    AfterInstallInitializeDeferred = 2,
    AfterInstallInitializeRollback = 3,
    BeforeInstallFinalizeImmediate = 4,
    BeforeInstallFinalizeDeferred = 5,
    BeforeInstallFinalizeRollback = 6
  }

  /// <summary>
  /// Расширение ExportAttribute для экспорта метаданных.
  /// </summary>
  [MetadataAttribute]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  // Наследование от IInstallerPluginExportAttribute не обязательно, MEF работает и без
  // этого, но для сохранения строгой типизации воспользуемся им.
  public class InstallerPluginExportAttribute : ExportAttribute, IInstallerPluginExportMetadata
  {
    public InstallerPluginExportAttribute(CustomActionNames action)
      : base(typeof(IInstallerPlugin))
    {
      Action = action;
    }

    public CustomActionNames Action { get; private set; }
  }

  /// <summary>
  /// Доступ к метаданным.
  /// </summary>
  public interface IInstallerPluginExportMetadata
  {
    CustomActionNames Action { get; }
  }

  /// <summary>
  /// Интерфейс расширения MEF.
  /// </summary>
  public interface IInstallerPlugin
  {
    ActionResult DoAction(Session session);
  }

  /// <summary>
  /// Класс для инкапсуляции использования расширения.
  /// </summary>
  public class InstallerPlugins
  {
    AggregateCatalog catalog;
    CompositionContainer container;

    [ImportMany(typeof(IInstallerPlugin))]
    public IEnumerable<Lazy<IInstallerPlugin, IInstallerPluginExportMetadata>> Plugins { get; set; }

    public InstallerPlugins(string[] files)
    {
      if (files != null && files.Length != 0)
      {
        catalog = new AggregateCatalog();
        Array.ForEach<string>(files, (file) =>
        {
          // Если загрузить файлы как Assembly.LoadFile(file), то потом их нельзя
          // будет удалить. Поэтому используем этот механизм загрузки.
          Assembly assembly = Assembly.Load(File.ReadAllBytes(file));
          catalog.Catalogs.Add(new AssemblyCatalog(assembly));
        });

        container = new CompositionContainer(catalog);
        container.ComposeParts(this);
      }
    }

    /// <summary>
    /// Выполнить все плагины.
    /// </summary>
    public ActionResult Run(Session session, string actionName)
    {
      ActionResult actionResult = ActionResult.Success;

      if (Plugins != null)
      {
        // На основе двух перечислений формируем имя метода и сравниваем его с переданным.
        foreach (Lazy<IInstallerPlugin, IInstallerPluginExportMetadata> plugin in Plugins.Where(p => p.Metadata.Action.ToString() == actionName))
        {
          actionResult = plugin.Value.DoAction(session);
        }
      }

      return actionResult;
    }
  }
}
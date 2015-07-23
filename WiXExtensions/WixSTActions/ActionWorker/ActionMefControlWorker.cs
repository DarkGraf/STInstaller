using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;

using WixSTActions.Mef;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  /// <summary>
  /// Управление MEF-плагинами. 
  /// </summary>
  class ActionMefControlWorker : ActionWorkerBase
  {
    private const string keyIsPrepare = "ActionMefControlWorkerIsPrepare";
    bool initialize;

    /// <summary>
    /// Создает экземпляр.
    /// Первый вызов должен происходить строго в фазе Immediate.
    /// </summary>
    public ActionMefControlWorker(Session session, bool initialize = false)
      : base(session)
    {
      this.initialize = initialize;
    }

    protected override UIType UITypeMode
    {
      get { return UIType.Server | UIType.Client; }
    }

    protected override bool IsAllowed
    {
      get { return true; }
    }

    protected override ActionResult DoWork()
    {
      if (initialize)
      {
        string[] files;
        // Проверяем, произошли ли подготовительные действия.
        if ((files = Session.GetService<ISessionSerializeCustomActionDataExtension>().
          DeserializeCustomActionData<string[]>(keyIsPrepare)) == null)
        {
          files = Prepare();
          // Сохраним файлы для всех действий.
          Session.GetService<ISessionSerializeCustomActionDataExtension>().
            SerializeCustomActionData<string[]>(files, keyIsPrepare);
        }
      }

      return RunPlugins();
    }

    /// <summary>
    /// Подготовительные действия. Возвращает массив плагинов.
    /// </summary>
    private string[] Prepare()
    {
      DataTable tablePlugin = Session.GetService<ISessionDataTableExtension>().
        CopyTableInfoToDataTable("MefPlugin");
      DataTable tableBinary = Session.GetService<ISessionDataTableExtension>().
        CopyTableInfoToDataTable("Binary");

      // Если таблицы пустые возвращаем пустой массив, так как null является признаком 
      // вызова данного метода.
      if (tablePlugin == null || tableBinary == null)
        return new string[0];

      // Получаем файлы расширения.
      var plugins = from plugin in tablePlugin.AsEnumerable()
                    join binary in tableBinary.AsEnumerable()
                    on plugin.Field<string>("Plugin") equals binary.Field<string>("Name")
                    select new
                    {
                      Id = plugin.Field<string>("Id"),
                      Plugin = binary.Field<byte[]>("Data"),
                      Sequence = plugin.Field<short?>("Sequence") ?? short.MaxValue
                    };

      // Сохраняем файлы на диск, во временную директорию.
      // Файлы должны удалиться другим действием вместе с общей временной директорией.
      IList<string> files = new List<string>();
      foreach (var plugin in plugins.OrderBy(v => v.Sequence))
      {
        string path = Path.Combine(Session.GetService<ISessionTempDirectoryExtension>().
          GetTempDirectory(), Path.ChangeExtension(Path.GetRandomFileName(), "dll"));
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
          stream.Write(plugin.Plugin, 0, plugin.Plugin.Length);
        }

        files.Add(path);
      }

      return files.ToArray();
    }

    private ActionResult RunPlugins()
    {
      string[] files = Session.GetService<ISessionSerializeCustomActionDataExtension>().
        DeserializeCustomActionData<string[]>(keyIsPrepare);
      // Инициализация инфраструктуры MEF.
      InstallerPlugins plugins = new InstallerPlugins(files);
      return plugins.Run(Session, Session.GetService<ISessionCustomActionNameGetterExtension>().
        GetCustomActionName());
    }
  }
}

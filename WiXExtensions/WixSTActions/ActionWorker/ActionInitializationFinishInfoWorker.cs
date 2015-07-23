using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;
using WixSTActions.WixControl;

namespace WixSTActions.ActionWorker
{
  interface IInitializationFinishInfoSessionProperties
  {
    /// <summary>
    /// Элемент для вывода итоговой информации об установке.
    /// </summary>
    string ControlProperty { get; }

    /// <summary>
    /// Тип установки.
    /// </summary>
    string InstallMode { get; }

    /// <summary>
    /// Директория для резервных копий.
    /// </summary>
    string BackupDirectory { get; }

    /// <summary>
    /// Принимает любое значение для того чтобы элемент управления был видим.
    /// </summary>
    string ControlVisible { get; set; }
  }

  class ActionInitializationFinishInfoWorker : ActionWorkerBase
  {
    IInitializationFinishInfoSessionProperties sessionProp;
    bool clear;

    public ActionInitializationFinishInfoWorker(Session session, IInitializationFinishInfoSessionProperties sessionProp, bool clear)
      : base(session) 
    {
      this.sessionProp = sessionProp;
      this.clear = clear;
    }

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override bool IsAllowed
    {
      get { return true; }
    }

    protected override ActionResult DoWork()
    {
      WixListBox listBox = new WixListBox(Session, sessionProp.ControlProperty);
      listBox.ClearItems();

      List<string> list = new List<string>();
      // Используем пробелы для отступов.       
      string tab = new string(' ', 4);

      // Если вставлять пустую строку, то будет отображаться ее значение.
      // Если вставлять одинаковую строку, то будет нарушено перемещение.
      // Будем вставлять пробелы, добавляя в каждый раз новый.
      string spaces = string.Empty;
      Func<string> GetSpaces = delegate { return spaces += " "; };
      
      // Тип установки.
      list.Add("Тип установки: " + sessionProp.InstallMode);
      list.Add(GetSpaces());

      // Новые базы данных.
      var listNewDataBase = from db in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
                            where db.IsNew
                            select db;
      if (listNewDataBase.Count() > 0)
      {
        list.Add("Устанавливаемые базы данных:");
        foreach (var db in listNewDataBase)
          list.Add(string.Format("{0}{1}.{2} ({3})", tab, db.Server, db.Name, db.Version));
        list.Add(GetSpaces());
      }

      // Обновляемые базы данных.
      var listExistDataBase = from db in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
                            where db.IsRequiringUpdate && !db.IsNew
                            select db;
      if (listExistDataBase.Count() > 0)
      {
        list.Add("Обновляемые базы данных:");
        foreach (var db in listExistDataBase)
          list.Add(string.Format("{0}{1}.{2} ({3})", tab, db.Server, db.Name, db.Version));
        list.Add(GetSpaces());
      }

      // Директория для резервных копий.
      list.Add("Директория для резервных копий: " + sessionProp.BackupDirectory);

      // Заполняем ListBox.
      for (int i = 0; i < list.Count; i++)
        listBox.AddItem(new WixListBoxItem(list[i], i.ToString()));

      listBox.SelectedValue = "";

      // Показываем элемент если не выставлен флаг очистки.
      sessionProp.ControlVisible = clear ? "" : "1";

      return ActionResult.Success;
    }
  }
}

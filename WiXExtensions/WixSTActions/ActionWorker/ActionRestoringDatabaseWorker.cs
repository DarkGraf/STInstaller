using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;

using WixSTActions.SqlWorker;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  // Доступ public нужен для внутренней сериализации.
  public class ActionRestoringDataBaseWorkerSavedData
  {
    public string InstallPath { get; set; }
    public string MdfPath { get; set; }
    public string LdfPath { get; set; }
  }

  /// <summary>
  /// Действие восстановления базы данных (только для новых БД).
  /// </summary>
  class ActionRestoringDatabaseWorker : ActionExtensionWorker
  {
    ISqlWorkersFactory factory;
    /// <summary>
    /// Данное поле нужно только для вывода имени базы данных в UI.
    /// </summary>
    private string currentDatabaseName = "";

    public ActionRestoringDatabaseWorker(Session session, ISqlWorkersFactory factory, string[] subscribers = null) 
      : base(session, subscribers)
    {
      this.factory = factory;
    }

    #region ActionExtensionWorker

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override CurrentInstallStatus WorkingStatus
    {
      get { return CurrentInstallStatus.Install | CurrentInstallStatus.Change; }
    }

    protected void UpdateActionDescription(string databaseName)
    {
      currentDatabaseName = databaseName;
      UpdateActionDescription();
    }

    protected override ActionExtensionWorker.ActionDescription GetActionDescription(InstallPhase phase)
    {
      string mes = "";
      string nameForDisplay = string.IsNullOrEmpty(currentDatabaseName) ? "" : " " + currentDatabaseName;

      switch (phase)
      {
        case InstallPhase.Immediate:
          mes = "Подготовка к восстановлению баз данных.";
          break;
        case InstallPhase.Deferred:
          mes = "Восстановление базы данных{0}.";
          mes = string.Format(mes, nameForDisplay);
          break;
        case InstallPhase.Rollback:
          mes = "Откат восстановления базы данных{0}.";
          mes = string.Format(mes, nameForDisplay);
          break;
      }
      return new ActionDescription { Name = mes, Description = mes };
    }

    protected override ActionResult DoWorkImmediate()
    {
      ActionRestoringDataBaseWorkerSavedData data = null;

      try
      {
        // Узнаем, есть ли базы данных указанные пользователем как новые.
        DatabaseInfo[] databases = GetNewDatabases();
        if (databases.Length == 0)
          return ActionResult.Success;

        // Получаем информацию об устанавливаемых шаблонах.
        // Дополнительное условие: так как данная функциональность нужна только для установки
        // новых БД, компонент должен требовать установки или быть уже установленным
        // (внимание, Reinstall выполняться не будет).
        // Если записей несколько (не должно быть), берем первую, остальные игнорируем.
        var template = (from file in Session.GetService<ISessionDataTableExtension>().
                          CopyTableInfoToDataTable("SqlTemplateFiles").AsEnumerable()
                        where (Session.GetService<ISessionComponentInstallStatusExtension>().
                          GetStatus(file.Field<string>("Component")) & (ComponentInstallStatus.Install | ComponentInstallStatus.AlreadyInstalled)) != ComponentInstallStatus.Unknow
                        select new
                        {
                          mdfFile = file.Field<string>("MdfFileBinaryKey"),
                          ldfFile = file.Field<string>("LdfFileBinaryKey"),
                          componentId = file.Field<string>("Component")
                        }).FirstOrDefault();
        if (template == null)
          return ActionResult.Success;

        // Получаем файлы из таблицы Binary, соответствующие SqlTemplateFiles.
        var records = (from binary in Session.GetService<ISessionDataTableExtension>().
                         CopyTableInfoToDataTable("Binary").AsEnumerable()
                       where binary.Field<string>("Name") == template.mdfFile
                         || binary.Field<string>("Name") == template.ldfFile
                       select new
                       {
                         DataBytes = binary.Field<byte[]>("Data"),
                         IsMdf = binary.Field<string>("Name") == template.mdfFile
                       }).ToArray();

        // Должно быть две записи, причем одна из них mdf файл, другая ldf.
        if (records.Length == 2 && records.FirstOrDefault(v => v.IsMdf) != null && records.FirstOrDefault(v => !v.IsMdf) != null)
        {
          data = new ActionRestoringDataBaseWorkerSavedData();

          foreach (var record in records)
          {
            // Сохраняем файлы во временную папку и передаем подписчикам.
            string path = Path.Combine(Session.GetService<ISessionTempDirectoryExtension>().GetTempDirectory(), Path.GetRandomFileName());
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
              stream.Write(record.DataBytes, 0, record.DataBytes.Length);
            }

            if (record.IsMdf)
              data.MdfPath = path;
            else
              data.LdfPath = path;
          }

          // Получаем директорию установки баз данных. Директория представлена свойством.
          string propertyDirectory = (from c in Session.GetService<ISessionDataTableExtension>().
                                        CopyTableInfoToDataTable("Component").AsEnumerable()
                                      where c.Field<string>("Component") == template.componentId
                                      select c.Field<string>("Directory_")).FirstOrDefault();
          data.InstallPath = Session[propertyDirectory];
        }

        return ActionResult.Success;
      }
      finally
      {
        // Передаем данные подписчикам.
        Session.GetService<ISessionSerializeCustomActionDataExtension>().
          SerializeCustomActionData<ActionRestoringDataBaseWorkerSavedData>(data, Subscribers);
      }
    }

    protected override ActionResult DoWorkDeferred()
    {
      // Получаем и десериализуем данные.
      // Если данные есть, то значит как минимум одну базы надо присоединять.
      ActionRestoringDataBaseWorkerSavedData data = Session.
        GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<ActionRestoringDataBaseWorkerSavedData>();
      if (data == null)
        return ActionResult.Success;

      DatabaseInfo[] databases = GetNewDatabases();

      // Присоединяем каждую базу.
      foreach (DatabaseInfo database in databases)
      {
        UpdateActionDescription(string.Format("{0}.{1}", database.Server, database.Name));

        // Создаем имена файлов.
        string mdfFile = DatabaseFileNameMaker.Make(database.Server, database.Name, data.InstallPath, "mdf");
        string ldfFile = DatabaseFileNameMaker.Make(database.Server, database.Name, data.InstallPath, "ldf");
        // Копируем файлы.
        File.Copy(data.MdfPath, mdfFile);
        File.Copy(data.LdfPath, ldfFile);
        // Выполняем присоединение базы.
        ISqlAttachDatabaseWorkerReturnedData returnedData;
        SqlWorkerBase worker = factory.CreateSqlAttachDatabaseWorker(database.Server, database.Name,
          AuthenticationType.Windows, "", "", mdfFile, ldfFile, out returnedData);
        worker.Execute();
        if (!returnedData.DatabaseCreated)
          return ActionResult.Failure;
      }

      return ActionResult.Success;
    }

    protected override ActionResult DoWorkRollback()
    {
      // Получаем и десериализуем данные.
      ActionRestoringDataBaseWorkerSavedData data = Session.
        GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<ActionRestoringDataBaseWorkerSavedData>();
      if (data == null)
        return ActionResult.Success;

      DatabaseInfo[] databases = GetNewDatabases();

      foreach (DatabaseInfo database in databases)
      {
        UpdateActionDescription(string.Format("{0}.{1}", database.Server, database.Name));

        // Если база данных есть, то отсоединяем ее.
        SqlWorkerBase worker = factory.CreateSqlDetachDatabaseWorker(database.Server, database.Name, AuthenticationType.Windows, "", "");
        worker.Execute();
        
        // Получаем имена файлов как в Deferred.
        string mdfFile = DatabaseFileNameMaker.Make(database.Server, database.Name, data.InstallPath, "mdf");
        string ldfFile = DatabaseFileNameMaker.Make(database.Server, database.Name, data.InstallPath, "ldf");

        // Удаляем файлы.
        if (File.Exists(mdfFile))
          File.Delete(mdfFile);
        if (File.Exists(ldfFile))
          File.Delete(ldfFile);
      }
      
      return ActionResult.Success;
    }

    #endregion

    private DatabaseInfo[] GetNewDatabases()
    {
      return (from db in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
              where db.IsNew
              select db).ToArray();
    }
  }
}

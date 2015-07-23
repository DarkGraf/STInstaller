using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;
using WixSTActions.SqlWorker;

namespace WixSTActions.ActionWorker
{
  public class ActionRunSqlScriptWorkerSavedData
  {
    public string Server { get; set; }
    public string Database { get; set; }
    public string Path { get; set; }
    public short Sequence { get; set; }
    public string Name { get; set; }
  }

  abstract class ActionRunSqlScriptWorker : ActionProgressExtensionWorker
  {
    protected ISqlWorkersFactory Factory { get; private set; }
    /// <summary>
    /// Данное поле нужно только для отображения информации в UI.
    /// </summary>
    private string currentScriptName = "";
    private string currentDatabaseName = "";

    public ActionRunSqlScriptWorker(Session session, ISqlWorkersFactory factory, string[] subscribers = null)
      : base(session, subscribers)
    {
      this.Factory = factory;
    }

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override CurrentInstallStatus WorkingStatus
    {
      // Режим работы: первоначальная установка и переустановка этой же версии.
      get { return CurrentInstallStatus.Install | CurrentInstallStatus.Change; }
    }

    protected void UpdateActionDescription(string script, string database)
    {
      currentScriptName = script;
      currentDatabaseName = database;
      UpdateActionDescription();
    }

    protected override ActionExtensionWorker.ActionDescription GetActionDescription(InstallPhase phase)
    {
      string mes = "";
      string info = string.IsNullOrEmpty(currentScriptName) ? "" : " " + currentScriptName + " для " + currentDatabaseName;

      switch (phase)
      {
        case InstallPhase.Immediate:
          mes = "Подготовка к выполнению SQL-скрипта.";
          break;
        case InstallPhase.Deferred:
          mes = "Выполнение SQL-скрипта{0}.";
          mes = string.Format(mes, info);
          break;
        case InstallPhase.Rollback:
          mes = "Откат выполнения SQL-скрипта.";
          break;
      }
      return new ActionDescription { Name = mes, Description = mes };

    }

    protected override ActionResult DoWorkImmediate()
    {
      IList<ActionRunSqlScriptWorkerSavedData> data = new List<ActionRunSqlScriptWorkerSavedData>();
      try
      {
        var files = (from file in Session.GetService<ISessionDataTableExtension>().
                      CopyTableInfoToDataTable("SqlScriptFile").AsEnumerable()
                    from db in GetDatabases() // Это "Cross Join".
                    where
                      FilesFilterForImmediateMode(
                        Session.GetService<ISessionComponentInstallStatusExtension>().GetStatus(file.Field<string>("Component")),
                        (ComponentInstallStatus)file.Field<short>("Attributes"),
                        db.IsNew, db.IsRequiringUpdate)
                    select new ActionRunSqlScriptWorkerSavedData
                    {
                      Server = db.Server,
                      Database = db.Name,
                      Path = "",
                      Sequence = file.Field<short>("Sequence"),
                      Name = file.Field<string>("ScriptBinary")
                    }).ToArray();

        if (files.Length == 0)
          return ActionResult.Success;

        // Получаем файлы скриптов.
        var physicalFiles = (from file in files.Select(v => v.Name).Distinct()
                            join binary in Session.GetService<ISessionDataTableExtension>().
                              CopyTableInfoToDataTable("Binary").AsEnumerable()
                            on file equals binary.Field<string>("Name")
                            select new
                            {
                              Name = file,
                              File = binary.Field<byte[]>("Data")
                            }).ToArray();

        // Сохраняем файлы на диск во временную папку.
        // Запоминаем также необходимую информацию.
        foreach (var physicalFile in physicalFiles)
        {
          string path = Path.Combine(Session.GetService<ISessionTempDirectoryExtension>().GetTempDirectory(), Path.GetRandomFileName());
          using (FileStream stream = new FileStream(path, FileMode.Create))
          {
            stream.Write(physicalFile.File, 0, physicalFile.File.Length);
          }

          for (int i = 0; i < files.Length; i++)
          {
            if (files[i].Name == physicalFile.Name)
            {
              files[i].Path = path;
              data.Add(files[i]);
            }
          }
        }

        return ActionResult.Success;
      }
      finally
      {
        // Передаем данные подписчикам. В качества ключа используем имя типа, так как 
        // класс далее наследуется и необходимо различать параметры для для производных типов.
        Session.GetService<ISessionSerializeCustomActionDataExtension>().
          SerializeCustomActionData<ActionRunSqlScriptWorkerSavedData[]>(data.ToArray(), GetType().Name, Subscribers);
      }
    }

    protected override ActionResult DoWorkDeferred()
    {
      ActionResult result = ActionResult.Success;

      // Получаем и десериализуем данные.
      ActionRunSqlScriptWorkerSavedData[] data = Session.
        GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<ActionRunSqlScriptWorkerSavedData[]>(GetType().Name);
      if (data == null || data.Length == 0)
        return ActionResult.Success;

      DatabaseInfo[] databases = GetDatabases();

      foreach (DatabaseInfo database in databases)
      {
        // Для каждой базы данных для разных Worker будем использовать
        // только одно соединение. Это нужно для оптимизации подключения
        // и необходимо для работы с однопользовательским режимом, чтобы 
        // в моменыт подключение/отключения не подсоединился какой-нибудь
        // другой процесс MS SQL (фоновый или пользователя).
        ISqlWorkerConnection connection;
        SqlWorkerBase connectioWorker = Factory.CreateSqlCreateConnectionWorker(database.Server, database.Name,
          AuthenticationType.Windows, "", "", out connection);

        RunSqlScriptBefore(database.Server, database.Name, connection);

        try
        {
          // Каждый скрипт выполняем для каждой базы.
          foreach (ActionRunSqlScriptWorkerSavedData d in data.Where(v => v.Server == database.Server && v.Database == database.Name).OrderBy(v => v.Sequence))
          {
            // Для отображения в UI.
            // При отмене инсталляции, может выскочить исключение InstallCanceledException
            // при вызове Session.Message(). Если это случилось, обработаем его в блоке catch.
            UpdateActionDescription(d.Name, database.Name);

            if (!File.Exists(d.Path))
              throw new FileNotFoundException();

            // Читаем полностью скрипт, разбивая его на подскрипты.
            SqlScriptParser parser = new SqlScriptParser(d.Path);

            SqlWorkerBase worker = Factory.CreateSqlRunScriptWorker(connection, parser, OnSqlWorkerProgress);
            worker.Execute();
          }
        }
        catch (Exception ex)
        {
          if (ex is InstallCanceledException)
            result = ActionResult.UserExit;
          // Если произошел сбой, вызываем специальный метод.
          RunSqlScriptFault(database.Server, database.Name, connection, ex);
        }

        RunSqlScriptAfter(database.Server, database.Name, connection);

        // Закрываем соединение.
        connectioWorker.Execute();
      }

      // Удаляем файл.
      foreach (string d in data.Select(v => v.Path).Distinct())
        File.Delete(d);

      return result;
    }

    private void OnSqlWorkerProgress(object sender, SqlWorkerProgressEventArgs e)
    {
      OnProgress(e.AllCount, e.Increment, e.IsInitializedData);
    }

    /// <summary>
    /// Получает массив баз данных над которыми будет выполняться скрипт.
    /// </summary>
    /// <returns></returns>
    protected abstract DatabaseInfo[] GetDatabases();

    /// <summary>
    /// Фильтр для получения файлов для режима Immediate.
    /// </summary>
    /// <param name="desiredStatus">Желаемый установочный статус компонента.</param>
    /// <param name="allowedStatus">Разрешенный установочный статус компонента заданный для свойств SQL-скрипта.
    /// Может содержать несколько значений.</param>
    /// <param name="isNew">Признак новой базы данных.</param>
    /// <param name="isRequiringUpdate">Признак, требует ли база данных обновление.</param>
    /// <returns></returns>
    protected abstract bool FilesFilterForImmediateMode(ComponentInstallStatus desiredStatus, ComponentInstallStatus allowedStatus,
      bool isNew, bool isRequiringUpdate);

    /// <summary>
    /// Вызывается перед выполнением Sql-скрипта.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="database">Имя базы данных.</param>
    protected abstract void RunSqlScriptBefore(string server, string database, ISqlWorkerConnection connection);

    /// <summary>
    /// Вызывается после выполнением Sql-скрипта.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="database">Имя базы данных.</param>
    protected abstract void RunSqlScriptAfter(string server, string database, ISqlWorkerConnection connection);

    /// <summary>
    /// Вызывается при сбое выполнения Sql-скрипта.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="database">Имя базы данных.</param>
    /// <param name="ex">Исключение возникшее при сбое.</param>
    protected abstract void RunSqlScriptFault(string server, string database, ISqlWorkerConnection connection, Exception ex);
  }

  class ActionRunSqlScriptNewDbWorker : ActionRunSqlScriptWorker
  {
    public ActionRunSqlScriptNewDbWorker(Session session, ISqlWorkersFactory factory, string[] subscribers = null)
      : base(session, factory, subscribers) { }

    protected override DatabaseInfo[] GetDatabases()
    {
      // Получаем только новые базы данных.
      return (from db in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
              where db.IsNew
              select db).ToArray();
    }

    protected override bool FilesFilterForImmediateMode(ComponentInstallStatus desiredStatus, ComponentInstallStatus allowedStatus, bool isNew, bool isRequiringUpdate)
    {
      // Текущий статус установки.
      CurrentInstallStatus currentInstallStatus = Session.GetService<ISessionCurrentInstallStatusExtension>().GetStatus();

      // Новая установка.
      if (currentInstallStatus == CurrentInstallStatus.Install
        && desiredStatus == ComponentInstallStatus.Install
        && allowedStatus.HasFlag(ComponentInstallStatus.Install))
      {
        // Для новой базы запускаем скрипт.
        return isNew;
      }

      // Обслуживание (изменение этого же продукта).
      // Добавлена новая база. В этом случае также необходимо выполнить скрипты с атрибутом Install.
      if (currentInstallStatus == CurrentInstallStatus.Change
        // Компонент уже должен быть установлен.
        && desiredStatus == ComponentInstallStatus.AlreadyInstalled)
      {
        return isNew && (allowedStatus.HasFlag(ComponentInstallStatus.Install)
          || allowedStatus.HasFlag(ComponentInstallStatus.Reinstall));
      }

      // При обновлении и patch новую базу не добавляем, поэтому эту ситуацию не обрабатываем.

      return false;
    }

    protected override ActionResult DoWorkRollback()
    {
      // Для новой базы данных скрипт не откатываем, так как база все равно будет удалятся.
      return ActionResult.Success;
    }

    protected override void RunSqlScriptBefore(string server, string database, ISqlWorkerConnection connection)
    {
      // Ни какая предварительная работа для новой базы данных не требуется.
    }

    protected override void RunSqlScriptAfter(string server, string database, ISqlWorkerConnection connection)
    {
      // Ни какая предварительная работа для новой базы данных не требуется.
    }

    protected override void RunSqlScriptFault(string server, string database, ISqlWorkerConnection connection, Exception ex)
    {
      // Для новой базы данных, если произошел сбой, установка отменяется.
      throw ex;
    }
  }

  public class ActionRunSqlScriptExistingDbWorkerSavedData
  {
    /// <summary>
    /// Имя сервера.
    /// </summary>
    public string Server { get; set; }
    /// <summary>
    /// Имя базы данных.
    /// </summary>
    public string Database { get; set; }
    /// <summary>
    /// Путь для расположения резервных копий файлов базы данных.
    /// </summary>
    public string Path { get; set; }
  }

  interface IRunSqlScriptExistingDbSessionProperties
  {
    /// <summary>
    /// Путь к резервной директории.
    /// </summary>
    string BackupPath { get; }
  }

  class ActionRunSqlScriptExistingDbWorker : ActionRunSqlScriptWorker
  {
    IRunSqlScriptExistingDbSessionProperties sessionProp;

    public ActionRunSqlScriptExistingDbWorker(Session session, ISqlWorkersFactory factory,
      IRunSqlScriptExistingDbSessionProperties sessionProp = null, string[] subscribers = null)
      : base(session, factory, subscribers) 
    {
      this.sessionProp = sessionProp;
    }

    protected override CurrentInstallStatus WorkingStatus
    {
      get
      {
        // Для существующих баз разрешаем работать при обновлении.
        return base.WorkingStatus | CurrentInstallStatus.Update | CurrentInstallStatus.Patch;
      }
    }

    protected override DatabaseInfo[] GetDatabases()
    {
      // Получаем существующие базы данных требующие обновление.
      return (from db in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
              where !db.IsNew && db.IsRequiringUpdate
              select db).ToArray();
    }

    protected override bool FilesFilterForImmediateMode(ComponentInstallStatus desiredStatus, ComponentInstallStatus allowedStatus, bool isNew, bool isRequiringUpdate)
    {
      // Текущий статус установки.
      CurrentInstallStatus currentInstallStatus = Session.GetService<ISessionCurrentInstallStatusExtension>().GetStatus();

      // Новая установка.
      if (currentInstallStatus == CurrentInstallStatus.Install
        && desiredStatus == ComponentInstallStatus.Install
        && allowedStatus.HasFlag(ComponentInstallStatus.Install))
      {
        // Для базы требующей обновления запускаем скрипт.
        return isRequiringUpdate;
      }

      // Обслуживание (изменение этого же продукта).
      // Здесь будут базы с версией меньше устанавливаемой.
      // Базы с версией равной и больше текущей не должны обновлятся согласно возврату хранимой процедуры (по ТЗ).
      if (currentInstallStatus == CurrentInstallStatus.Change
        // Компонент уже должен быть установлен.
        && desiredStatus == ComponentInstallStatus.AlreadyInstalled
        && allowedStatus.HasFlag(ComponentInstallStatus.Reinstall))
      {
        return isRequiringUpdate;
      }

      // При обновлении запускаем скрипты только с признаком переустановки.
      if (currentInstallStatus == CurrentInstallStatus.Update
        && desiredStatus == ComponentInstallStatus.Reinstall
        && allowedStatus.HasFlag(ComponentInstallStatus.Reinstall))
      {
        return isRequiringUpdate;
      }

      // При patch запускаем скрипты только с признаком переустановки.
      if (currentInstallStatus == CurrentInstallStatus.Patch
        && desiredStatus == ComponentInstallStatus.AlreadyInstalled
        && allowedStatus.HasFlag(ComponentInstallStatus.Reinstall))
      {
        return isRequiringUpdate;
      }

      return false;
    }

    protected override ActionResult DoWorkImmediate()
    {
      // Сформируем для каждой базы данных уникальное имя резервной копии.
      DatabaseInfo[] databases = GetDatabases();

      // Разошлем подписчикам.
      ActionRunSqlScriptExistingDbWorkerSavedData[] data = null;
      if (databases.Length > 0)
      {
        // Резервная директория. К ней добавляем папку объединяющую все файлы.
        string path = Path.Combine(sessionProp.BackupPath, DateTime.Now.ToString("yyyyMMddHHmmss"));
        data = (from d in databases
                select new ActionRunSqlScriptExistingDbWorkerSavedData
                {
                  Server = d.Server,
                  Database = d.Name,
                  // Файлы одной базы хранятся внутри директории ..\Сервер\БазаДанных\
                  Path = Path.Combine(path, d.Server.Replace("\\", ""), d.Name), 
                }).ToArray();        
      }
      Session.GetService<ISessionSerializeCustomActionDataExtension>().
        SerializeCustomActionData<ActionRunSqlScriptExistingDbWorkerSavedData[]>(data, Subscribers);

      return base.DoWorkImmediate();
    }

    protected override ActionResult DoWorkRollback()
    {
      // TODO. Проверить, если прервали работу во время выполнения скриптов, то, возможно,
      // откат будет делаться для всех баз, что не корректно. Может нужно будет ориентироваться
      // по наличию файлов баз в резервной директории.
      DatabaseInfo[] databases = GetDatabases();

      // Выполняем откат для каждой базы.
      foreach (DatabaseInfo database in databases)
      {
        ISqlWorkerConnection connection;
        SqlWorkerBase connectioWorker = Factory.CreateSqlCreateConnectionWorker(database.Server, database.Name,
          AuthenticationType.Windows, "", "", out connection);

        RunSqlScriptFault(database.Server, database.Name, connection, null);

        connectioWorker.Execute();
      }

      return ActionResult.Success;
    }

    protected override void RunSqlScriptBefore(string server, string database, ISqlWorkerConnection connection)
    {
      string path;
      // Получаем путь к резервной директории.
      if (!GetInstallPath(server, database, out path))
        return;

      // Если директории не существует, то создаем ее.
      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);

      CopyingFilesBetweenBackupAndInstallation(server, database, connection, path, false);

      SqlWorkerBase worker;
      // Переключаемся на данную базу.
      worker = Factory.CreateSqlUseDatabaseWorker(connection, database);
      worker.Execute();

      // Переводим в Single-режим.
      worker = Factory.CreateSqlSetSingleUserWorker(connection);
      worker.Execute();
    }

    protected override void RunSqlScriptAfter(string server, string database, ISqlWorkerConnection connection)
    {
      // Отключаем Single-режим.
      SqlWorkerBase worker = Factory.CreateSqlSetMultiUserWorker(connection);
      worker.Execute();
    }

    protected override void RunSqlScriptFault(string server, string database, ISqlWorkerConnection connection, Exception ex)
    {
      // При сбое выполняем откат. Работу не прерываем.
      string path;
      // Получаем путь к резервной директории.
      if (!GetInstallPath(server, database, out path))
        return;

      CopyingFilesBetweenBackupAndInstallation(server, database, connection, path, true);

      // Пишем в сообщения причину об обошибках.
      if (ex != null)
      {
        ISessionInstallReportExtension sessionService = Session.GetService<ISessionInstallReportExtension>();
        sessionService.AddInfo(string.Format("Не удалось обновить базу {0}.{1}", server, database), InstallReportInfoType.Header2);
        sessionService.AddInfo(string.Format("Причина: {0}", ex.Message), InstallReportInfoType.Description);
      }
    }

    /// <summary>
    /// Получение пути к резервной директории.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="database">Имя базы данных.</param>
    /// <param name="path">Путь к резервной директории.</param>
    /// <returns>Если истино, то продолжить работу в вызываемом методе, иначе нет данных - выход.</returns>
    private bool GetInstallPath(string server, string database, out string path)
    {
      // Получаем все сервера, имена баз данных и пути к резервным копиям.
      ActionRunSqlScriptExistingDbWorkerSavedData[] data;
      data = Session.GetService<ISessionSerializeCustomActionDataExtension>().
        DeserializeCustomActionData<ActionRunSqlScriptExistingDbWorkerSavedData[]>();
      if (data == null)
      {
        path = null;
        return false;
      }

      path = (from d in data
              where d.Server == server && d.Database == database
              select d.Path).FirstOrDefault();
      // Если неизвестен резервный путь, не работаем.
      if (path == null)
        throw new FileNotFoundException("Не определена директория для сохранения резервных копий.");
      return true;
    }

    /// <summary>
    /// Копирование файлов между резервными копиями и инсталляцией.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="database">Имя базы данных.</param>
    /// <param name="connection">Открытое соединение.</param>
    /// <param name="path">Путь к резервной директории.</param>
    /// <param name="reverse">Направление копирование. Если ложно, то копируется из
    /// инсталляции в резервную директорию Если истино, то из резервной директории
    /// в инсталляцию.</param>
    private void CopyingFilesBetweenBackupAndInstallation(string server, string database, 
      ISqlWorkerConnection connection, string path, bool reverse)
    {
      SqlWorkerBase worker;

      // Узнаем физические имена файлов данных и лога.
      ISqlGetPhysicalFilePathWorkerReturnedData fileInfo;
      worker = Factory.CreateSqlGetPhysicalFilePathWorker(connection, out fileInfo);
      worker.Execute();

      // Отсоединяем БД.
      worker = Factory.CreateSqlDetachDatabaseWorker(connection);
      worker.Execute();

      string mdfFile = Path.GetFileName(fileInfo.MdfFilePath);
      string ldfFile = Path.GetFileName(fileInfo.LdfFilePath);
      mdfFile = Path.Combine(path, mdfFile);
      ldfFile = Path.Combine(path, ldfFile);
      if (!reverse)
      {
        // Копируем ее файлы в резервную директорию.
        File.Copy(fileInfo.MdfFilePath, mdfFile);
        File.Copy(fileInfo.LdfFilePath, ldfFile);
      }
      else
      {
        // С резервной директории копируем копию.
        File.Copy(mdfFile, fileInfo.MdfFilePath, true);
        File.Copy(ldfFile, fileInfo.LdfFilePath, true);
      }

      // Присоединяем обратно.
      ISqlAttachDatabaseWorkerReturnedData attached;
      worker = Factory.CreateSqlAttachDatabaseWorker(connection, database, fileInfo.MdfFilePath, fileInfo.LdfFilePath, out attached);
      worker.Execute();
      // Проверяем только при операции копирования в резервную директорию.
      if (!reverse && !attached.DatabaseCreated)
        throw new Exception(string.Format("Не удалось присоеденить базу данных {0} к серверу {1}.", database, server));
    }
  }
}

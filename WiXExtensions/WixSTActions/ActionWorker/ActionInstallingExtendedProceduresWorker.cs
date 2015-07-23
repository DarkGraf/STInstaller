using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  public class ActionInstallingExtendedProceduresWorkerSavedData
  {
    /// <summary>
    /// Путь к серверному файлу.
    /// </summary>
    public string PathToServerFile { get; set; }
    /// <summary>
    /// Путь к файлу во временной директории.
    /// </summary>
    public string PathToTempFile { get; set; }
    /// <summary>
    /// Временный файл, используемый для сохранения копии оригинального файла для восстановления.
    /// </summary>
    public string PathToBackupFile { get; set; }
  }

  class ActionInstallingExtendedProceduresWorker : ActionExtensionWorker
  {
    public ActionInstallingExtendedProceduresWorker(Session session, string[] subscribers = null)
      : base(session, subscribers) { }

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override CurrentInstallStatus WorkingStatus
    {
      get { return CurrentInstallStatus.Install | CurrentInstallStatus.Change; }
    }

    protected override ActionExtensionWorker.ActionDescription GetActionDescription(InstallPhase phase)
    {
      string mes = "";

      switch (phase)
      {
        case InstallPhase.Immediate:
          mes = "Подготовка к установке расширений SQL-сервера.";
          break;
        case InstallPhase.Deferred:
          mes = "Установка расширений SQL-сервера.";
          break;
        case InstallPhase.Rollback:
          mes = "Отмена установки расширений SQL-сервера.";
          break;
      }
      return new ActionDescription { Name = mes, Description = mes };
    }

    protected override ActionResult DoWorkImmediate()
    {
      IList<ActionInstallingExtendedProceduresWorkerSavedData> data = new List<ActionInstallingExtendedProceduresWorkerSavedData>();

      try
      {
        // Получаем список серверов и их путей которые выбраны в окне баз данных.
        ServerInfo[] servers = (from s in Session.GetService<ISessionServerInfoExtension>().GetServerInfos()
                                join d in Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos()
                                on s.Name equals d.Server
                                select s).Distinct().ToArray();
        if (servers.Length == 0)
          return ActionResult.Success;

        // Получаем список устанавливаемых файлов.
        // Компонент должен требовать установки или быть уже установленным (внимание, Reinstall выполняться не будет).
        var files = (from file in Session.GetService<ISessionDataTableExtension>().
                       CopyTableInfoToDataTable("SqlExtendedProcedures").AsEnumerable()
                     join binary in Session.GetService<ISessionDataTableExtension>().
                       CopyTableInfoToDataTable("Binary").AsEnumerable()
                     on file.Field<string>("BinaryKey") equals binary.Field<string>("Name")
                     where (Session.GetService<ISessionComponentInstallStatusExtension>().
                       GetStatus(file.Field<string>("Component")) & (ComponentInstallStatus.Install | ComponentInstallStatus.AlreadyInstalled)) != ComponentInstallStatus.Unknow
                     select new
                     {
                       Name = file.Field<string>("Name"),
                       Data = binary.Field<byte[]>("Data")
                     }).ToArray();

        // Если файлов нет, не продолжаем.
        if (files.Length == 0)
          return ActionResult.Success;

        // Копируем файлы во временную директорию.
        string tempDirectory = Session.GetService<ISessionTempDirectoryExtension>().GetTempDirectory();
        foreach (var file in files)
        {
          // Сохраняем файлы во временную папку.
          string path = Path.Combine(tempDirectory, file.Name);
          using (FileStream stream = new FileStream(path, FileMode.Create))
          {
            stream.Write(file.Data, 0, file.Data.Length);
          }
        }

        // Для каждого сервера проверяем существование устанавливаемых файлов,
        // если у файла существует версия то проверяем по версии (должна быть больше), 
        // иначе проверяем бинарно.        
        foreach (ServerInfo server in servers)
        {
          string serverDirectory = Path.GetDirectoryName(server.Path);
          foreach (var file in files)
          {
            bool isActual = false;
            ActionInstallingExtendedProceduresWorkerSavedData d = new ActionInstallingExtendedProceduresWorkerSavedData 
            {
              PathToServerFile = Path.Combine(serverDirectory, file.Name),
              PathToTempFile = Path.Combine(tempDirectory, file.Name),
              PathToBackupFile = Path.Combine(tempDirectory, Path.GetRandomFileName())
            };

            // Если файла у сервера нет, добавляем.
            isActual = !File.Exists(d.PathToServerFile);

            // Проверка версии.
            if (!isActual)
            {
              string serverVersion = FileVersionInfo.GetVersionInfo(d.PathToServerFile).FileVersion;
              string tempVersion = FileVersionInfo.GetVersionInfo(d.PathToTempFile).FileVersion;
              isActual = serverVersion != null && tempVersion != null &&
                new Version(serverVersion).CompareTo(new Version(tempVersion)) == -1;
            }

            // Проверка бинарно.
            if (!isActual)
              isActual = !ComparingFiles.FileCompare(d.PathToServerFile, d.PathToTempFile);

            // Запоминаем только отличающиеся файлы для конкретных серверов и передаем их подписчикам.
            if (isActual)
              data.Add(d);
          }
        }

        // Вычисляем нужные файлы.
        IList<string> saved = data.Select(v => Path.GetFileName(v.PathToTempFile)).Distinct().ToList();
        // Удаляем ненужные файлы.
        foreach (string file in files.Select(v => v.Name).Except(saved))
          File.Delete(Path.Combine(tempDirectory, file));

        return ActionResult.Success;
      }
      finally
      {
        Session.GetService<ISessionSerializeCustomActionDataExtension>().
          SerializeCustomActionData<ActionInstallingExtendedProceduresWorkerSavedData[]>(data.ToArray());
      }
    }

    protected override ActionResult DoWorkDeferred()
    {
      // Получаем файлы для серверов.
      ActionInstallingExtendedProceduresWorkerSavedData[] data = Session.GetService<ISessionSerializeCustomActionDataExtension>().
        DeserializeCustomActionData<ActionInstallingExtendedProceduresWorkerSavedData[]>();

      foreach (ActionInstallingExtendedProceduresWorkerSavedData d in data)
      {
        // Копируем старые заменяемые файлы во временную директорию, если они есть.
        if (File.Exists(d.PathToServerFile))
          File.Copy(d.PathToServerFile, d.PathToBackupFile);
        // Копируем новые файлы.
        File.Copy(d.PathToTempFile, d.PathToServerFile, true);
      }

      return ActionResult.Success;
    }

    protected override ActionResult DoWorkRollback()
    {
      // Получаем файлы для серверов.
      ActionInstallingExtendedProceduresWorkerSavedData[] data = Session.GetService<ISessionSerializeCustomActionDataExtension>().
        DeserializeCustomActionData<ActionInstallingExtendedProceduresWorkerSavedData[]>();

      foreach (ActionInstallingExtendedProceduresWorkerSavedData d in data)
      {
        // Удаляем установившиеся файлы.
        if (File.Exists(d.PathToServerFile))
          File.Delete(d.PathToServerFile);

        // Если есть резервная копия во временной директориии, восстанавливаем.
        if (File.Exists(d.PathToBackupFile))
          File.Copy(d.PathToBackupFile, d.PathToServerFile);
      }

      return ActionResult.Success;
    }
  }
}

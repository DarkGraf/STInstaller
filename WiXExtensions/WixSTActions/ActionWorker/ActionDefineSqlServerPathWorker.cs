using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.SqlWorker;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  /// <summary>
  /// Автоматически определяет путь к базе данных и устанавливает его в заданное свойство.
  /// </summary>
  class ActionDefineSqlServerPathWorker : ActionWorkerBase
  {
    /// <summary>
    /// Минимальная поддерживаемая версия SQL-сервера.
    /// </summary>
    const int SupportVersion = 10;

    ISqlWorkersFactory factory;

    public ActionDefineSqlServerPathWorker(Session session, ISqlWorkersFactory factory) : base(session) 
    {
      this.factory = factory;
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
      // Получаем локальные сервера.
      string[] servers = SqlServersFinder.Find();

      // Обрабатываем каждый сервер.      
      foreach (string server in servers.OrderBy(v => v))
      {
        SqlWorkerBase worker;

        // Определяем его доступность и наличие административных прав у пользователя;.
        // Пользователя берем по Windows-аутентификации (по ТЗ).
        try
        {
          ISqlCheckingAdminRightsWorkerReturnedData checkingData;
          worker = factory.CreateSqlCheckingAdminRightsWorker(server, AuthenticationType.Windows, "", "", out checkingData);
          worker.Execute();
          if (!checkingData.IsAdmin)
            continue;
        }
        catch (SqlWorkerConnectionFaultException)
        {
          // Перехватываем только это исключение.
          continue;
        }

        // Определяем версию сервера (поддерживаем от 2008).
        ISqlServerVersionWorkerReturnedData versionData;
        worker = factory.CreateServerVersionWorker(server, AuthenticationType.Windows, "", "", out versionData);
        worker.Execute();
        if (versionData.Version < SupportVersion)
          continue;

        // Получаем пути к серверам и запоминаем их в сессии.
        // Windows-аутентификаци по ТЗ.
        ISqlGetMsSqlServerProcessWorkerReturnedData data;
        worker = factory.CreateSqlGetMsSqlServerProcessWorker(server, AuthenticationType.Windows, "", "", out data);
        worker.Execute();

        // Если процесс определить не получилось, прерываем установку.
        if (data.Process == null)
          throw new FileNotFoundException(string.Format("Не определен путь к серверу {0}", server)); 

        ServerInfo info = new ServerInfo();
        info.Name = server;
        info.Path = data.Process.MainModule.FileName;

        Session.GetService<ISessionServerInfoExtension>().AddServerInfo(info);
      }

      return ActionResult.Success;
    }
  }
}

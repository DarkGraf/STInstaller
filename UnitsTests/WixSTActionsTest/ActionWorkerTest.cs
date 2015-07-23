using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using WixSTActions.ActionWorker;
using WixSTActions.Utils;
using WixSTActions.SqlWorker;

namespace WixSTActionsTest
{
  #region Общие заглушки

  /// <summary>
  /// Заклушка с не реализованными методами.
  /// </summary>
  class StubNotImplementedFactory : ISqlWorkersFactory
  {
    public virtual SqlWorkerBase CreateSqlCheckingAdminRightsWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlCheckingAdminRightsWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateServerVersionWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlServerVersionWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlGetDatabaseFromProcedureWorker(string server, AuthenticationType authenticationType, string user, string password, string version, out ISqlGetDatabaseFromProcedureWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlGetMsSqlServerProcessWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlGetMsSqlServerProcessWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlCheckConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlCheckConnectionWorker(string server, AuthenticationType authenticationType, string user, string password)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlDatabaseExistWorker(string server, string checkedDatabase, AuthenticationType authenticationType, string user, string password, out ISqlDatabaseExistWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlAttachDatabaseWorker(string server, string newDatabase, AuthenticationType authenticationType, string user, string password, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlAttachDatabaseWorker(ISqlWorkerConnection connection, string newDatabase, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlDetachDatabaseWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlDetachDatabaseWorker(ISqlWorkerConnection connection)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(string server, string database, AuthenticationType authenticationType, string user, string password, out ISqlGetPhysicalFilePathWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(ISqlWorkerConnection connection, out ISqlGetPhysicalFilePathWorkerReturnedData data)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlSetSingleUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlSetSingleUserWorker(ISqlWorkerConnection connection)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlSetMultiUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlSetMultiUserWorker(ISqlWorkerConnection connection)
    {
      throw new NotImplementedException();
    }

    public virtual SqlWorkerBase CreateSqlCreateConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password, out ISqlWorkerConnection workerConnection)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlRunScriptWorker(string server, string database, AuthenticationType authenticationType, string user, string password, ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlRunScriptWorker(ISqlWorkerConnection connection, ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
    {
      throw new NotImplementedException();
    }

    public SqlWorkerBase CreateSqlUseDatabaseWorker(ISqlWorkerConnection connection, string newDatabase)
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>
  /// Базовый класс заглушки для ActionWorker.
  /// </summary>
  class StubActionWorkerSessionService : ISessionService
  {
    public StubSessionUITypeGetterExtension StubSessionUITypeGetterExtension = new StubSessionUITypeGetterExtension();

    /// <summary>
    /// Переопределить и в конце вызвать базовый метод: return base.GetService<T>().
    /// </summary>
    public virtual T GetService<T>()
    {
      if (typeof(T) == typeof(ISessionUITypeGetterExtension))
        return (T)(object)StubSessionUITypeGetterExtension;
      throw new NotImplementedException();
    }
  }

  // Данное расширение (служба) требуется для всех тестов для работы с UIType, 
  // так как оно используется в базовом классе ActionWorkerBase.
  class StubSessionUITypeGetterExtension : ISessionUITypeGetterExtension
  {
    public UIType UIType;

    public UIType GetUIType()
    {
      return UIType;
    }

    public void SetUIType(UIType type)
    {
      UIType = type;
    }
  }

  #endregion

  [TestClass]
  public class ActionUITypeWorkerTest
  {
    class StubSessionService : StubActionWorkerSessionService
    {
      StubSessionDataTableExtension StubSessionDataTableExtension = new StubSessionDataTableExtension();

      public override T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionDataTableExtension))
          return (T)(object)StubSessionDataTableExtension;
        return base.GetService<T>();
      }
    }

    class StubSessionDataTableExtension : ISessionDataTableExtension
    {
      public DataTable CopyTableInfoToDataTable(string name)
      {
        DataTable tbl = new DataTable();
        tbl.Columns.Add("Id", typeof(string));
        tbl.Columns.Add("Type", typeof(string));
        tbl.Rows.Add(new object[] { "MainUI", "Client" });
        return tbl;
      }
    }    

    class SessionProperties : IActionUITypeSessionProperties
    {
      public string Mode { get; set; }
    }

    /// <summary>
    /// Проверка инициализации состояния клиента (достаточно без проверки сервера).
    /// </summary>
    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionUITypeWorkerSuccess()
    {
      using (ShimsContext.Create())
      {
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        // Устанавливаем заглушку сервиса сессий.
        StubSessionService service = new StubSessionService();
        session.SetDefaultSessionService(service);
        SessionProperties sessionProperties = new SessionProperties();

        // Проверяем состояние клиента.
        ActionUITypeWorker worker = new ActionUITypeWorker(session, sessionProperties);
        Assert.AreEqual(ActionResult.Success, worker.Execute());
        // Сохранения для использования в Wix.
        Assert.AreEqual("Client", sessionProperties.Mode);
        // Сохранение для использования в CustomAction.
        Assert.AreEqual(UIType.Client, service.StubSessionUITypeGetterExtension.UIType);
      }
    }
  }

  [TestClass]
  public class ActionSelectDatabasesWorkerTest
  {
    #region Заглушки только для Initialize    

    class StubSqlGetDatabaseFromProcedureWorkerReturnedData : ISqlGetDatabaseFromProcedureWorkerReturnedData
    {
      public bool ProcedureExist { get; set; }
      public IDictionary<string, Tuple<string, bool>> Databases { get; set; }
    }

    class StubFactory : StubNotImplementedFactory
    {
      public override SqlWorkerBase CreateSqlGetDatabaseFromProcedureWorker(string server, AuthenticationType authenticationType, string user, string password, string version, out ISqlGetDatabaseFromProcedureWorkerReturnedData data)
      {
        data = new StubSqlGetDatabaseFromProcedureWorkerReturnedData();
        data.ProcedureExist = true;
        data.Databases = new Dictionary<string, Tuple<string, bool>>();
        if (server == "TESTCOMP\\SQL2012")
        {
          data.Databases.Add("aaa", new Tuple<string, bool>("1.0.0", true));
          data.Databases.Add("bbb", new Tuple<string, bool>("1.1.0", true));
        }
        if (server == "TESTCOMP\\SQL2014")
        {
          data.Databases.Add("aaa", new Tuple<string, bool>("1.1.0", true));
          data.Databases.Add("ccc", new Tuple<string, bool>("2.0.0", false));
        }
        return new StubSqlWorkerBase();
      }
    }

    class StubSqlWorkerBase : SqlWorkerBase
    {
      public StubSqlWorkerBase()
        : base("server", "database", AuthenticationType.Windows, "", "")
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
      }
      protected override void DoWork(SqlConnection connection) { }
    }

    #endregion

    #region Общие заглушки

    class StubSelectDatabasesSessionProperties : ISelectDatabasesSessionProperties
    {
      public string Version { get; set; }
      public string SelectedDatabase { get; set; }
      public string SelectedServer { get; set; }
      public string ExistDatabase { get; set; }
      public string ExistControlProperty { get; set; }
      public string NewDatabase { get; set; }
    }

    class StubSessionService : StubActionWorkerSessionService
    {
      public StubSessionDatabaseInfoExtension StubSessionDatabaseInfoExtension = new StubSessionDatabaseInfoExtension();
      public StubSessioServerInfoExtension StubSessioServerInfoExtension = new StubSessioServerInfoExtension();

      public override T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionDatabaseInfoExtension))
          return (T)(object)StubSessionDatabaseInfoExtension;
        if (typeof(T) == typeof(ISessionServerInfoExtension))
          return (T)(object)StubSessioServerInfoExtension;
        return base.GetService<T>();
      }
    }

    class StubSessionDatabaseInfoExtension : ISessionDatabaseInfoExtension
    {
      List<DatabaseInfo> Databases = new List<DatabaseInfo>();

      public void AddDatabaseInfo(DatabaseInfo info)
      {
        Databases.Add(info);
      }

      public DatabaseInfo[] GetDatabaseInfos()
      {
        return Databases.ToArray();
      }

      public void DeleteDatabaseInfo(string server, string name)
      {
        Databases = Databases.Where(v => v.Server != server || v.Name != name).ToList();
      }
    }

    class StubSessioServerInfoExtension : ISessionServerInfoExtension
    {
      List<ServerInfo> Servers = new List<ServerInfo>();

      public void AddServerInfo(ServerInfo info)
      {
        Servers.Add(info);
      }

      public ServerInfo[] GetServerInfos()
      {
        return Servers.ToArray();
      }
    }

    #endregion

    IDisposable shimsContext;

    [TestInitialize]
    public void TestInitialize()
    {
      shimsContext = ShimsContext.Create();
    }

    [TestCleanup]
    public void TestCleanup()
    {
      shimsContext.Dispose();
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionSelectDatabasesWorkerInitialize()
    {
      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubSelectDatabasesSessionProperties sessionProp = new StubSelectDatabasesSessionProperties();
      StubFactory factory = new StubFactory();
      // Устанавливаем заглушку сервиса сессий.
      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      // Добавляем сервера.
      service.StubSessioServerInfoExtension.AddServerInfo(new ServerInfo { Name = "TESTCOMP\\SQL2012" });
      service.StubSessioServerInfoExtension.AddServerInfo(new ServerInfo { Name = "TESTCOMP\\SQL2014" });
      
      ActionSelectDatabasesWorker worker = new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.Initialization, sessionProp, factory);
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      // Получаем из сервиса сессий массив информаций о баз данных.
      DatabaseInfo[] infos = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(4, infos.Length);
      for (int i = 0; i < infos.Length; i++)
      {
        Assert.AreEqual(new string[] { "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2014", "TESTCOMP\\SQL2014" }[i], infos[i].Server);
        Assert.AreEqual(new string[] { "aaa", "bbb", "aaa", "ccc" }[i], infos[i].Name);
        Assert.AreEqual(new string[] { "1.0.0", "1.1.0", "1.1.0", "2.0.0" }[i], infos[i].Version);
        Assert.AreEqual(new bool[] { true, true, true, false }[i], infos[i].IsRequiringUpdate);
        Assert.IsFalse(infos[i].IsNew);
      }
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionSelectDatabasesWorkerAddingNew()
    {
      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubSelectDatabasesSessionProperties sessionProp = new StubSelectDatabasesSessionProperties();
      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      ActionSelectDatabasesWorker worker = new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.AddingNew, sessionProp, null);

      DatabaseInfo[] databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(0, databases.Length);

      // Входные данные для данного действия.
      sessionProp.SelectedServer = "TESTCOMP\\SQL2012";
      sessionProp.NewDatabase = "ASPO";
      sessionProp.Version = "1.0.0.0";

      Assert.AreEqual(ActionResult.Success, worker.Execute());
      databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(1, databases.Length);
      Assert.AreEqual("TESTCOMP\\SQL2012", databases[0].Server);
      Assert.AreEqual("ASPO", databases[0].Name);
      Assert.AreEqual("1.0.0.0", databases[0].Version);
      Assert.IsTrue(databases[0].IsNew);
      Assert.IsTrue(databases[0].IsRequiringUpdate);

      // Свойство, содержащее имя новой базы, должно быть пустое.
      Assert.AreEqual("", sessionProp.NewDatabase);
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionSelectDatabasesWorkerAddingExisting()
    {
      // Для UI-элементов.
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemGetString = (@this, property) =>
      {
        // Для UI-элемента хранения существующих баз данных, моделируем хранение информации
        // в объекте CustomActionData, внутри которого сериализован DatabaseInfo.
         CustomActionData data = new CustomActionData();
         data.AddObject<DatabaseInfo>(typeof(DatabaseInfo).ToString(),
           new DatabaseInfo { Server = "TESTCOMP\\SQL2012", Name = "ASPO", Version = "1.0.0.0", IsNew = false, IsRequiringUpdate = true });
         return data.ToString();
      };
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemSetStringString = (@this, property, value) => { };
      // Методы замещения для работы с элеметом UI.
      string argument = null;
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; };
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.OpenViewStringObjectArray = (@this, sqlFormat, args) =>
      {
        // Вызывается при удалении, в argument помещаем сериализованный CustomActionData с DatabaseInfo.
        if (args.Length == 2)
          argument = args[1].ToString();
        return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimView().Instance;
      };

      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubSelectDatabasesSessionProperties sessionProp = new StubSelectDatabasesSessionProperties();
      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      ActionSelectDatabasesWorker worker = new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.AddingExisting, sessionProp, null);

      DatabaseInfo[] databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(0, databases.Length);

      // Входные данные.
      sessionProp.SelectedServer = "TESTCOMP\\SQL2012";
      sessionProp.Version = "1.0.0.0";
      sessionProp.ExistDatabase = "ASPO";
      sessionProp.ExistControlProperty = "UIControl";

      Assert.AreEqual(ActionResult.Success, worker.Execute());
      databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(1, databases.Length);
      Assert.AreEqual("TESTCOMP\\SQL2012", databases[0].Server);
      Assert.AreEqual("ASPO", databases[0].Name);
      Assert.AreEqual("1.0.0.0", databases[0].Version);
      Assert.IsFalse(databases[0].IsNew);
      Assert.IsTrue(databases[0].IsRequiringUpdate);

      // В argument должен содержаться сериализованный удаленный элемент из списка существующих элементов.
      Assert.IsNotNull(argument);
      DatabaseInfo info = new CustomActionData(argument).GetObject<DatabaseInfo>(typeof(DatabaseInfo).ToString());
      Assert.AreEqual("TESTCOMP\\SQL2012", info.Server);
      Assert.AreEqual("ASPO", info.Name);
      Assert.AreEqual("1.0.0.0", info.Version);
      Assert.IsFalse(info.IsNew);
      Assert.IsTrue(info.IsRequiringUpdate);
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionSelectDatabasesWorkerDelete()
    {
      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubSelectDatabasesSessionProperties sessionProp = new StubSelectDatabasesSessionProperties();
      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      // Добавляем базы.
      service.StubSessionDatabaseInfoExtension.AddDatabaseInfo(new DatabaseInfo { Server = "TESTCOMP\\SQL2012", Name = "ASPO", IsNew = true, Version = "1.0.0.0", IsRequiringUpdate = true });
      service.StubSessionDatabaseInfoExtension.AddDatabaseInfo(new DatabaseInfo { Server = "TESTCOMP\\SQL2014", Name = "ASPO", IsNew = false, Version = "1.0.0.1", IsRequiringUpdate = true });

      // Методы замещения для работы с элеметом UI.
      Record record = null;
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; };
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.OpenViewStringObjectArray = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimView().Instance; };
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.CreateRecordInt32 = (@this, fieldCount) => { return record = new Record(fieldCount); };

      ActionSelectDatabasesWorker worker = new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.Deleting, sessionProp, null);

      DatabaseInfo[] databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(2, databases.Length);

      // Удаляем базу данных добавленную пользователем (не должна запомнаться в списке существующих баз данных).
      sessionProp.SelectedDatabase = "TESTCOMP\\SQL2012.ASPO";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      // Проверяем оставшуюся (достаточно два поля).
      databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(1, databases.Length);
      Assert.AreEqual("TESTCOMP\\SQL2014", databases[0].Server);
      Assert.AreEqual("ASPO", databases[0].Name);
      // В списке не запомнилась.
      Assert.IsNull(record);

      // Удаляем базу данных существующую на сервере (должна запомниться в списке существующих баз данных).
      sessionProp.SelectedDatabase = "TESTCOMP\\SQL2014.ASPO";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      // Проверяем (достаточно два поля).
      databases = service.StubSessionDatabaseInfoExtension.GetDatabaseInfos();
      Assert.AreEqual(0, databases.Length);
      // В списке запомнилась.
      Assert.IsNotNull(record);
      DatabaseInfo info = new CustomActionData(record.GetString(3)).GetObject<DatabaseInfo>(typeof(DatabaseInfo).ToString());
      Assert.AreEqual("TESTCOMP\\SQL2014", info.Server);
      Assert.AreEqual("ASPO", info.Name);
      Assert.AreEqual("1.0.0.1", info.Version);
      Assert.IsFalse(info.IsNew);
      Assert.IsTrue(info.IsRequiringUpdate);
    }
  }

  [TestClass]
  public class ActionDefineSqlServerPathWorkerTest
  {
    #region Заглушки

    class StubSqlCheckingAdminRightsWorkerReturnedData : ISqlCheckingAdminRightsWorkerReturnedData
    {
      public bool IsAdmin { get; set; }
    }

    class StubSqlServerVersionWorkerReturnedData : ISqlServerVersionWorkerReturnedData
    {
      public int Version { get; set; }
    }

    class StubSqlGetMsSqlServerProcessWorkerReturnedData : ISqlGetMsSqlServerProcessWorkerReturnedData
    {
      public Process Process { get; set; }
    }

    class StubFactory : StubNotImplementedFactory
    {
      public override SqlWorkerBase CreateSqlCheckingAdminRightsWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlCheckingAdminRightsWorkerReturnedData data)
      {
        data = new StubSqlCheckingAdminRightsWorkerReturnedData();
        if (server == "TESTCOMP\\SQL2008")
          throw new SqlWorkerConnectionFaultException();
        data.IsAdmin = new string[] { "TESTCOMP", "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2014" }.Contains(server);
        return new StubSqlWorkerBase();
      }

      public override SqlWorkerBase CreateServerVersionWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlServerVersionWorkerReturnedData data)
      {
        data = new StubSqlServerVersionWorkerReturnedData();
        if (new string[] { "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2014" }.Contains(server))
          data.Version = 10;
        else
          data.Version = 9;
        return new StubSqlWorkerBase();
      }

      public override SqlWorkerBase CreateSqlGetMsSqlServerProcessWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlGetMsSqlServerProcessWorkerReturnedData data)
      {
        data = new StubSqlGetMsSqlServerProcessWorkerReturnedData();
        data.Process = new System.Diagnostics.Fakes.ShimProcess().Instance;
        return new StubSqlWorkerBase();
      }
    }

    class StubSqlWorkerBase : SqlWorkerBase
    {
      public StubSqlWorkerBase()
        : base("server", "database", AuthenticationType.Windows, "", "")
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
      }
      protected override void DoWork(SqlConnection connection) { }
    }

    class StubSessionService : StubActionWorkerSessionService
    {
      public StubSessioServerInfoExtension StubSessioServerInfoExtension = new StubSessioServerInfoExtension();

      public override T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionServerInfoExtension))
          return (T)(object)StubSessioServerInfoExtension;
        return base.GetService<T>();
      }
    }

    class StubSessioServerInfoExtension : ISessionServerInfoExtension
    {
      List<ServerInfo> Servers = new List<ServerInfo>();

      public void AddServerInfo(ServerInfo info)
      {
        Servers.Add(info);
      }

      public ServerInfo[] GetServerInfos()
      {
        return Servers.ToArray();
      }
    }

    #endregion

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionDefineSqlServerPathWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        ISqlWorkersFactory factory = new StubFactory();
        // Устанавливаем заглушку сервиса сессий.
        StubSessionService service = new StubSessionService();
        service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
        session.SetDefaultSessionService(service);

        // Для получения локальных серверов.
        // TESTCOMP - не поддерживаемая версия.
        // TESTCOMP\\SQL2008 - отключен. 
        // TESTCOMP\\SQL2012 - работает.
        // TESTCOMP\\SQL2014 - работает.
        string[] servers = new string[] { "TESTCOMP", "TESTCOMP\\SQL2008", "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2014" };
        Microsoft.Win32.Fakes.ShimRegistryKey.AllInstances.OpenSubKeyString = delegate { return new Microsoft.Win32.Fakes.ShimRegistryKey(); };
        Microsoft.Win32.Fakes.ShimRegistryKey.AllInstances.GetValueString = delegate { return new string[] { "MSSQLSERVER", "SQL2008", "SQL2012", "SQL2014" }; };
        System.Fakes.ShimEnvironment.MachineNameGet = delegate { return "TESTCOMP"; };

        System.Diagnostics.Fakes.ShimProcess.AllInstances.MainModuleGet = delegate { return new System.Diagnostics.Fakes.ShimProcessModule().Instance; };
        int index = 0;
        System.Diagnostics.Fakes.ShimProcessModule.AllInstances.FileNameGet = delegate
        {
          return new string[] { @"C:\Programm\SQL2012\sqlserv.exe", @"C:\Programm\SQL2014\sqlserv.exe" }[index++];
        };

        // Начало теста.
        ActionDefineSqlServerPathWorker worker = new ActionDefineSqlServerPathWorker(session, factory);
        Assert.AreEqual(ActionResult.Success, worker.Execute());

        // Получаем информацию из сессии о серверах.
        ServerInfo[] actualServers = service.StubSessioServerInfoExtension.GetServerInfos();
        Assert.AreEqual(2, actualServers.Length);
        for (int i = 0; i < 2; i++)
        {
          Assert.AreEqual(new string[] { "TESTCOMP\\SQL2012", "TESTCOMP\\SQL2014" }[i], actualServers[i].Name);
          Assert.AreEqual(new string[] { @"C:\Programm\SQL2012\sqlserv.exe", @"C:\Programm\SQL2014\sqlserv.exe" }[i], actualServers[i].Path);
        }
      }
    }
  }

  [TestClass]
  public class ActionCheckConnectionWorkerTest
  {
    #region Заглушки

    class StubCheckConnectionSessionProperties : ICheckConnectionSessionProperties
    {
      public string Server { get; set; }
      public string Database { get; set; }
      public string ConnectionSuccessful { get; set; }
      public string StringMessage { get; set; }
    }

    class StubFactory : StubNotImplementedFactory
    {
      public override SqlWorkerBase CreateSqlCheckConnectionWorker(string server, AuthenticationType authenticationType, string user, string password)
      {
        return new StubSqlWorkerBase(server != "VIRTUALHOST");
      }

      public override SqlWorkerBase CreateSqlDatabaseExistWorker(string server, string checkedDatabase, AuthenticationType authenticationType, string user, string password, out ISqlDatabaseExistWorkerReturnedData data)
      {
        data = new StubSqlDatabaseExistWorkerReturnedData();
        data.DatabaseExists = server == "VIRTUALHOST" && checkedDatabase == "ASPO";
        return new StubSqlWorkerBase(false);
      }
    }

    class StubSqlDatabaseExistWorkerReturnedData : ISqlDatabaseExistWorkerReturnedData
    {
      public bool DatabaseExists { get; set; }
    }

    class StubSqlWorkerBase : SqlWorkerBase
    {
      bool generateException;

      public StubSqlWorkerBase(bool generateException) : base("server", "database", AuthenticationType.Windows, "", "")
      {
        this.generateException = generateException;        
      }
      protected override void DoWork(SqlConnection connection)
      {
        if (generateException)
          throw new Exception();
      }
    }

    class StubSessionService : StubActionWorkerSessionService
    {
    }

    #endregion

    #region  Вспомогательные члены

    /// <summary>
    /// Контекст теста.
    /// </summary>
    IDisposable shimsContext;

    [TestInitialize]
    public void TestInitialize()
    {
      shimsContext = ShimsContext.Create();
    }

    [TestCleanup]
    public void TestCleanup()
    {
      shimsContext.Dispose();
    }

    #endregion

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionCheckConnectionWorkerOnlyServer()
    {
      // Специально отдельно, потом меняется.
      System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubFactory factory = new StubFactory();
      StubCheckConnectionSessionProperties properties = new StubCheckConnectionSessionProperties { Server = "VIRTUALHOST", Database = "" };      

      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      ActionCheckConnectionWorker worker = new ActionCheckConnectionWorker(session, CheckConnectionType.OnlyServer, properties, factory);

      // Передаем правильные данные.
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Success, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.ConnectionSuccessful, properties.StringMessage);

      // Открытие соединения. Не выполнется.
      System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Closed; };
      System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { };

      // Передаем неправильное имя сервера.
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Failure, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.ConnectionNoSuccessful, properties.StringMessage);

      // Передадим сервер с некорректным именем.
      properties.Server = "-+*/";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Failure, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.ServerNameIsNotCorrect, properties.StringMessage);
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionCheckConnectionWorkerDatabaseMustNotExist()
    {
      System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubFactory factory = new StubFactory();
      StubCheckConnectionSessionProperties properties = new StubCheckConnectionSessionProperties { Server = "VIRTUALHOST", Database = "NEWASPO" };

      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      ActionCheckConnectionWorker worker = new ActionCheckConnectionWorker(session, CheckConnectionType.DatabaseMustNotExist, properties, factory);

      // Передаем правильные данные.
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Success, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.ConnectionSuccessful, properties.StringMessage);

      // База данных существует.
      properties.Database = "ASPO";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Failure, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.DataBaseExist, properties.StringMessage);

      // Передадим пустую базу данных.
      properties.Database = "";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Failure, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.DataBaseNameIsNotCorrect, properties.StringMessage);
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionCheckConnectionWorkerDatabaseMustExist()
    {
      System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
      StubFactory factory = new StubFactory();
      StubCheckConnectionSessionProperties properties = new StubCheckConnectionSessionProperties { Server = "VIRTUALHOST", Database = "NEWASPO" };

      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      session.SetDefaultSessionService(service);

      ActionCheckConnectionWorker worker = new ActionCheckConnectionWorker(session, CheckConnectionType.DatabaseMustExist, properties, factory);

      // База данных не существует.
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Failure, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.DataBaseNotExist, properties.StringMessage);

      // База данных существует.
      properties.Database = "ASPO";
      Assert.AreEqual(ActionResult.Success, worker.Execute());
      Assert.AreEqual(ActionCheckConnectionWorker.ConnectionResult.Success, properties.ConnectionSuccessful);
      Assert.AreEqual(ActionCheckConnectionWorker.StringsMessages.ConnectionSuccessful, properties.StringMessage);
    }
  }

  [TestClass]
  public class ActionInstallingExtendedProceduresWorkerTest
  {
    #region Заглушки

    class StubSessionInstallPhaseExtension : ISessionInstallPhaseExtension
    {
      public InstallPhase InstallPhase;

      public InstallPhase GetInstallPhase()
      {
        return InstallPhase;
      }
    }

    class StubSessionInstallStatusExtension : ISessionCurrentInstallStatusExtension, ISessionComponentInstallStatusExtension
    {
      public CurrentInstallStatus CurrentInstallStatus;
      public ComponentInstallStatus ComponentInstallStatus;

      public CurrentInstallStatus GetStatus()
      {
        return CurrentInstallStatus;
      }

      public ComponentInstallStatus GetStatus(string componentName)
      {
        return ComponentInstallStatus;
      }
    }

    class StubSessionSerializeCustomActionDataExtension : ISessionSerializeCustomActionDataExtension
    {
      public Dictionary<string, object> Store = new Dictionary<string, object>();

      public void SerializeCustomActionData<T>(T data, string key, string[] subscribers)
      {
        Store[key] = data;
      }

      public void SerializeCustomActionData<T>(T data, string key) { }

      public void SerializeCustomActionData<T>(T data, string[] subscribers)
      {
        SerializeCustomActionData<T>(data, typeof(T).Name, subscribers);
      }

      public void SerializeCustomActionData<T>(T data) 
      {
        SerializeCustomActionData<T>(data, typeof(T).Name, null);
      }

      public T DeserializeCustomActionData<T>(string key)
      {
        return (T)Store[key];
      }

      public T DeserializeCustomActionData<T>()
      {
        return DeserializeCustomActionData<T>(typeof(T).Name);
      }
    }

    class StubSessionServerInfoExtension : ISessionServerInfoExtension
    {
      public ServerInfo[] ServerInfos;

      public void AddServerInfo(ServerInfo info) { }

      public ServerInfo[] GetServerInfos()
      {
        return ServerInfos;
      }
    }

    class StubSessionDatabaseInfoExtension : ISessionDatabaseInfoExtension
    {
      public DatabaseInfo[] DatabaseInfos;

      public void AddDatabaseInfo(DatabaseInfo info) { }

      public DatabaseInfo[] GetDatabaseInfos()
      {
        return DatabaseInfos;
      }

      public void DeleteDatabaseInfo(string server, string name) { }
    }

    class StubSessionDataTableExtension : ISessionDataTableExtension
    {
      public Dictionary<string, DataTable> Tables = new Dictionary<string, DataTable>();

      public DataTable CopyTableInfoToDataTable(string name)
      {
        return Tables[name];
      }
    }

    class StubSessionTempDirectoryExtension : ISessionTempDirectoryExtension
    {
      public string TempDirectory;

      public void CreateTempDirectory() { }

      public string GetTempDirectory()
      {
        return TempDirectory;
      }

      public void DeleteTempDirectory() { }
    }

    class StubSessionService : StubActionWorkerSessionService
    {
      public StubSessionInstallPhaseExtension StubSessionInstallPhaseExtension = new StubSessionInstallPhaseExtension();
      public StubSessionInstallStatusExtension StubSessionInstallStatusExtension = new StubSessionInstallStatusExtension();
      public StubSessionSerializeCustomActionDataExtension StubSessionSerializeCustomActionDataExtension = new StubSessionSerializeCustomActionDataExtension();
      public StubSessionServerInfoExtension StubSessionServerInfoExtension = new StubSessionServerInfoExtension();
      public StubSessionDatabaseInfoExtension StubSessionDatabaseInfoExtension = new StubSessionDatabaseInfoExtension();
      public StubSessionDataTableExtension StubSessionDataTableExtension = new StubSessionDataTableExtension();
      public StubSessionTempDirectoryExtension StubSessionTempDirectoryExtension = new StubSessionTempDirectoryExtension();

      public override T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionInstallPhaseExtension))
          return (T)(object)StubSessionInstallPhaseExtension;
        if (typeof(T) == typeof(ISessionCurrentInstallStatusExtension))
          return (T)(object)StubSessionInstallStatusExtension;
        if (typeof(T) == typeof(ISessionComponentInstallStatusExtension))
          return (T)(object)StubSessionInstallStatusExtension;
        if (typeof(T) == typeof(ISessionSerializeCustomActionDataExtension))
          return (T)(object)StubSessionSerializeCustomActionDataExtension;
        if (typeof(T) == typeof(ISessionServerInfoExtension))
          return (T)(object)StubSessionServerInfoExtension;
        if (typeof(T) == typeof(ISessionDatabaseInfoExtension))
          return (T)(object)StubSessionDatabaseInfoExtension;
        if (typeof(T) == typeof(ISessionDataTableExtension))
          return (T)(object)StubSessionDataTableExtension;
        if (typeof(T) == typeof(ISessionTempDirectoryExtension))
          return (T)(object)StubSessionTempDirectoryExtension;
        return base.GetService<T>();
      }
    }

    #endregion

    #region  Вспомогательные члены

    /// <summary>
    /// Контекст теста.
    /// </summary>
    IDisposable shimsContext;

    [TestInitialize]
    public void TestInitialize()
    {
      shimsContext = ShimsContext.Create();
    }

    [TestCleanup]
    public void TestCleanup()
    {
      shimsContext.Dispose();
    }

    #endregion

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionInstallingExtendedProceduresWorkerImmediate()
    {
      Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;

      #region Заглушки сессии.

      StubSessionService service = new StubSessionService();
      service.StubSessionUITypeGetterExtension.UIType = UIType.Server;
      service.StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.Immediate;
      service.StubSessionInstallStatusExtension.CurrentInstallStatus = CurrentInstallStatus.Install;
      service.StubSessionInstallStatusExtension.ComponentInstallStatus = ComponentInstallStatus.Install;
      service.StubSessionServerInfoExtension.ServerInfos = new ServerInfo[]
      {
        new ServerInfo { Name = "SQL2008", Path = "C:\\SQL2008\\sqlservr.exe" },
        new ServerInfo { Name = "SQL2012", Path = "C:\\SQL2012\\sqlservr.exe" },
        new ServerInfo { Name = "SQL2014", Path = "C:\\SQL2014\\sqlservr.exe" }
      };
      service.StubSessionDatabaseInfoExtension.DatabaseInfos = new DatabaseInfo[]
      {
        new DatabaseInfo { Server = "SQL2008", Name = "ASPO1" },
        new DatabaseInfo { Server = "SQL2008", Name = "ASPO2" },
        new DatabaseInfo { Server = "SQL2012", Name = "ASPO1" },
        new DatabaseInfo { Server = "SQL2014", Name = "ASPO3" }
      };

      DataTable sqlExtendedProceduresTable = new DataTable();
      sqlExtendedProceduresTable.Columns.Add("BinaryKey", typeof(string));
      sqlExtendedProceduresTable.Columns.Add("Component", typeof(string));
      sqlExtendedProceduresTable.Columns.Add("Name", typeof(string));
      sqlExtendedProceduresTable.Rows.Add(new object[] { "BinaryDll", "ComponentId", "ASPO_XP_MSSQL.dll" });
      sqlExtendedProceduresTable.Rows.Add(new object[] { "BinaryIni", "ComponentId", "ASPO_XP_MSSQL.ini" });
      service.StubSessionDataTableExtension.Tables.Add("SqlExtendedProcedures", sqlExtendedProceduresTable);

      DataTable binaryTable = new DataTable();
      binaryTable.Columns.Add("Name", typeof(string));
      binaryTable.Columns.Add("Data", typeof(byte[]));
      binaryTable.Rows.Add(new object[] { "BinaryDll", new byte[] { 1 } }); 
      binaryTable.Rows.Add(new object[] { "BinaryIni", new byte[] { 2 } }); 
      service.StubSessionDataTableExtension.Tables.Add("Binary", binaryTable);

      service.StubSessionTempDirectoryExtension.TempDirectory = "C:\\Temp\\abcdefgh.ijk";

      session.SetDefaultSessionService(service);

      #endregion

      #region Оболочки

      List<string> tempFilesInTempDirectory = new List<string>();

      System.IO.Fakes.ShimFileStream.ConstructorStringFileMode = (@this, path, mode) =>
      {
        if (mode == FileMode.Create)
          tempFilesInTempDirectory.Add(path);
      };
      System.IO.Fakes.ShimFileStream.AllInstances.WriteByteArrayInt32Int32 = delegate { };
      System.IO.Fakes.ShimFile.ExistsString = delegate { return false; };

      #endregion

      // Начало теста.
      ActionInstallingExtendedProceduresWorker worker = 
        new ActionInstallingExtendedProceduresWorker(session, new string[] { "Deferred", "Rollback" });

      worker.Execute();

      string tempDirectory = service.StubSessionTempDirectoryExtension.TempDirectory;
      // Проверяем создание файлов во временной директории.
      Assert.AreEqual(sqlExtendedProceduresTable.Rows.Count, tempFilesInTempDirectory.Count);
      for (int i = 0; i < tempFilesInTempDirectory.Count; i++)
      {
        string path = Path.Combine(tempDirectory, sqlExtendedProceduresTable.Rows[i]["Name"].ToString());
        Assert.IsTrue(tempFilesInTempDirectory.Contains(path));
      }

      // Проверяем переданные данные подписчикам.
      // Количество строк должно быть равно количеству файлов умноженному на количество серверов.
      ActionInstallingExtendedProceduresWorkerSavedData[] data = (ActionInstallingExtendedProceduresWorkerSavedData[])
        service.StubSessionSerializeCustomActionDataExtension.Store["ActionInstallingExtendedProceduresWorkerSavedData[]"];
      int expectedCount = data.Length;
        
      Assert.AreEqual(service.StubSessionServerInfoExtension.ServerInfos.Length
        * service.StubSessionDataTableExtension.Tables["SqlExtendedProcedures"].Rows.Count, expectedCount);
      for (int i = 0; i < service.StubSessionServerInfoExtension.ServerInfos.Length; i++)
      {
        for (int j = 0; j < service.StubSessionDataTableExtension.Tables["SqlExtendedProcedures"].Rows.Count; j++)
        {
          ActionInstallingExtendedProceduresWorkerSavedData d = new ActionInstallingExtendedProceduresWorkerSavedData
          {
            PathToServerFile = Path.Combine(Path.GetDirectoryName(service.StubSessionServerInfoExtension.ServerInfos[i].Path),
              service.StubSessionDataTableExtension.Tables["SqlExtendedProcedures"].Rows[j]["Name"].ToString()),
            PathToTempFile = Path.Combine(tempDirectory,
              service.StubSessionDataTableExtension.Tables["SqlExtendedProcedures"].Rows[j]["Name"].ToString())
          };
          Assert.IsTrue(data.Count(v => v.PathToServerFile == d.PathToServerFile &&
            v.PathToTempFile == d.PathToTempFile) == 1);
        }        
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;

namespace WixSTActionsTest
{
  [TestClass]
  public class SessionInstallPhaseExtensionTest
  {
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionInstallPhaseExtensionTesting()
    {
      using (ShimsContext.Create())
      {
        string sessionData = string.Empty;
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemGetString = delegate { return sessionData; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemSetStringString = (@this, property, value) => { sessionData = value; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        ISessionInstallPhaseExtension extension = new SessionInstallPhaseExtension(session, null);

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = (@this, mode) => { return false; };
        Assert.AreEqual(InstallPhase.UISequence, extension.GetInstallPhase());

        session[SessionInstallPhaseExtension.ServerSideKey] = "1";
        Assert.AreEqual(InstallPhase.Immediate, extension.GetInstallPhase());

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = (@this, mode) => { return mode == InstallRunMode.Scheduled; };
        Assert.AreEqual(InstallPhase.Deferred, extension.GetInstallPhase());

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = (@this, mode) => { return mode == InstallRunMode.Rollback; };
        Assert.AreEqual(InstallPhase.Rollback, extension.GetInstallPhase());
      }
    }
  }

  [TestClass]
  public class SessionCustomActionNameGetterExtensionTest
  {
    #region Методы для теста

    string customActionName;

    [TestInitialize]
    public void TestInitialize()
    {
      customActionName = "";
    }

    [CustomAction]
    private ActionResult MethodA(ISessionCustomActionNameGetterExtension extension)
    {
      customActionName = extension.GetCustomActionName();
      return ActionResult.Success;
    }

    // Без атрибута CustomAction.
    private ActionResult MethodB(ISessionCustomActionNameGetterExtension extension)
    {
      return MethodA(extension);
    }

    [CustomAction]
    private ActionResult MethodC(ISessionCustomActionNameGetterExtension extension)
    {
      return MethodB(extension);
    }

    #endregion

    /// <summary>
    /// Получение имени при отсутствующих методах с атрибутом CustomAction.
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionCustomActionNameGetterExtensionNull()
    {
      using (ShimsContext.Create())
      {
        ISessionCustomActionNameGetterExtension extension = new SessionCustomActionNameGetterExtension(null, null);
        Assert.IsNull(extension.GetCustomActionName());
      }
    }

    /// <summary>
    /// Получение имени при прямом вызове.
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionCustomActionNameGetterExtensionDirect()
    {
      using (ShimsContext.Create())
      {
        ISessionCustomActionNameGetterExtension extension = new SessionCustomActionNameGetterExtension(null, null);
        MethodA(extension);
        Assert.AreEqual("MethodA", customActionName);
      }
    }

    /// <summary>
    /// Получение имени при косвенном вызове.
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionCustomActionNameGetterExtensionIndirect()
    {
      using (ShimsContext.Create())
      {
        ISessionCustomActionNameGetterExtension extension = new SessionCustomActionNameGetterExtension(null, null);
        MethodC(extension);
        Assert.AreEqual("MethodC", customActionName);
      }
    }
  }

  [TestClass]
  public class SessionSerializeCustomActionDataExtensionTest
  {
    #region Заглушки

    class StubSessionService : ISessionService
    {
      public T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionInstallPhaseExtension))
          return (T)(object)(new StubSessionInstallPhaseExtension());
        else if (typeof(T) == typeof(ISessionCustomActionNameGetterExtension))
          return (T)(object)(new StubSessionCustomActionNameGetterExtension());
        throw new NotImplementedException();
      }
    }

    class StubSessionInstallPhaseExtension : ISessionInstallPhaseExtension
    {
      public static InstallPhase InstallPhase { get; set; }

      public InstallPhase GetInstallPhase()
      {
        return InstallPhase;
      }

      public void SetServerSide()
      {
        throw new NotImplementedException();
      }
    }

    class StubSessionCustomActionNameGetterExtension : ISessionCustomActionNameGetterExtension
    {
      public static string CustomActionName { get; set; }

      public string GetCustomActionName()
      {
        return CustomActionName;
      }
    }

    #endregion

    #region Тестовые данные для сериализации.

    public class TestData
    {
      public int Id;
      public string Name;
    }

    TestData[] expected = new TestData[] 
    { 
      new TestData { Id = 1, Name = "aaa"  },
      new TestData { Id = 2, Name = "bbb"  },
      new TestData { Id = 3, Name = "ccc"  }
    };

    #endregion

    /// <summary>
    /// Общая проверка.
    /// </summary>
    private void CheckData(TestData[] actual)
    {
      Assert.IsNotNull(actual);
      Assert.AreEqual(expected.Length, actual.Length);
      for (int i = 0; i < expected.Length; i++)
      {
        Assert.AreEqual(expected[i].Id, actual[i].Id);
        Assert.AreEqual(expected[i].Name, actual[i].Name);
      }
    }

    /// <summary>
    /// Тест на сериализацию по заданному ключу перечисленным подписчикам.    
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionSerializeCustomActionDataExtensionWithKeyAndSubscribers()
    {
      using (ShimsContext.Create())
      {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        CustomActionData data = new CustomActionData();
        data.AddObject<GlobalStore>(typeof(GlobalStore).ToString(), new GlobalStore { Enable = false });
        dictionary[SessionSerializeCustomActionDataExtension.GlobalKey] = data.ToString(); // Глобальный кэш отключен.
        StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.Immediate; // Режим Server Side.

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemSetStringString = (@this, property, value) => { dictionary[property] = value; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemGetString = (@this, property) =>
          { return dictionary.ContainsKey(property) ? dictionary[property] : string.Empty; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.CustomActionDataGet = (@this) => { return new CustomActionData(dictionary["Deferred"]); };

        // Устанавливаем метод для десериализации.
        StubSessionCustomActionNameGetterExtension.CustomActionName = "Deferred"; 

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        ISessionSerializeCustomActionDataExtension extension = new SessionSerializeCustomActionDataExtension(session, new StubSessionService());
        extension.SerializeCustomActionData<TestData[]>(expected, "TestKey", new string[] { "Deferred", "Rollback" });

        StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.Deferred;
        TestData[] actual = extension.DeserializeCustomActionData<TestData[]>("TestKey");
        // Сравним данные.
        CheckData(actual);

        // Иммитируем десериализацию для фазы Immediate.
        // Для фазы Immediate данные не передаются через CustomActionData, поэтому их необходимо получать напрямую.
        StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.Immediate;
        actual = extension.DeserializeCustomActionData<TestData[]>("TestKey");
        // Сравним данные.
        CheckData(actual);
      }
    }

    /// <summary>
    /// Сериализация при отстутствующих подписчиках.
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionSerializeCustomActionDataExtensionSubscribersNull()
    {
      using (ShimsContext.Create())
      {
        // Ни каких исключений быть не должно.
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionSerializeCustomActionDataExtension extension = new SessionSerializeCustomActionDataExtension(session, new StubSessionService());
        extension.SerializeCustomActionData<string>("abc", "key", null);
      }
    }

    /// <summary>
    /// Тест сериализации в режиме "client side" и переносе данных для работы
    /// в режиме "server side".
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionSerializeCustomActionDataExtensionClientSideServerSide()
    {
      using (ShimsContext.Create())
      {
        // Общие данные.
        Dictionary<string, string> dictionary = new Dictionary<string, string>();

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemSetStringString = (@this, property, value) =>
          { dictionary[property] = value; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemGetString = (@this, property) =>
          { return dictionary.ContainsKey(property) ? dictionary[property] : string.Empty; };

        // Метод должен иметь название аналогичное одному из названий реальных методов CustomAction.
        StubSessionCustomActionNameGetterExtension.CustomActionName = "InstallerInitializeUI"; 
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionSerializeCustomActionDataExtension extension = new SessionSerializeCustomActionDataExtension(session, new StubSessionService());

        // Тест режима Clien Side.
        StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.UISequence;
        extension.SerializeCustomActionData<TestData[]>(expected, "TestKey");
        // В режиме Client Side должно сохраняться в одном месте с определенным ключом.
        Assert.IsTrue(dictionary.ContainsKey(SessionSerializeCustomActionDataExtension.GlobalKey));
        Assert.AreEqual(1, dictionary.Count);

        // Добавим еще данные.
        extension.SerializeCustomActionData<int>(7, "int");
        Assert.IsTrue(dictionary.ContainsKey(SessionSerializeCustomActionDataExtension.GlobalKey));
        Assert.AreEqual(1, dictionary.Count);

        // Читаем данные из глобального кэша.
        TestData[] actual = extension.DeserializeCustomActionData<TestData[]>("TestKey");
        CheckData(actual);

        // Переключаем режим на Server Side.
        StubSessionInstallPhaseExtension.InstallPhase = InstallPhase.Immediate;

        // Читаем данные из сессии.
        actual = extension.DeserializeCustomActionData<TestData[]>("TestKey");
        // Количество записей должно увеличиться на количество методов.
        Assert.AreEqual(1 + extension.GetMethods().Length, dictionary.Count);
        CheckData(actual);
      }
    }
  }

  [TestClass]
  public class SessionInstallStatusExtensionTest
  {
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionCurrentInstallStatusExtensionSuccess()
    {
      using (ShimsContext.Create())
      {
        // Начало теста.
        CurrentInstallStatus status;
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession();
        ISessionCurrentInstallStatusExtension extension = new SessionCurrentInstallStatusExtension(session, null);

        // Тест на корректное определение статуса установки.
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.EvaluateConditionString = (@this, condition) =>
        { return condition.Equals(CurrentInstallStatusDefiner.InstallCondition); };
        status = extension.GetStatus();
        Assert.AreEqual(CurrentInstallStatus.Install, status);

        // Тест на корректное определение статуса удаления.
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.EvaluateConditionString = (@this, condition) =>
        { return condition.Equals(CurrentInstallStatusDefiner.UninstallCondition); };
        status = extension.GetStatus();
        Assert.AreEqual(CurrentInstallStatus.Uninstall, status);

        // Тест на не корректное определение статуса (нет подходящего статуса).
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.EvaluateConditionString = (@this, condition) =>
        { return false; };
        status = extension.GetStatus();
        Assert.AreEqual(CurrentInstallStatus.Unknow, status);
      }
    }

    /// <summary>
    /// Тест на не корректное определение статуса (несколько статусов оказались истинными).
    /// </summary>
    [TestMethod]
    [TestCategory("SessionExtension")]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SessionCurrentInstallStatusExtensionFailure()
    {
      using (ShimsContext.Create())
      {
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.EvaluateConditionString = (@this, condition) =>
        { return true; };

        // Начало теста.
        CurrentInstallStatus status;
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession();
        ISessionCurrentInstallStatusExtension extension = new SessionCurrentInstallStatusExtension(session, null);
        status = extension.GetStatus();
        Assert.AreEqual(CurrentInstallStatus.Unknow, status);
      }
    }
  }

  [TestClass]
  public class SessionDataTableExtensionTest
  {
    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionDataTableExtensionTableInfoNotFound()
    {
      using (ShimsContext.Create())
      {
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.TablesGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimTableCollection().Instance; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionDataTableExtension extension = new SessionDataTableExtension(session, null);

        Assert.IsNull(extension.CopyTableInfoToDataTable("Unknow"));
      }
    }

    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionDataTableExtensionSuccess()
    {
      using (ShimsContext.Create())
      {
        // Для проверки схемы.
        List<ColumnInfo> columns = new List<ColumnInfo>
        {
          new ColumnInfo("id", typeof(int), 0, true),
          new ColumnInfo("name", typeof(string), 72, true),
          new ColumnInfo("dataType", typeof(Stream), 0, true)
        };
        TableInfo tableInfo = new TableInfo("TableName", columns, new string[] { "id" });

        // Для проверки данных.
        var data = new[]
        {
          // DataType = Id + Index.
          new { Id = 1, Name = "aaa", DataType = new MemoryStream(new byte[] { 1, 2, 3 }) },
          new { Id = 2, Name = "bbb", DataType = new MemoryStream(new byte[] { 2, 3, 4 })  },
          new { Id = 3, Name = "ccc", DataType = new MemoryStream(new byte[] { 3, 4, 5 })  },
          new { Id = 4, Name = "[CalcField]", DataType = new MemoryStream(new byte[] { 4, 5, 6 })  },
          new { Id = 5, Name = "It is [CalcField][CalcField] Complex", DataType = new MemoryStream(new byte[] { 5, 6, 7 })  }
        };

        // Счетчик для View.Fetch() и получения Record.
        int count = -1;

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.ItemGetString = (@this, property) =>
        {
          if (property == "CalcField")
            return "CalcField";
          else
            throw new InvalidHandleException();
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.TablesGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimTableCollection().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimTableCollection.AllInstances.ItemGetString = delegate { return tableInfo; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.OpenViewStringObjectArray = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimView().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimView.AllInstances.Fetch = delegate { return ++count < data.Length ? new Record() : null; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimRecord.AllInstances.ItemGetString = (@this, fieldName) =>
        {
          if (fieldName == "id")
            return data[count].Id;
          if (fieldName == "name")
            return data[count].Name;
          if (fieldName == "dataType")
            return data[count].DataType;
          throw new Exception();
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimInstallerHandle.AllInstances.Dispose = delegate { };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionDataTableExtension extension = new SessionDataTableExtension(session, null);

        DataTable table = extension.CopyTableInfoToDataTable("TableName");

        // Проверяем схему.
        Assert.AreEqual(columns.Count, table.Columns.Count);
        for (int i = 0; i < columns.Count; i++)
        {
          Assert.AreEqual(columns[i].Name, table.Columns[i].ColumnName);
          Assert.AreEqual(columns[i].Type == typeof(Stream) ? typeof(byte[]) : columns[i].Type, table.Columns[i].DataType);
        }

        // Проверяем данные.
        Assert.AreEqual(data.Length, table.Rows.Count);
        for (int i = 0; i < data.Length; i++)
        {
          Assert.AreEqual(data[i].Id, table.Rows[i]["id"]);

          if (i == 3)
            Assert.AreEqual("CalcField", table.Rows[i]["name"]); // Сложное поле.
          else if (i == 4)
            Assert.AreEqual("It is CalcFieldCalcField Complex", table.Rows[i]["name"]); // Сложное поле.
          else
            Assert.AreEqual(data[i].Name, table.Rows[i]["name"]); // Простое поле.

          for (int j = 0; j < data[i].DataType.Length; j++)
            Assert.AreEqual(data[i].Id + j, ((byte[])table.Rows[i]["dataType"])[j]);
        }
      }
    }

    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionDataTableExtensionFieldIsNull()
    {
      using (ShimsContext.Create())
      {
        // Для проверки схемы.
        List<ColumnInfo> columns = new List<ColumnInfo>
        {
          new ColumnInfo("name", typeof(string), 0, true)
        };
        TableInfo tableInfo = new TableInfo("TableName", columns, new string[] { "name" });

        // Для проверки данных.
        var data = new[]
        {
          new { Name = "1" },
          new { Name = (string)null }
        };

        // Счетчик для View.Fetch() и получения Record.
        int count = -1;

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.TablesGet = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimTableCollection().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimTableCollection.AllInstances.ItemGetString = delegate { return tableInfo; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.OpenViewStringObjectArray = delegate { return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimView().Instance; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimView.AllInstances.Fetch = delegate { return ++count < data.Length ? new Record() : null; };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimRecord.AllInstances.ItemGetString = (@this, fieldName) =>
        {
          if (fieldName == "name")
            return data[count].Name;
          throw new Exception();
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimInstallerHandle.AllInstances.Dispose = delegate { };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionDataTableExtension extension = new SessionDataTableExtension(session, null);

        DataTable table = extension.CopyTableInfoToDataTable("TableName");

        // Проверяем данные.
        Assert.AreEqual(data.Length, table.Rows.Count);
        for (int i = 0; i < data.Length; i++)
          Assert.AreEqual(data[i].Name == null, table.Rows[i]["name"] == DBNull.Value);
      }
    }
  }

  [TestClass]
  public class SessionDаtabaseInfoExtensionTest
  {
    class StubSessionService : ISessionService
    {
      StubSessionSerializeCustomActionDataExtension stubSessionSerializeCustomActionDataExtension = new StubSessionSerializeCustomActionDataExtension();

      public T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionSerializeCustomActionDataExtension))
          return (T)(object)stubSessionSerializeCustomActionDataExtension;
        throw new NotImplementedException();
      }
    }

    class StubSessionSerializeCustomActionDataExtension : ISessionSerializeCustomActionDataExtension
    {
      DatabaseInfo[] array = null;

      public void SerializeCustomActionData<T>(T data, string key, string[] subscribers)
      {
        throw new NotImplementedException();
      }

      public void SerializeCustomActionData<T>(T data, string key)
      {
        throw new NotImplementedException();
      }

      public void SerializeCustomActionData<T>(T data, string[] subscribers)
      {
        throw new NotImplementedException();
      }

      public void SerializeCustomActionData<T>(T data)
      {
        array = (DatabaseInfo[])(object)data;
      }

      public T DeserializeCustomActionData<T>(string key)
      {
        throw new NotImplementedException();
      }

      public T DeserializeCustomActionData<T>()
      {
        return (T)(object)array;
      }
    }

    void Check(ISessionDatabaseInfoExtension extension, List<DatabaseInfo> list)
    {
      DatabaseInfo[] dbs = extension.GetDatabaseInfos();
      Assert.AreEqual(list.Count, dbs.Length);
      for (int i = 0; i < list.Count; i++ )
      {
        Assert.AreEqual(list[i].Server, dbs[i].Server);
        Assert.AreEqual(list[i].Name, dbs[i].Name);
        Assert.AreEqual(list[i].IsRequiringUpdate, dbs[i].IsRequiringUpdate);
        Assert.AreEqual(list[i].IsNew, dbs[i].IsNew);
        Assert.AreEqual(list[i].Version, dbs[i].Version);
      }
    }

    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionDаtabaseInfoExtensionTesting()
    {
      using (ShimsContext.Create())
      {
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionDatabaseInfoExtension extension = new SessionDatabaseInfoExtension(session, new StubSessionService());
        List<DatabaseInfo> list = new List<DatabaseInfo>();
        DatabaseInfo db;

        // Сохраненной информации нет.
        Check(extension, list);

        // Добавляем одну.
        db = new DatabaseInfo { Server = "server1", Name = "aaa", IsRequiringUpdate = true, IsNew = false, Version = "1.0.0.0" };
        extension.AddDatabaseInfo(db);
        list.Add(db);
        Check(extension, list);

        // Добавляем еще две.
        db = new DatabaseInfo { Server = "server1", Name = "bbb", IsRequiringUpdate = true, IsNew = false, Version = "1.0.0.0" };
        extension.AddDatabaseInfo(db);
        list.Add(db);
        db = new DatabaseInfo { Server = "server2", Name = "ccc", IsRequiringUpdate = true, IsNew = false, Version = "1.0.0.0" };
        extension.AddDatabaseInfo(db);
        list.Add(db);
        Check(extension, list);

        // Добавляем дубликат, ничего не должно измениться.
        db = new DatabaseInfo { Server = "server2", Name = "ccc" };
        extension.AddDatabaseInfo(db);
        Check(extension, list);

        // Удаляем одну, ключи: сервер и имя.
        db = list.FirstOrDefault(v => v.Server == "server1" && v.Name == "bbb");
        extension.DeleteDatabaseInfo(db.Server, db.Name);
        list.Remove(db);
        Check(extension, list);
      }
    }
  }

  [TestClass]
  public class SessionTempDirectoryExtensionTest
  {
    class StubSessionService : ISessionService
    {
      ISessionSerializeCustomActionDataExtension sessionSerializeCustomActionDataExtension = new StubSessionSerializeCustomActionDataExtension();

      public T GetService<T>()
      {
        if (typeof(T) == typeof(ISessionSerializeCustomActionDataExtension))
          return (T)(object)sessionSerializeCustomActionDataExtension;
        throw new NotImplementedException();
      }
    }

    class StubSessionSerializeCustomActionDataExtension : ISessionSerializeCustomActionDataExtension
    {
      public static string TempDirectoryName;

      public void SerializeCustomActionData<T>(T data, string key, string[] subscribers)
      {
        throw new NotImplementedException();
      }

      public void SerializeCustomActionData<T>(T data, string key)
      {
        TempDirectoryName = data.ToString();
      }

      public void SerializeCustomActionData<T>(T data, string[] subscribers)
      {
        throw new NotImplementedException();
      }

      public void SerializeCustomActionData<T>(T data)
      {
        throw new NotImplementedException();
      }

      public T DeserializeCustomActionData<T>(string key)
      {
        return (T)(object)TempDirectoryName;
      }

      public T DeserializeCustomActionData<T>()
      {
        throw new NotImplementedException();
      }
    }

    [TestMethod]
    [TestCategory("SessionExtension")]
    public void SessionTempDirectoryExtensionSuccess()
    {
      using (ShimsContext.Create())
      {
        string exceptedDirectory = "C:\\Temp\\STqwerty.tmp";
        string createdDirectory = "";
        string deletedDirectory = "";

        System.IO.Fakes.ShimPath.GetTempPath = () => { return "C:\\Temp"; };
        System.IO.Fakes.ShimPath.GetRandomFileName = () => { return "qwerty.tmp"; };

        System.IO.Fakes.ShimDirectory.CreateDirectoryString = (path) =>
        {
          createdDirectory = path;
          return null;
        };
        System.IO.Fakes.ShimDirectory.ExistsString = (path) => { return !string.IsNullOrEmpty(createdDirectory) && string.IsNullOrEmpty(deletedDirectory); };
        System.IO.Fakes.ShimDirectory.DeleteStringBoolean = (path, recursive) => { deletedDirectory = path; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionTempDirectoryExtension extension = new SessionTempDirectoryExtension(session, new StubSessionService());

        // Директории не должно быть.
        Assert.IsNull(extension.GetTempDirectory());

        // Создаем директорию.
        extension.CreateTempDirectory();

        // Контроль создания директории.
        Assert.AreEqual(exceptedDirectory, createdDirectory);
        // Проверяем данные для подписчиков.
        Assert.AreEqual(exceptedDirectory, StubSessionSerializeCustomActionDataExtension.TempDirectoryName);

        // Проверка наличии директории.
        Assert.AreEqual(exceptedDirectory, extension.GetTempDirectory());

        // Удаляем директорию.
        extension.DeleteTempDirectory();

        // Контроль удаления директории.
        Assert.AreEqual(exceptedDirectory, deletedDirectory);

        // Директории не должно быть.
        Assert.IsNull(extension.GetTempDirectory());
      }
    }

    [TestMethod]
    [ExpectedException(typeof(SessionTempDirectoryExtension.TempDirectoryAlreadyCreatedException))]
    [TestCategory("SessionExtension")]
    public void SessionTempDirectoryExtensionMultiCreationException()
    {
      using (ShimsContext.Create())
      {
        System.IO.Fakes.ShimPath.GetTempPath = () => { return "C:\\Temp"; };
        System.IO.Fakes.ShimPath.GetRandomFileName = () => { return "qwerty.tmp"; };
        System.IO.Fakes.ShimDirectory.CreateDirectoryString = (path) => { return null; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionTempDirectoryExtension extension = new SessionTempDirectoryExtension(session, new StubSessionService());

        // Создаем директорию.
        extension.CreateTempDirectory();

        // Создаем директорию еще раз, должно быть исключение.
        extension.CreateTempDirectory();
      }
    }

    [TestMethod]
    [ExpectedException(typeof(SessionTempDirectoryExtension.InvalidInstallRunModeException))]
    [TestCategory("SessionExtension")]
    public void SessionTempDirectoryExtensionScheduledModeException()
    {
      using (ShimsContext.Create())
      {
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = (@this, mode) => { return mode == InstallRunMode.Scheduled; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionTempDirectoryExtension extension = new SessionTempDirectoryExtension(session, new StubSessionService());

        // Создаем директорию.
        extension.CreateTempDirectory();
      }
    }

    [TestMethod]
    [ExpectedException(typeof(SessionTempDirectoryExtension.InvalidInstallRunModeException))]
    [TestCategory("SessionExtension")]
    public void SessionTempDirectoryExtensionRollbackModeException()
    {
      using (ShimsContext.Create())
      {
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = (@this, mode) => { return mode == InstallRunMode.Rollback; };

        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        SessionTempDirectoryExtension extension = new SessionTempDirectoryExtension(session, new StubSessionService());

        // Создаем директорию.
        extension.CreateTempDirectory();
      }
    }
  }
}

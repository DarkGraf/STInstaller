using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using WixSTActions.SqlWorker;

namespace WixSTActionsTest
{
  /// <summary>
  /// Тест базового класса SqlWorker.
  /// </summary>
  [TestClass]
  public class SqlWorkerBaseTest
  {
    /// <summary>
    /// Класс-заглушка для тестирования.
    /// </summary>
    class StubSqlWorker : SqlWorkerBase
    {
      public StubSqlWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
        : base(server, database, authenticationType, user, password) { }
      public StubSqlWorker(SqlConnection connection)
        : base(connection) { }

      protected override void DoWork(System.Data.SqlClient.SqlConnection connection) { }

      // Для теста делаем открытые свойства.
      public string ServerProp { get { return Server; } }
      public string DatabaseProp { get { return Database; } }
      public AuthenticationType AuthenticationTypeProp { get { return AuthenticationType; } }
      public string UserProp { get { return User; } }
      public string PasswordProp { get { return Password; } }
    }

    /// <summary>
    /// Создание SqlWorker с аутентификационными данными.
    /// </summary>
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlWorkerBaseCreateWithAuthenticationData()
    {
      using (ShimsContext.Create())
      {
        bool wasInvokedOpen = false;
        bool wasInvokedClose = false;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { wasInvokedOpen = true; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return wasInvokedOpen ? ConnectionState.Open : ConnectionState.Closed; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Close = delegate { wasInvokedClose = true; };

        StubSqlWorker worker = new StubSqlWorker("TESTHOST", "TESTDB", AuthenticationType.Sql, "sa", "pass");
        worker.Execute();
        // Должно вызваться открытие соединения.
        Assert.IsTrue(wasInvokedOpen);
        // Должно вызваться закрытие соединения.
        Assert.IsTrue(wasInvokedClose);
        Assert.AreEqual("TESTHOST", worker.ServerProp);
        Assert.AreEqual("TESTDB", worker.DatabaseProp);
        Assert.AreEqual(AuthenticationType.Sql, worker.AuthenticationTypeProp);
        Assert.AreEqual("sa", worker.UserProp);
        Assert.AreEqual("pass", worker.PasswordProp);
      }
    }

    /// <summary>
    /// Создание SqlWorker с открытым соединением.
    /// </summary>
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlWorkerBaseCreateWithOpenConnection()
    {
      using (ShimsContext.Create())
      {
        bool wasInvokedOpen;
        bool wasInvokedClose;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { wasInvokedOpen = true; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Close = delegate { wasInvokedClose = true; };

        string connectionString = new SqlConnectionStringBuilder { DataSource = "TESTHOST", InitialCatalog = "TESTDB", IntegratedSecurity = false, UserID = "sa", Password = "pass" }.ConnectionString;

        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        // Сбросываем флаги.
        wasInvokedOpen = false;
        wasInvokedClose = false;
        StubSqlWorker worker = new StubSqlWorker(connection);
        worker.Execute();
        // Не должно вызваться открытие соединения.
        Assert.IsFalse(wasInvokedOpen);
        // Не должно вызваться закрытие соединения.
        Assert.IsFalse(wasInvokedClose);
        Assert.AreEqual("TESTHOST", worker.ServerProp);
        Assert.AreEqual("TESTDB", worker.DatabaseProp);
        Assert.AreEqual(AuthenticationType.Sql, worker.AuthenticationTypeProp);
        Assert.AreEqual("sa", worker.UserProp);
        Assert.AreEqual("pass", worker.PasswordProp);
      }
    }

    /// <summary>
    /// Создание SqlWorker с закрытым соединением.
    /// </summary>
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlWorkerBaseCreateWithCloseConnection()
    {
      using (ShimsContext.Create())
      {
        bool wasInvokedOpen = false;
        bool wasInvokedClose = false;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { wasInvokedOpen = true; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return wasInvokedOpen ? ConnectionState.Open : ConnectionState.Closed; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Close = delegate { wasInvokedClose = true; };

        string connectionString = new SqlConnectionStringBuilder { DataSource = "TESTHOST", InitialCatalog = "TESTDB", IntegratedSecurity = false, UserID = "sa", Password = "pass" }.ConnectionString;

        SqlConnection connection = new SqlConnection(connectionString);
        StubSqlWorker worker = new StubSqlWorker(connection);
        worker.Execute();
        // Должно вызваться открытие соединения.
        Assert.IsTrue(wasInvokedOpen);
        // Должно вызваться закрытие соединения.
        Assert.IsTrue(wasInvokedClose);
        Assert.AreEqual("TESTHOST", worker.ServerProp);
        Assert.AreEqual("TESTDB", worker.DatabaseProp);
        Assert.AreEqual(AuthenticationType.Sql, worker.AuthenticationTypeProp);
        Assert.AreEqual("sa", worker.UserProp);
        Assert.AreEqual("pass", worker.PasswordProp);
      }
    }
  }

  [TestClass]
  public class SqlProgressWorkerTest
  {
    class StubSqlProgressWorker : SqlProgressWorker
    {
      public StubSqlProgressWorker(EventHandler<SqlWorkerProgressEventArgs> progressEvent = null) : base("", "", AuthenticationType.Sql, "", "", progressEvent) { }

      protected override void DoWork(System.Data.SqlClient.SqlConnection connection)
      {
        OnProgress(100, 50);
        OnProgress(100, 100);

        // Так как AllCount == CurrentCount, то происходит сброс.
        OnProgress(100, 30);
      }
    }

    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlProgressWorkerTesting()
    {
      List<SqlWorkerProgressEventArgs> list = new List<SqlWorkerProgressEventArgs>();
      EventHandler<SqlWorkerProgressEventArgs> handler = (sender, e) => { list.Add(e); };

      using (ShimsContext.Create())
      {
        //System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

        StubSqlProgressWorker worker = new StubSqlProgressWorker(handler);
        worker.Execute();
      }

      // За одни вызов должны быть пять единиц данных.
      Assert.AreEqual(5, list.Count);

      // Инициализируемые.
      Assert.AreEqual(100, list[0].AllCount);
      Assert.AreEqual(0, list[0].CurrentCount);
      Assert.AreEqual(0, list[0].Increment);
      Assert.IsTrue(list[0].IsInitializedData);
      // Первые рабочие.
      Assert.AreEqual(100, list[1].AllCount);
      Assert.AreEqual(50, list[1].CurrentCount);
      Assert.AreEqual(50, list[1].Increment);
      Assert.IsFalse(list[1].IsInitializedData);
      // Вторые рабочие.
      Assert.AreEqual(100, list[2].AllCount);
      Assert.AreEqual(100, list[2].CurrentCount);
      Assert.AreEqual(50, list[2].Increment);
      Assert.IsFalse(list[2].IsInitializedData);
      // Так как AllCount == CurrentCount, то происходит сброс.
      // Инициализируемые.
      Assert.AreEqual(100, list[3].AllCount);
      Assert.AreEqual(0, list[3].CurrentCount);
      Assert.AreEqual(0, list[3].Increment);
      Assert.IsTrue(list[3].IsInitializedData);
      // Третьи рабочие.
      Assert.AreEqual(100, list[4].AllCount);
      Assert.AreEqual(30, list[4].CurrentCount);
      Assert.AreEqual(30, list[4].Increment);
      Assert.IsFalse(list[4].IsInitializedData);
    }
  }

  [TestClass]
  public class SqlCheckingAdminRightsWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlCheckingAdminRightsWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

        ISqlCheckingAdminRightsWorkerReturnedData data;
        SqlCheckingAdminRightsWorker worker = new SqlCheckingAdminRightsWorker("TESTHOST", AuthenticationType.Sql, "sa", "pass", out data);

        // Есть права.
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteScalar = delegate { return 1; };
        worker.Execute();
        Assert.IsTrue(data.IsAdmin);

        // Нет прав.
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteScalar = delegate { return DBNull.Value; };
        worker.Execute();
        Assert.IsFalse(data.IsAdmin);
      }
    }
  }

  [TestClass]
  public class SqlServerVersionWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlServerVersionWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteScalar = delegate { return 9; };

        ISqlServerVersionWorkerReturnedData data;
        SqlServerVersionWorker worker = new SqlServerVersionWorker("TESTHOST", AuthenticationType.Sql, "sa", "pass", out data);
        worker.Execute();
        Assert.AreEqual(9, data.Version);
      }
    }
  }

  [TestClass]
  public class SqlGetDatabaseFromProcedureWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlGetDatabaseFromProcedureWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

        ISqlGetDatabaseFromProcedureWorkerReturnedData data;
        SqlGetDatabaseFromProcedureWorker worker = new SqlGetDatabaseFromProcedureWorker("TESTHOST", AuthenticationType.Sql, "sa", "pass", "1.3.0", out data);

        // Процедура есть. Данные есть.
        System.Data.Common.Fakes.ShimDbDataAdapter.AllInstances.FillDataSet = (@this, dataSet) =>
        {
          DataTable tbl = new DataTable();
          tbl.Columns.Add("Column1", typeof(int));
          tbl.Rows.Add(new object[] { "1" });
          dataSet.Tables.Add(tbl);

          tbl = new DataTable();
          tbl.Columns.Add("Name", typeof(string));
          tbl.Columns.Add("Version", typeof(string));
          tbl.Columns.Add("IsRequiringUpdate", typeof(bool));
          tbl.Rows.Add(new object[] { "aaa", "1.0.0", true });
          tbl.Rows.Add(new object[] { "bbb", "1.1.0", false });
          dataSet.Tables.Add(tbl);

          return 3;
        };
        worker.Execute();
        Assert.IsTrue(data.ProcedureExist);
        Assert.AreEqual(2, data.Databases.Count);
        for (int i = 0; i < data.Databases.Count; i++)
        {
          Assert.AreEqual(new string[] { "aaa", "bbb" }[i], data.Databases.Keys.ToArray()[i]);
          Assert.AreEqual(new string[] { "1.0.0", "1.1.0" }[i], data.Databases.Values.ToArray()[i].Item1);
          Assert.AreEqual(new bool[] { true, false }[i], data.Databases.Values.ToArray()[i].Item2);
        }

        // Процедура есть. Данных нет.
        System.Data.Common.Fakes.ShimDbDataAdapter.AllInstances.FillDataSet = (@this, dataSet) =>
        {
          DataTable tbl = new DataTable();
          tbl.Columns.Add("Column1", typeof(int));
          tbl.Rows.Add(new object[] { "1" });
          dataSet.Tables.Add(tbl);

          tbl = new DataTable();
          tbl.Columns.Add("Name", typeof(string));
          tbl.Columns.Add("Version", typeof(string));
          tbl.Columns.Add("IsRequiringUpdate", typeof(bool));
          dataSet.Tables.Add(tbl);

          return 1;
        };
        worker.Execute();
        Assert.IsTrue(data.ProcedureExist);
        Assert.AreEqual(0, data.Databases.Count);

        // Процедура отсутствует.
        System.Data.Common.Fakes.ShimDbDataAdapter.AllInstances.FillDataSet = (@this, dataSet) =>
        {
          DataTable tbl = new DataTable();
          tbl.Columns.Add("Column1", typeof(int));
          tbl.Rows.Add(new object[] { "1" });
          dataSet.Tables.Add(tbl);
          return 1;
        };
        worker.Execute();
        Assert.IsFalse(data.ProcedureExist);
      } 
    }
  }

  [TestClass]
  public class SqlGetMsSqlServerProcessWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlGetMsSqlServerProcessWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        // Ожидаемый процесс.
        int pid = 50;
        Process process = new System.Diagnostics.Fakes.ShimProcess().Instance;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteScalar = delegate { return pid; };
        System.Diagnostics.Fakes.ShimProcess.GetProcessByIdInt32 = (processId) => { return processId == pid ? process : null; };

        // Начало теста.
        ISqlGetMsSqlServerProcessWorkerReturnedData data;
        SqlGetMsSqlServerProcessWorker worker = new SqlGetMsSqlServerProcessWorker("TESTHOST", AuthenticationType.Windows, "", "", out data);
        worker.Execute();

        Assert.AreEqual(process, data.Process);
      }
    }
  }

  /// <summary>
  /// Тест класса SqlCheckConnectionWorker.
  /// </summary>
  [TestClass]
  public class SqlCheckConnectionWorkerTest
  {
    /// <summary>
    /// Тест на успешное подключение к базе данных.
    /// </summary>
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlCheckConnectionWorkerSuccess()
    {
      using (ShimsContext.Create())
      {
        bool wasInvoked = false;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate
        {
          wasInvoked = true;
          return ConnectionState.Open;
        };

        SqlCheckConnectionWorker worker = new SqlCheckConnectionWorker("TESTHOST", "TESTDB", AuthenticationType.Sql, "sa", "sa");
        worker.Execute();
        Assert.IsTrue(wasInvoked);
      }
    }

    /// <summary>
    /// Тест на безуспешное подключение к серверу.
    /// </summary>
    [ExpectedException(typeof(SqlWorkerConnectionFaultException))]
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlCheckConnectionWorkerFault()
    {
      using (ShimsContext.Create())
      {
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { throw new Exception(); };

        SqlCheckConnectionWorker worker = new SqlCheckConnectionWorker("TESTHOST", AuthenticationType.Sql, "sa", "sa");
        worker.Execute();
      }
    }
  }

  [TestClass]
  public class SqlDatabaseExistWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlDatabaseExistWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        DataTable table = new DataTable();
        table.Columns.Add("database_name", typeof(string));
        table.Columns.Add("dbid", typeof(short));
        table.Columns.Add("create_date", typeof(DateTime));

        // Открытое соединение.
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };

        ISqlDatabaseExistWorkerReturnedData data;
        SqlDatabaseExistWorker worker = new SqlDatabaseExistWorker("TESTHOST", "DBTest", AuthenticationType.Windows, "", "", out data);

        // Проверка, что база существует.
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.GetSchemaString = (@this, collectionName) =>
        {
          table.Rows.Clear();
          table.Rows.Add(new object[] { "master", 1, DateTime.Now });
          table.Rows.Add(new object[] { "DBTest", 10, DateTime.Now });
          return table;
        };
        worker.Execute();
        Assert.IsTrue(data.DatabaseExists);

        // Проверка, что база не существует.
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.GetSchemaString = (@this, collectionName) =>
        {
          table.Rows.Clear();
          table.Rows.Add(new object[] { "master", 1, DateTime.Now });
          return table;
        };

        worker.Execute();
        Assert.IsFalse(data.DatabaseExists);
      }
    }
  }

  [TestClass]
  public class SqlAttachDatabaseWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlAttachDatabaseWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        // Открытое соединение.
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteScalar = (@this) => 
        {
          bool result = @this.CommandText == string.Format(SqlQueries.AttachDatabase, "NEWDB", "C:\\data.mdf", "C:\\log.ldf");
          return result ? 0 : 1; 
        };

        ISqlAttachDatabaseWorkerReturnedData data;
        SqlAttachDatabaseWorker worker = new SqlAttachDatabaseWorker("TESTHOST", "NEWDB", AuthenticationType.Windows, "", "",
          "C:\\data.mdf", "C:\\log.ldf", out data);
        worker.Execute();

        Assert.IsTrue(data.DatabaseCreated);
      }
    }
  }

  [TestClass]
  public class SqlRunScriptWorkerTest
  {
    /// <summary>
    /// Заглушка ISqlScriptParser.
    /// </summary>
    class StubSqlScriptParser : ISqlScriptParser
    {
      string[] queries = { "select 1", "select 2", "select 3", "select 4", "select 5" };

      public string[] Queries
      {
        get { return queries; }
      }
    }

    /// <summary>
    /// Запуск SqlRunScriptWorker в нормальном режиме.
    /// </summary>
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlRunScriptWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        int allCount = 0; // Всего запросов в callback.
        int curCount = 0; // Номер текущего запроса callback.
        int i = 0; // Счетчик для выполнении запросов.
        ISqlScriptParser parser = new StubSqlScriptParser();

        //System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return ConnectionState.Open; };
        System.Data.SqlClient.Fakes.ShimSqlCommand.AllInstances.ExecuteNonQuery = (@this) =>
        {
          // Комманда выполняется до увеличения счетчика.
          Assert.AreEqual(i, curCount);
          Assert.AreEqual(parser.Queries[i++], @this.CommandText);
          return 1;
        };

        EventHandler<SqlWorkerProgressEventArgs> callback = (sender, e) =>
        {
          allCount = e.AllCount;
          curCount = e.CurrentCount;
        };

        // Начало теста.
        SqlRunScriptWorker worker = new SqlRunScriptWorker("TESTHOST", "DBTest", AuthenticationType.Windows, "", "",
          parser, callback);
        worker.Execute();

        Assert.AreEqual(i, allCount);
        Assert.AreEqual(i, curCount);
      }
    }
  }

  [TestClass]
  public class SqlCreateConnectionWorkerTest
  {
    [TestMethod]
    [TestCategory("SqlWorker")]
    public void SqlCreateConnectionWorkerTesting()
    {
      using (ShimsContext.Create())
      {
        bool active = false;
        bool openingIsInvoked = false;
        bool closingIsInvoked = false;

        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Open = delegate { active = openingIsInvoked = true; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.StateGet = delegate { return active ? ConnectionState.Open : ConnectionState.Closed; };
        System.Data.SqlClient.Fakes.ShimSqlConnection.AllInstances.Close = delegate { closingIsInvoked = true; };

        ISqlWorkerConnection workerConnection;
        SqlCreateConnectionWorker worker = new SqlCreateConnectionWorker("TESTHOST", "DBTest", AuthenticationType.Windows, "", "", out workerConnection);
        // После создание должно быть вызвано только открытие.
        Assert.IsTrue(openingIsInvoked);
        Assert.IsFalse(closingIsInvoked);

        openingIsInvoked = false;
        worker.Execute();
        // После выполения должно быть вызвано только закрытие.
        Assert.IsFalse(openingIsInvoked);
        Assert.IsTrue(closingIsInvoked);
      }
    }
  }
}

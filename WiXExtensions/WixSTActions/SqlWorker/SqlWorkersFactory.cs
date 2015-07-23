using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using WixSTActions.SqlWorker;

namespace WixSTActions.SqlWorker
{
  interface ISqlWorkersFactory
  {
    SqlWorkerBase CreateSqlCheckingAdminRightsWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlCheckingAdminRightsWorkerReturnedData data);

    SqlWorkerBase CreateServerVersionWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlServerVersionWorkerReturnedData data);

    SqlWorkerBase CreateSqlGetDatabaseFromProcedureWorker(string server, AuthenticationType authenticationType, string user, string password, string version,
      out ISqlGetDatabaseFromProcedureWorkerReturnedData data);

    SqlWorkerBase CreateSqlGetMsSqlServerProcessWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlGetMsSqlServerProcessWorkerReturnedData data);

    SqlWorkerBase CreateSqlCheckConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password);

    SqlWorkerBase CreateSqlCheckConnectionWorker(string server, AuthenticationType authenticationType, string user, string password);

    SqlWorkerBase CreateSqlDatabaseExistWorker(string server, string checkedDatabase, AuthenticationType authenticationType, string user, string password,
      out ISqlDatabaseExistWorkerReturnedData data);

    SqlWorkerBase CreateSqlAttachDatabaseWorker(string server, string newDatabase, AuthenticationType authenticationType, string user, string password,
      string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data);

    SqlWorkerBase CreateSqlAttachDatabaseWorker(ISqlWorkerConnection connection, string newDatabase, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data);

    SqlWorkerBase CreateSqlDetachDatabaseWorker(string server, string database, AuthenticationType authenticationType, string user, string password);

    SqlWorkerBase CreateSqlDetachDatabaseWorker(ISqlWorkerConnection connection);

    SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      out ISqlGetPhysicalFilePathWorkerReturnedData data);

    SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(ISqlWorkerConnection connection, out ISqlGetPhysicalFilePathWorkerReturnedData data);

    SqlWorkerBase CreateSqlSetSingleUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password);

    SqlWorkerBase CreateSqlSetSingleUserWorker(ISqlWorkerConnection connection);

    SqlWorkerBase CreateSqlSetMultiUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password);

    SqlWorkerBase CreateSqlSetMultiUserWorker(ISqlWorkerConnection connection);

    SqlWorkerBase CreateSqlCreateConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      out ISqlWorkerConnection workerConnection);

    SqlWorkerBase CreateSqlRunScriptWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null);

    SqlWorkerBase CreateSqlRunScriptWorker(ISqlWorkerConnection connection, ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null);

    SqlWorkerBase CreateSqlUseDatabaseWorker(ISqlWorkerConnection connection, string newDatabase);
  }

  #region Интерфейсы для возврата данных.

  public interface ISqlCheckingAdminRightsWorkerReturnedData
  {
    bool IsAdmin { get; set; }
  }

  public interface ISqlServerVersionWorkerReturnedData
  {
    int Version { get; set; }
  }

  public interface ISqlGetDatabaseFromProcedureWorkerReturnedData
  {
    bool ProcedureExist { get; set; }
    IDictionary<string, Tuple<string, bool>> Databases { get; set; }
  }

  public interface ISqlGetMsSqlServerProcessWorkerReturnedData
  {
    Process Process { get; set; }
  }

  public interface ISqlDatabaseExistWorkerReturnedData
  {
    bool DatabaseExists { get; set; }
  }

  public interface ISqlAttachDatabaseWorkerReturnedData
  {
    bool DatabaseCreated { get; set; }
  }

  public interface ISqlGetPhysicalFilePathWorkerReturnedData
  {
    string MdfFilePath { get; set; }
    string LdfFilePath { get; set; }
  }

  public interface ISqlWorkerConnection
  {
    SqlConnection Connection { get; }
  }

  #endregion

  public class SqlWorkersFactory : ISqlWorkersFactory
  {
    public SqlWorkerBase CreateSqlCheckingAdminRightsWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlCheckingAdminRightsWorkerReturnedData data)
    {
      return new SqlCheckingAdminRightsWorker(server, authenticationType, user, password, out data);
    }

    public SqlWorkerBase CreateServerVersionWorker(string server, AuthenticationType authenticationType, string user, string password, 
      out ISqlServerVersionWorkerReturnedData data)
    {
      return new SqlServerVersionWorker(server, authenticationType, user, password, out data);
    }

    public SqlWorkerBase CreateSqlGetDatabaseFromProcedureWorker(string server, AuthenticationType authenticationType, string user, string password, string version, 
      out ISqlGetDatabaseFromProcedureWorkerReturnedData data)
    {
      return new SqlGetDatabaseFromProcedureWorker(server, authenticationType, user, password, version, out data);
    }

    public SqlWorkerBase CreateSqlGetMsSqlServerProcessWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlGetMsSqlServerProcessWorkerReturnedData data)
    {
      return new SqlGetMsSqlServerProcessWorker(server, authenticationType, user, password, out data);
    }

    public SqlWorkerBase CreateSqlCheckConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      return new SqlCheckConnectionWorker(server, database, authenticationType, user, password);
    }

    public SqlWorkerBase CreateSqlCheckConnectionWorker(string server, AuthenticationType authenticationType, string user, string password)
    {
      return new SqlCheckConnectionWorker(server, authenticationType, user, password);
    }

    public SqlWorkerBase CreateSqlDatabaseExistWorker(string server, string checkedDatabase, AuthenticationType authenticationType, string user, string password, out ISqlDatabaseExistWorkerReturnedData data)
    {
      return new SqlDatabaseExistWorker(server, checkedDatabase, authenticationType, user, password, out data);
    }

    public SqlWorkerBase CreateSqlAttachDatabaseWorker(string server, string newDatabase, AuthenticationType authenticationType, string user, string password, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data)
    {
      return new SqlAttachDatabaseWorker(server, newDatabase, authenticationType, user, password, pathToMdf, pathToLdf, out data);
    }

    public SqlWorkerBase CreateSqlAttachDatabaseWorker(ISqlWorkerConnection connection, string newDatabase, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data)
    {
      return new SqlAttachDatabaseWorker(connection.Connection, newDatabase, pathToMdf, pathToLdf, out data);
    }

    public SqlWorkerBase CreateSqlDetachDatabaseWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      return new SqlDetachDatabaseWorker(server, database, authenticationType, user, password);
    }

    public SqlWorkerBase CreateSqlDetachDatabaseWorker(ISqlWorkerConnection connection)
    {
      return new SqlDetachDatabaseWorker(connection.Connection);
    }

    public SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(string server, string database, AuthenticationType authenticationType, string user, string password, out ISqlGetPhysicalFilePathWorkerReturnedData data)
    {
      return new SqlGetPhysicalFilePathWorker(server, database, authenticationType, user, password, out data);
    }

    public SqlWorkerBase CreateSqlGetPhysicalFilePathWorker(ISqlWorkerConnection connection, out ISqlGetPhysicalFilePathWorkerReturnedData data)
    {
      return new SqlGetPhysicalFilePathWorker(connection.Connection, out data);
    }

    public SqlWorkerBase CreateSqlSetSingleUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      return new SqlSetSingleMultiUserWorker(server, database, authenticationType, user, password, SqlUserMode.Single);
    }

    public SqlWorkerBase CreateSqlSetSingleUserWorker(ISqlWorkerConnection connection)
    {
      return new SqlSetSingleMultiUserWorker(connection.Connection, SqlUserMode.Single);
    }

    public SqlWorkerBase CreateSqlSetMultiUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      return new SqlSetSingleMultiUserWorker(server, database, authenticationType, user, password, SqlUserMode.Multi);
    }

    public SqlWorkerBase CreateSqlSetMultiUserWorker(ISqlWorkerConnection connection)
    {
      return new SqlSetSingleMultiUserWorker(connection.Connection, SqlUserMode.Multi);
    }

    public SqlWorkerBase CreateSqlCreateConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password, out ISqlWorkerConnection workerConnection)
    {
      return new SqlCreateConnectionWorker(server, database, authenticationType, user, password, out workerConnection);
    }

    public SqlWorkerBase CreateSqlRunScriptWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
    {
      return new SqlRunScriptWorker(server, database, authenticationType, user, password, parser, progressEvent);
    }

    public SqlWorkerBase CreateSqlRunScriptWorker(ISqlWorkerConnection connection, ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
    {
      return new SqlRunScriptWorker(connection.Connection, parser, progressEvent);
    }

    public SqlWorkerBase CreateSqlUseDatabaseWorker(ISqlWorkerConnection connection, string newDatabase)
    {
      return new SqlUseDatabaseWorker(connection.Connection, newDatabase);
    }
  }
}

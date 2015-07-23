using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Создает открытое соединение, используемое в других Worker.
  /// При выполнении Execute, закрывает соединение.
  /// </summary>
  class SqlCreateConnectionWorker : SqlWorkerBase
  {
    public class ReturnedData : ISqlWorkerConnection
    {
      public ReturnedData(SqlConnection connection)
      {
        Connection = connection;
      }
      public SqlConnection Connection { get; private set; }
    }

    public SqlCreateConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      out ISqlWorkerConnection workerConnection) 
      : this(new SqlConnection(GetConnectionString(server, database, authenticationType, user, password)), out workerConnection) { }

    private SqlCreateConnectionWorker(SqlConnection connection, out ISqlWorkerConnection workerConnection)
      : base(connection)
    {
      ReturnedData data;
      workerConnection = data = new ReturnedData(connection);
      data.Connection.Open();
    }

    protected override void DoWork(SqlConnection connection)
    {
      connection.Close();
    }
  }
}

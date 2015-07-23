using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  class SqlDetachDatabaseWorker : SqlWorkerBase
  {
    public SqlDetachDatabaseWorker(string server, string database, AuthenticationType authenticationType, string user, string password) 
      : base(server, database, authenticationType, user, password) { }

    public SqlDetachDatabaseWorker(SqlConnection connection)
      : base(connection) { }

    protected override void DoWork(System.Data.SqlClient.SqlConnection connection)
    {
      string query = SqlQueries.SetSingleUser + Environment.NewLine + SqlQueries.DetachDatabase;
      query = string.Format(query, Database);
      SqlCommand command = connection.CreateCommand();
      command.CommandText = query;
      command.ExecuteNonQuery();      
    }
  }
}

using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  enum SqlUserMode { Single, Multi }

  class SqlSetSingleMultiUserWorker : SqlWorkerBase
  {
    SqlUserMode mode;

    public SqlSetSingleMultiUserWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      SqlUserMode mode) 
      : base(server, database, authenticationType, user, password) 
    {
      this.mode = mode;
    }

    public SqlSetSingleMultiUserWorker(SqlConnection connection, SqlUserMode mode)
      : base(connection)
    {
      this.mode = mode;
    }

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = connection.CreateCommand();
      string query = mode == SqlUserMode.Single ? SqlQueries.SetSingleUser : SqlQueries.SetMultiUser;
      command.CommandText = string.Format(query, Database);
      command.ExecuteNonQuery();
    }
  }
}

using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Переключение баз данных.
  /// </summary>
  class SqlUseDatabaseWorker : SqlWorkerBase
  {
    string newDatabase;

    public SqlUseDatabaseWorker(SqlConnection connection, string newDatabase) : base(connection) 
    {
      this.newDatabase = newDatabase;
    }

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = connection.CreateCommand();
      command.CommandText = string.Format(SqlQueries.UseDatabase, newDatabase);
      command.ExecuteNonQuery();
    }
  }
}

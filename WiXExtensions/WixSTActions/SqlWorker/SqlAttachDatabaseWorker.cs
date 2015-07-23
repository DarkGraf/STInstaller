using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  class SqlAttachDatabaseWorker : SqlWorkerBase
  {
    /// <summary>
    /// Возвращаемые данные методом Execute();
    /// </summary>
    public class ReturnedData : ISqlAttachDatabaseWorkerReturnedData
    {
      public bool DatabaseCreated { get; set; }
    }

    ReturnedData data;
    string pathToMdf;
    string pathToLdf;
    string newDatabase;

    public SqlAttachDatabaseWorker(string server, string newDatabase, AuthenticationType authenticationType, string user, string password,
      string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data) 
      : base(server, "", authenticationType, user, password) 
    {
      data = this.data = new ReturnedData();
      this.newDatabase = newDatabase;
      this.pathToMdf = pathToMdf;
      this.pathToLdf = pathToLdf;
    }

    public SqlAttachDatabaseWorker(SqlConnection connection, string newDatabase, string pathToMdf, string pathToLdf, out ISqlAttachDatabaseWorkerReturnedData data)
      : base(connection)
    {
      data = this.data = new ReturnedData();
      this.newDatabase = newDatabase;
      this.pathToMdf = pathToMdf;
      this.pathToLdf = pathToLdf;
    }

    protected override void DoWork(SqlConnection connection)
    {
      string query = string.Format(SqlQueries.AttachDatabase, newDatabase, pathToMdf, pathToLdf);
      SqlCommand command = connection.CreateCommand();
      command.CommandText = query;
      object obj = command.ExecuteScalar();
      int result;
      if (obj != null && Int32.TryParse(obj.ToString(), out result))
        data.DatabaseCreated = result == 0;
      else
        data.DatabaseCreated = false;
    }
  }
}

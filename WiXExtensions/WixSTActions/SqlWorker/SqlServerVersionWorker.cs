using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  class SqlServerVersionWorker : SqlWorkerBase
  {
    class SqlServerVersionWorkerReturnedData : ISqlServerVersionWorkerReturnedData
    {
      public int Version { get; set; }
    }

    SqlServerVersionWorkerReturnedData data;

    public SqlServerVersionWorker(string server, AuthenticationType authenticationType, string user, string password, out ISqlServerVersionWorkerReturnedData data)
      : base(server, "", authenticationType, user, password) 
    {
      data = this.data = new SqlServerVersionWorkerReturnedData();
    }

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = new SqlCommand(SqlQueries.ServerVersion, connection);
      object obj = command.ExecuteScalar();
      data.Version = 0;
      int result;
      if (obj != DBNull.Value && int.TryParse(obj.ToString(), out result))
        data.Version = result;
    }
  }
}

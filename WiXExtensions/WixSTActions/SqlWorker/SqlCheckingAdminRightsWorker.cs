using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Проверяет, является ли текущий 
  /// пользователь является администратором.
  /// </summary>
  class SqlCheckingAdminRightsWorker : SqlWorkerBase
  {
    public class ReturnedData : ISqlCheckingAdminRightsWorkerReturnedData
    {
      public bool IsAdmin { get; set; }
    }

    ReturnedData data;

    public SqlCheckingAdminRightsWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlCheckingAdminRightsWorkerReturnedData data) : base(server, "", authenticationType, user, password)
    {
      data = this.data = new ReturnedData();
    }

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = new SqlCommand(SqlQueries.IsAdmin, connection);
      object obj = command.ExecuteScalar();
      data.IsAdmin = obj != DBNull.Value && (int)obj == 1;
    }
  }
}

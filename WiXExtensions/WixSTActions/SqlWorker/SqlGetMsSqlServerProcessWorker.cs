using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Получает процесс сервера базы данных.
  /// </summary>
  class SqlGetMsSqlServerProcessWorker : SqlWorkerBase
  {
    /// <summary>
    /// Возвращаемые данные методом Execute();
    /// </summary>
    public class ReturnedData : ISqlGetMsSqlServerProcessWorkerReturnedData
    {
      public Process Process { get; set; }
    }

    ReturnedData data;

    public SqlGetMsSqlServerProcessWorker(string server, AuthenticationType authenticationType, string user, string password,
      out ISqlGetMsSqlServerProcessWorkerReturnedData data) : base(server, "", authenticationType, user, password)
    {
      data = this.data = new ReturnedData();
    }

    #region SqlWorker

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = new SqlCommand(SqlQueries.GetPid, connection);
      object objPid = command.ExecuteScalar();

      int pid;
      if (objPid != null && Int32.TryParse(objPid.ToString(), out pid))
      {
        try
        {
          data.Process = Process.GetProcessById(pid);
        }
        catch { }
      }
    }

    #endregion
  }
}

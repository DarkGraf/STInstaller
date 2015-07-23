using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  class SqlGetPhysicalFilePathWorker : SqlWorkerBase
  {
    public class ReturnedData : ISqlGetPhysicalFilePathWorkerReturnedData
    {
      public string MdfFilePath { get; set; }
      public string LdfFilePath { get; set; }
    }

    ReturnedData data;

    public SqlGetPhysicalFilePathWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      out ISqlGetPhysicalFilePathWorkerReturnedData data)
      : base(server, database, authenticationType, user, password)
    {
      data = this.data = new ReturnedData();
    }

    public SqlGetPhysicalFilePathWorker(SqlConnection connection, out ISqlGetPhysicalFilePathWorkerReturnedData data)
      : base(connection)
    {
      data = this.data = new ReturnedData();
    }

    protected override void DoWork(SqlConnection connection)
    {
      SqlCommand command = connection.CreateCommand();
      command.CommandText = SqlQueries.GetPhysicalFilePath;
      using (SqlDataReader reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          // Предполагается что файлы данных и файлы лога не разбиты, т. е.
          // всего две строки в результате.
          if (reader["type"].Equals("ROWS"))
            data.MdfFilePath = reader["path"].ToString();
          if (reader["type"].Equals("LOG"))
            data.LdfFilePath = reader["path"].ToString();
        }
      }
    }
  }
}

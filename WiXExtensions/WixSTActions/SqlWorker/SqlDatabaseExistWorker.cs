using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Проверка наличия базы данных на сервере.
  /// </summary>
  class SqlDatabaseExistWorker : SqlWorkerBase
  {
    public class ReturnedData : ISqlDatabaseExistWorkerReturnedData
    {
      public bool DatabaseExists { get; set; }
    }

    private string checkedDatabase;
    ReturnedData data;

    public SqlDatabaseExistWorker(string server, string checkedDatabase, AuthenticationType authenticationType, string user, string password,
      out ISqlDatabaseExistWorkerReturnedData data) : base(server, "", authenticationType, user, password)
    {
      this.checkedDatabase = checkedDatabase;
      data = this.data = new ReturnedData();
    }

    #region SqlWorker

    protected override void DoWork(SqlConnection connection)
    {
      DataTable bases = connection.GetSchema("Databases");
      // Не обращаем внимание не регистр.
      data.DatabaseExists = bases.AsEnumerable().FirstOrDefault(v => v.Field<string>(0).ToLower() == checkedDatabase.ToLower()) != null;
    }

    #endregion
  }
}

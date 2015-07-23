using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace WixSTActions.SqlWorker
{
  class SqlGetDatabaseFromProcedureWorker : SqlWorkerBase
  {
    class SqlGetDatabaseFromProcedureWorkerReturnedData : ISqlGetDatabaseFromProcedureWorkerReturnedData
    {
      public bool ProcedureExist { get; set; }
      /// <summary>
      /// Ключ - имя базы данных.
      /// Значение - кортеж из версии и признака необходимости обновления.
      /// </summary>
      public IDictionary<string, Tuple<string, bool>> Databases { get; set; }
    }

    SqlGetDatabaseFromProcedureWorkerReturnedData data;
    string version;

    public SqlGetDatabaseFromProcedureWorker(string server, AuthenticationType authenticationType, string user, string password, string version,
      out ISqlGetDatabaseFromProcedureWorkerReturnedData data) : base(server, "", authenticationType, user, password)
    {
      data = this.data = new SqlGetDatabaseFromProcedureWorkerReturnedData();
      this.version = version;
    }

    protected override void DoWork(SqlConnection connection)
    {
      DataSet dataSet = new DataSet();
      SqlCommand command = new SqlCommand(SqlQueries.GetDatabaseFromProcedure, connection);
      command.Parameters.Add("@Version", SqlDbType.Char, 19);
      command.Parameters["@Version"].Value = version;
      SqlDataAdapter adapter = new SqlDataAdapter(command);
      adapter.Fill(dataSet);

      // Если количество таблиц в наборе данных равно двум, то
      // процедура есть и она выполнилась. Иначе, процедуры нет.
      data.ProcedureExist = dataSet.Tables.Count == 2;

      // Если таблицы есть, получаем их.
      if (data.ProcedureExist)
      {
        data.Databases = (from row in dataSet.Tables[1].AsEnumerable()
                         select new
                         {
                           // Имя базы данных.
                           Name = row.Field<string>("Name"),
                           // Версия базы данных.
                           Version = row.Field<string>("Version"),
                           // Признак, требует ли база данных обновления.
                           IsRequiringUpdate = row.Field<bool>("IsRequiringUpdate")
                         }).ToDictionary(key => key.Name, val => new Tuple<string, bool>(val.Version, val.IsRequiringUpdate));
      }
    }
  }
}

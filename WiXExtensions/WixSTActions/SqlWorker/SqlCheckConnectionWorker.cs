using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Проверка подключения к серверу баз данных.
  /// </summary>
  class SqlCheckConnectionWorker : SqlWorkerBase
  {
    /// <summary>
    /// Проверка подключения к серверу и базе данных.
    /// </summary>
    public SqlCheckConnectionWorker(string server, string database, AuthenticationType authenticationType, string user, string password)
      : base(server, database, authenticationType, user, password) { }

    /// <summary>
    /// Проверка подключения только к серверу.
    /// </summary>
    public SqlCheckConnectionWorker(string server, AuthenticationType authenticationType, string user, string password)
      : this(server, "", authenticationType, user, password) { }

    #region SqlWorker

    protected override void DoWork(SqlConnection connection) { }

    #endregion
  }
}

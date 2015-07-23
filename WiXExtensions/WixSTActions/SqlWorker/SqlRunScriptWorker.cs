using System;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  public interface ISqlScriptParser
  {
    string[] Queries { get; }
  }

  /// <summary>
  /// Выполняет скрипты.
  /// </summary>
  class SqlRunScriptWorker : SqlProgressWorker
  {
    private ISqlScriptParser parser;

    public SqlRunScriptWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
      : base(server, database, authenticationType, user, password, progressEvent)
    {
      this.parser = parser;
    }

    public SqlRunScriptWorker(SqlConnection connection, ISqlScriptParser parser, EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
      : base(connection, progressEvent)
    {
      this.parser = parser;
    }

    #region SqlWorker

    protected override void DoWork(SqlConnection connection)
    {
      // Выполняем скрипт.
      try
      {
        int allCount = parser.Queries.Length;
        int currentCount = 0;

        foreach (string query in parser.Queries)
        {
          SqlCommand command = new SqlCommand(query, connection);
          command.ExecuteNonQuery();

          OnProgress(allCount, ++currentCount);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    #endregion
  }
}

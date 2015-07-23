using System;
using System.Data;
using System.Data.SqlClient;

namespace WixSTActions.SqlWorker
{
  /// <summary>
  /// Тип аутентификации к SQL серверу.
  /// </summary>
  public enum AuthenticationType { Windows, Sql }

  /// <summary>
  /// Базовый класс для работы с базой данных.
  /// Реализован по паттерну «Шаблонный метод».
  /// </summary>
  public abstract class SqlWorkerBase
  {
    private SqlConnection connection = null;
    /// <summary>
    /// Признак, что нужно освобождать подключение.
    /// </summary>
    private bool shouldReleaseRonnection = false;

    protected string Server { get; private set; }
    protected string Database { get; private set; }
    protected AuthenticationType AuthenticationType { get; private set; }
    protected string User { get; private set; }
    protected string Password { get; private set; }

    #region Конструкторы

    /// <summary>
    /// Создает объект с новым активным подключением.
    /// </summary>
    public SqlWorkerBase(string server, string database, AuthenticationType authenticationType, string user, string password)
    {
      Server = server;
      Database = database;
      AuthenticationType = authenticationType;
      User = user;
      Password = password;
    }

    /// <summary>
    /// Создает объект с существующим подключением. Если подключение было закрыто, то оно
    /// откроется на время работы и затем закроется. Если подключение было открыто, то оно
    /// не изменяется.
    /// </summary>
    public SqlWorkerBase(SqlConnection connection)
    {
      // Запомним аутентификационные данные.
      SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
      Server = builder.DataSource;
      Database = builder.InitialCatalog;
      AuthenticationType = builder.IntegratedSecurity ? AuthenticationType.Windows : AuthenticationType.Sql;
      User = builder.UserID;
      Password = builder.Password;

      this.connection = connection;
    }

    #endregion

    /// <summary>
    /// Выполнение основной функциональности.
    /// </summary>
    public void Execute()
    {
      try
      {
        SqlConnection connection = CreateConnection();

        if (OpenConnection(connection))
          DoWork(connection);
        else
          throw new SqlWorkerConnectionFaultException();
      }
      finally
      {
        DisposeConnection();
      }
    }

    #region Абстрактные члены.

    /// <summary>
    /// Основная работа с открытым соединением.
    /// </summary>
    /// <param name="connection"></param>
    protected abstract void DoWork(SqlConnection connection);

    #endregion

    #region Частные методы.

    /// <summary>
    /// Создает (если не создан) объект подключения и возвращает его.
    /// </summary>
    /// <returns></returns>
    private SqlConnection CreateConnection()
    {
      // Если подключения нет, то создаем.
      if (connection == null)
      {
        // Получаем строку соединения.
        string connectionString = GetConnectionString(Server, Database, AuthenticationType, User, Password);
        connection = new SqlConnection(connectionString);        
      }
      return connection;
    }

    /// <summary>
    /// Активирует (если не активно) подключение и возвращает результат.
    /// </summary>
    /// <returns></returns>
    private bool OpenConnection(SqlConnection connection)
    {
      try
      {
        if (connection.State != ConnectionState.Open)
        {
          connection.Open();
          // Если сами открыли соедиение, то его сами и закроем потом.
          shouldReleaseRonnection = true;
        }
        return connection.State == ConnectionState.Open;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Освобождает подключение, если оно создано самим объектом.
    /// </summary>
    private void DisposeConnection()
    {
      // Если сами открыли соедиение, то его сами и закроем.
      if (shouldReleaseRonnection)
        connection.Dispose();
    }

    #endregion

    #region Статические методы.

    /// <summary>
    /// На основе входных данных создает строку соединения.
    /// </summary>
    /// <param name="server">Имя сервера.</param>
    /// <param name="dataBase">Имя базы данных.</param>
    /// <param name="authType">Тип аутентификации.</param>
    /// <param name="user">Имя пользователя.</param>
    /// <param name="password">Пароль.</param>
    /// <returns></returns>
    protected static string GetConnectionString(string server, string database, AuthenticationType authType, string user, string password)
    {
      SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
      builder.DataSource = server;
      builder.InitialCatalog = database;
      builder.IntegratedSecurity = authType == AuthenticationType.Windows;
      builder.ConnectTimeout = 10;

      if (!builder.IntegratedSecurity)
      {
        builder.UserID = user;
        builder.Password = password;
      }

      return builder.ConnectionString;
    }

    #endregion
  }

  /// <summary>
  /// Данные для события EventHandler&lt;SqlWorkerProgressEventArgs&gt; в классе SqlProgressWorker.
  /// </summary>
  public class SqlWorkerProgressEventArgs : EventArgs
  {
    /// <summary>
    /// Общее количество.
    /// </summary>
    public int AllCount { get; private set; }

    /// <summary>
    /// Текущее количество.
    /// </summary>
    public int CurrentCount { get; private set; }

    public int Increment { get; private set; }

    /// <summary>
    /// Признак инициализируемых данных. Посылаются один самыми первыми.
    /// </summary>
    public bool IsInitializedData { get; private set; }

    public SqlWorkerProgressEventArgs(int allCount, int currentCount, int increment, bool isInitializedData)
    {
      AllCount = allCount;
      CurrentCount = currentCount;
      Increment = increment;
      IsInitializedData = isInitializedData;
    }
  }

  /// <summary>
  /// Расширение с возможностью генерации события прогресса.
  /// </summary>
  public abstract class SqlProgressWorker : SqlWorkerBase
  {
    /// <summary>
    /// Событие показывающие прогресс выполнения скриптов.
    /// </summary>
    private event EventHandler<SqlWorkerProgressEventArgs> progressEvent;

    /// <summary>
    /// Признак отправленных инициализируемых данных.
    /// </summary>
    private bool initializedDataAreSent;

    /// <summary>
    /// Предыдущее текущее количество.
    /// </summary>
    private int prevCurrentCount;

    public SqlProgressWorker(string server, string database, AuthenticationType authenticationType, string user, string password,
      EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
      : base(server, database, authenticationType, user, password)
    {
      this.progressEvent = progressEvent;
      initializedDataAreSent = false;
      prevCurrentCount = 0;
    }

    public SqlProgressWorker(SqlConnection connection,
      EventHandler<SqlWorkerProgressEventArgs> progressEvent = null)
      : base(connection)
    {
      this.progressEvent = progressEvent;
      initializedDataAreSent = false;
      prevCurrentCount = 0;
    }

    protected void OnProgress(int allCount, int currentCount)
    {
      if (progressEvent != null)
      {
        if (!initializedDataAreSent)
        {
          // Если инициализируемые данные не отправляли, то отправляем.
          progressEvent(this, new SqlWorkerProgressEventArgs(allCount, 0, 0, true));
          initializedDataAreSent = true;
        }

        SqlWorkerProgressEventArgs e = new SqlWorkerProgressEventArgs(allCount, currentCount, currentCount - prevCurrentCount, false);
        progressEvent(this, e);

        prevCurrentCount = currentCount;

        // Если полное колличество равно текущему, то восстанавливаем
        // признак инициализируемых данных.
        if (allCount == currentCount)
        {
          initializedDataAreSent = false;
          prevCurrentCount = 0;
        }
      }
    }
  }

  public class SqlWorkerConnectionFaultException : Exception
  {
    public override string Message
    {
      get { return "Ошибка подключения серверу к баз данных."; }
    }
  }  
}

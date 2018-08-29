using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.SqlWorker;
using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  interface ICheckConnectionSessionProperties
  {
    /// <summary>
    /// Имя сервера (для проверки).
    /// </summary>
    string Server { get; }
    /// <summary>
    /// Имя базы данные (для проверки).
    /// </summary>
    string Database { get; }
    /// <summary>
    /// Признак успешного подключения.
    /// </summary>
    string ConnectionSuccessful { get; set; }
    /// <summary>
    /// Сообщение для пользователя.
    /// </summary>
    string StringMessage { get; set; }
  }

  enum CheckConnectionType
  {
    OnlyServer,
    DatabaseMustExist,
    DatabaseMustNotExist
  }

  /// <summary>
  /// Проверяет корректность заполнения полей, подключение к серверу базы данных и отсутствие таблицы в базе данных.
  /// В случае успеха устанавливает "1" и возвращает текстовое описание состояния в указанные свойства.
  /// </summary>
  class ActionCheckConnectionWorker : ActionWorkerBase
  {
    internal static class ConnectionResult
    {
      public const string Success = "1";
      public const string Failure = "";
    }

    internal static class StringsMessages
    {
      public const string ConnectionSuccessful = "Проверка соединения выполнена.";
      public const string ConnectionNoSuccessful = "Не выполнена проверка соединения из-за ошибки инициализации поставщика.";
      public const string DataBaseExist = "База данных с данным именем уже существует на сервере.";
      public const string DataBaseNotExist = "База данных с данным именем не существует на сервере.";
      public const string ServerNameIsNotCorrect = "Необходимо указать корректное имя сервера.";
      public const string DataBaseNameIsNotCorrect = "Необходимо указать корректное имя базы данных.";
    }

    ICheckConnectionSessionProperties sessionProp;

    /// <summary>
    /// Тип проверки.
    /// </summary>
    CheckConnectionType checkConnectionType;
    ISqlWorkersFactory factory;

    public ActionCheckConnectionWorker(Session session, CheckConnectionType checkConnectionType, ICheckConnectionSessionProperties sessionProp,
      ISqlWorkersFactory factory) : base(session)
    {
      this.checkConnectionType = checkConnectionType;
      this.sessionProp = sessionProp;
      this.factory = factory;
    }

    #region ActionWorkerBase

    protected override UIType UITypeMode
    {
      get { return UIType.Server; }
    }

    protected override bool IsAllowed
    {
      get { return true; }
    }

    protected override ActionResult DoWork()
    {
      CheckCorrectField();

      if (sessionProp.ConnectionSuccessful == ConnectionResult.Success)
        CheckConnectionToServer();

      if (checkConnectionType != CheckConnectionType.OnlyServer && sessionProp.ConnectionSuccessful == ConnectionResult.Success)
        DataBaseExists();

      if (sessionProp.ConnectionSuccessful == ConnectionResult.Success)
        sessionProp.StringMessage = StringsMessages.ConnectionSuccessful;

      return ActionResult.Success;
    }

    #endregion

    #region Отдельные проверки.

    /// <summary>
    /// Проверка на корректные поля.
    /// </summary>
    /// <returns></returns>
    void CheckCorrectField()
    {
      Func<string, bool> func = (str) =>
      {
        foreach (char c in str)
          if (!(c >= 'A' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '-'))
            return true;
        return false;
      };

      sessionProp.ConnectionSuccessful = ConnectionResult.Failure;

      if (string.IsNullOrWhiteSpace(sessionProp.Server) || func(sessionProp.Server))
      {
        sessionProp.StringMessage = StringsMessages.ServerNameIsNotCorrect;
        return;
      }

      if (checkConnectionType != CheckConnectionType.OnlyServer && (string.IsNullOrWhiteSpace(sessionProp.Database) || func(sessionProp.Database)))
      {
        sessionProp.StringMessage = StringsMessages.DataBaseNameIsNotCorrect;
        return;
      }

      sessionProp.ConnectionSuccessful = ConnectionResult.Success;
    }

    /// <summary>
    /// Проверяет подключение к серверу.
    /// </summary>
    void CheckConnectionToServer()
    {
      SqlWorkerBase worker =  factory.CreateSqlCheckConnectionWorker(sessionProp.Server, AuthenticationType.Windows, "", "");
      try
      {
        worker.Execute();
        sessionProp.ConnectionSuccessful = ConnectionResult.Success;
      }
      catch
      {
        sessionProp.ConnectionSuccessful = ConnectionResult.Failure;
      }

      if (sessionProp.ConnectionSuccessful == ConnectionResult.Failure)
        sessionProp.StringMessage = StringsMessages.ConnectionNoSuccessful;
    }

    /// <summary>
    /// Проверяет наличие базы данных на сервере. 
    /// </summary>
    void DataBaseExists()
    {
      ISqlDatabaseExistWorkerReturnedData data;
      SqlWorkerBase worker = factory.CreateSqlDatabaseExistWorker(sessionProp.Server, sessionProp.Database, AuthenticationType.Windows, "", "", out data);
      try
      {
        worker.Execute();
      }
      catch (Exception)
      {
        // Неизвестная ошибка.
        sessionProp.ConnectionSuccessful = ConnectionResult.Failure;
        sessionProp.StringMessage = StringsMessages.ConnectionNoSuccessful;
        return;
      }

      if (checkConnectionType == CheckConnectionType.DatabaseMustExist)
      {
        // В данном случае правильным является наличие базы данных.
        sessionProp.ConnectionSuccessful = data.DatabaseExists ? ConnectionResult.Success : ConnectionResult.Failure;
      }
      else if (checkConnectionType == CheckConnectionType.DatabaseMustNotExist)
      {
        // В данном случае правильным является отсутствие базы данных.
        sessionProp.ConnectionSuccessful = !data.DatabaseExists ? ConnectionResult.Success : ConnectionResult.Failure;
      }
      else
      {
        // Неправильное состояние.
        sessionProp.ConnectionSuccessful = ConnectionResult.Failure;
        sessionProp.StringMessage = StringsMessages.ConnectionNoSuccessful;
        return;
      }

      if (data.DatabaseExists)
        sessionProp.StringMessage = StringsMessages.DataBaseExist;
      else
        sessionProp.StringMessage = StringsMessages.DataBaseNotExist;
    }

    #endregion
  }
}

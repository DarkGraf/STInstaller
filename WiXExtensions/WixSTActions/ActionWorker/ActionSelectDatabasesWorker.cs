using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.Utils;
using WixSTActions.SqlWorker;
using WixSTActions.WixControl;

namespace WixSTActions.ActionWorker
{
  enum ActionSelectDatabasesWorkerMode
  {
    /// <summary>
    /// Инициализация ActionSelectDatabasesWorker.
    /// </summary>
    Initialization,
    /// <summary>
    /// Добавление новой базы.
    /// </summary>
    AddingNew,
    /// <summary>
    /// Добавление существующей базы.
    /// </summary>
    AddingExisting,
    /// <summary>
    /// Удаление базы.
    /// </summary>
    Deleting
  }

  interface ISelectDatabasesSessionProperties
  {
    /// <summary>
    /// Версия инсталлятора, используется для определения актуальных баз из хранимой процедуры.
    /// </summary>
    string Version { get; }
    /// <summary>
    /// Получение выбранной базы данных в форме списка баз данных.
    /// </summary>
    string SelectedDatabase { get; }
    /// <summary>
    /// Получение выбранного сервера в форме списка серверов.
    /// </summary>
    string SelectedServer { get; }
    /// <summary>
    /// Получения имени базы данных в форме создания новой базы.
    /// </summary>
    string NewDatabase { get; set; }
    /// <summary>
    /// Получения имени базы данных в форме добавления существующей базы.
    /// </summary>
    string ExistDatabase { get; }
    /// <summary>
    /// Элемент управления списоком существующих баз данных.
    /// </summary>
    string ExistControlProperty { get; }
  }

  /// <summary>
  /// Получение баз данных.
  /// </summary>
  class ActionSelectDatabasesWorker : ActionWorkerBase
  {
    ActionSelectDatabasesWorkerMode mode;
    ISelectDatabasesSessionProperties sessionProp;
    ISqlWorkersFactory factory;

    public ActionSelectDatabasesWorker(Session session, ActionSelectDatabasesWorkerMode mode, ISelectDatabasesSessionProperties sessionProp, ISqlWorkersFactory factory)
      : base(session)
    {
      this.mode = mode;
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
      switch (mode)
      {
        case ActionSelectDatabasesWorkerMode.Initialization:
          Initialization();
          break;
        case ActionSelectDatabasesWorkerMode.AddingNew:
          AddingNew();
          break;
        case ActionSelectDatabasesWorkerMode.AddingExisting:
          AddingExisting();
          break;
        case ActionSelectDatabasesWorkerMode.Deleting:
          Deleting();
          break;
      }

      return ActionResult.Success;
    }

    #endregion

    private void Initialization()
    {
      // Получаем локальные сервера.
      string[] servers = Session.GetService<ISessionServerInfoExtension>().GetServerInfos().Select(v => v.Name).OrderBy(v => v).ToArray();

      // Обрабатываем каждый сервер.      
      foreach (string server in servers)
      {
        SqlWorkerBase worker;

        // Получаем разрешенные базы данных (по наличию специальной хранимой процедуры).
        ISqlGetDatabaseFromProcedureWorkerReturnedData databaseData;
        worker = factory.CreateSqlGetDatabaseFromProcedureWorker(server, AuthenticationType.Windows, "", "", sessionProp.Version, out databaseData);
        worker.Execute();
        // Если процедура существует, получаем базы данных.
        if (databaseData.ProcedureExist)
        {
          foreach (KeyValuePair<string, Tuple<string, bool>> db in databaseData.Databases)
          {
            Session.GetService<ISessionDatabaseInfoExtension>().AddDatabaseInfo(new DatabaseInfo
            {
              Server = server,
              Name = db.Key,
              Version = db.Value.Item1,
              IsRequiringUpdate = db.Value.Item2,
              IsNew = false
            });
          }
        }
      }
    }

    private void AddingNew()
    {
      // Создаем с общими свойствами.
      DatabaseInfo info = new DatabaseInfo 
      { 
        Server = sessionProp.SelectedServer,
        Name = sessionProp.NewDatabase,
        Version = sessionProp.Version,
        IsNew = true,
        IsRequiringUpdate = true
      };

      Session.GetService<ISessionDatabaseInfoExtension>().AddDatabaseInfo(info);
      sessionProp.NewDatabase = "";
    }

    private void AddingExisting()
    {
      // Для существующего добавления, информацию по базе данных получаем из списка существующих баз.      
      WixComboBox combo = new WixComboBox(Session, sessionProp.ExistControlProperty);
      string value = combo.SelectedValue;
      if (value != "")
      {
        CustomActionData customActionData = new CustomActionData(value);
        DatabaseInfo info = customActionData.GetObject<DatabaseInfo>(typeof(DatabaseInfo).ToString());
        // Затем данную позицию удаляем из списка сущестующих баз.
        combo.RemoveItem(combo.SelectedValue);

        // Значение по умолчанию.
        combo.SelectedValue = combo.Items.Count > 0 ? combo.Items[0].Value : "";

        // Полученную информацию добавляем в сессию.
        Session.GetService<ISessionDatabaseInfoExtension>().AddDatabaseInfo(info);
      }
    }

    private void Deleting()
    {
      string server;
      string database;
      // Получаем имена сервера и базы данных.
      NameViewConverter.ParseNameView(sessionProp.SelectedDatabase, out server, out database);

      // Получаем информацию об этой базе.
      DatabaseInfo info = Session.GetService<ISessionDatabaseInfoExtension>().GetDatabaseInfos().FirstOrDefault(v => v.Server == server && v.Name == database);

      // Если база данных существовала (т.е. не добавлена пользователем),
      // то запомним ее в специальном списке существующих баз данных (в элементе управления).
      // Так как это структура, то если ничего не будет найдено в запросе LINQ, поле Server не будет проинициализирован.
      if (!string.IsNullOrEmpty(info.Server) && !info.IsNew)
      {
        // Для вывода элемента будем использовать имя, версию и признак поддержки.
        // Для значения сохраним, в виде строки, сериализованный DatabaseInfo в CustomActionData.
        WixComboBox combo = new WixComboBox(Session, sessionProp.ExistControlProperty);
        // Здесь аккуратнее, так как ComboBox не отображает некоторые символы.
        string displayedName = sessionProp.SelectedDatabase + " " + info.Version + (info.IsRequiringUpdate ? "" : " (не поддерживается)");
        CustomActionData customActionData = new CustomActionData();
        customActionData.AddObject<DatabaseInfo>(typeof(DatabaseInfo).ToString(), info);
        combo.AddItem(new WixComboItem(displayedName, customActionData.ToString()));

        // Значение по умолчанию.
        combo.SelectedValue = combo.Items.Count > 0 ? combo.Items[0].Value : "";
      }

      Session.GetService<ISessionDatabaseInfoExtension>().DeleteDatabaseInfo(server, database);
    }    
  }
}

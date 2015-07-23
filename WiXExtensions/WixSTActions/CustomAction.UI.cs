using System;
using System.Reflection;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.ActionWorker;
using WixSTActions.SqlWorker;

namespace WixSTActions
{
  public partial class CustomAction
  {
    static ActionResult DispatcherUI(Session session, string methodName)
    {
      CustomActionProperties properties = new CustomActionProperties(session);
      SqlWorkersFactory factory = new SqlWorkersFactory();

      switch (methodName)
      {
        case "InstallerInitializeUI":
          //System.Diagnostics.Debugger.Launch();
          // Добавляем общие действия.
          CommonInitialization(session, properties);
          // Определяем сервера и путь к exe-файлам экземплярам серверов.
          ActionEngine.Instance.AddWorker(new ActionDefineSqlServerPathWorker(session, factory));
          // Определяем список установленных баз данных на различных серверах.
          ActionEngine.Instance.AddWorker(new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.Initialization, properties.SelectDatabases, factory));
          // Инициализируем компоненты.
          // Заполняем серверами.
          ActionEngine.Instance.AddWorker(new ActionServerUIControlWorker(session, properties.ServerUIControl));
          // Заполняем базами данных.
          ActionEngine.Instance.AddWorker(new ActionDatabaseUIControlWorker(session, properties.DatabaseUIControl));
          // Создание элемента управления на стандартной форме для доступа к файлу отчета.
          ActionEngine.Instance.AddWorker(new ActionWidgetCreaterWorker(session, properties.WidgetCreater));
          break;
        case "AddDatabaseToList":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.AddingNew, properties.SelectDatabases, factory));
          // Заново заполним компонент.
          ActionEngine.Instance.AddWorker(new ActionDatabaseUIControlWorker(session, properties.DatabaseUIControl));
          break;
        case "AddExistDatabaseToList":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.AddingExisting, properties.SelectDatabases, factory));
          // Заново заполним компонент.
          ActionEngine.Instance.AddWorker(new ActionDatabaseUIControlWorker(session, properties.DatabaseUIControl));
          break;
        case "DeleteDatabaseFromList":
          //System.Diagnostics.Debugger.Launch();
          // Удаляем из сессии информацию о базе данных.
          ActionEngine.Instance.AddWorker(new ActionSelectDatabasesWorker(session, ActionSelectDatabasesWorkerMode.Deleting, properties.SelectDatabases, factory));
          // Заново заполним компонент.
          ActionEngine.Instance.AddWorker(new ActionDatabaseUIControlWorker(session, properties.DatabaseUIControl));
          break;
        case "CheckConnectionDatabaseNotExist":
          //System.Diagnostics.Debugger.Launch();
          ActionEngine.Instance.AddWorker(new ActionCheckConnectionWorker(session, CheckConnectionType.DatabaseMustNotExist, properties.CheckConnection, factory));
          break;
        case "InitializationFinishInfo":
          //System.Diagnostics.Debugger.Launch();
          // Вывод сводной информации об установке
          ActionEngine.Instance.AddWorker(new ActionInitializationFinishInfoWorker(session, properties.InitializationFinishInfo, false));
          break;
        case "ClearFinishInfo":
          //System.Diagnostics.Debugger.Launch();
          // Очистка информации об установке
          ActionEngine.Instance.AddWorker(new ActionInitializationFinishInfoWorker(session, properties.InitializationFinishInfo, true));
          break;
      }

      return ActionEngine.Instance.Run();
    }

    /// <summary>
    /// Действие инициализация инсталлятора. 
    /// </summary>
    [CustomAction]
    public static ActionResult InstallerInitializeUI(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Добавление новой базы данных.
    /// </summary>
    [CustomAction]
    public static ActionResult AddDatabaseToList(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Добавление существующей базы данных.
    /// </summary>
    [CustomAction]
    public static ActionResult AddExistDatabaseToList(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Удаление базы данных из списка.
    /// </summary>
    [CustomAction]
    public static ActionResult DeleteDatabaseFromList(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    /// <summary>
    /// Метод проверки соединения для формы подключения к серверу баз данных.
    /// Проверяет корректность заполнения полей, подключение к серверу базы данных
    /// и отсутствие таблицы в базе данных.
    /// В случае успеха устанавливает в "1" свойство CONNECTIONSUCCESSFUL.
    /// В свойстве STRINGMESSAGE возвращается текстовое описание состояния.
    /// </summary>
    [CustomAction]
    public static ActionResult CheckConnectionDatabaseNotExist(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult InitializationFinishInfo(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }

    [CustomAction]
    public static ActionResult ClearFinishInfo(Session session)
    {
      return DispatcherUI(session, MethodBase.GetCurrentMethod().Name);
    }
  }
}

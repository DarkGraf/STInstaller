using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.Utils
{
  /* В данном файле реализован класс SessionExtension - расширение объекта Session на возможность получения
   * различных сервисов. Служба сервисов реализует интерфейс ISessionService и должна быть проиницилизирована
   * в статическом поле SessionExtension. Служба сервисов реализована по паттерну "Service Locator" */

  #region Расширение объекта Session, класс SessionExtension

  static class SessionExtension
  {
    static ISessionService sessionService;

    public static T GetService<T>(this Session session)
    {
      // По умолчанию инициализируем сервис сессий.
      if (sessionService == null)
        sessionService = new SessionService(session);

      return sessionService.GetService<T>();
    }

    public static void SetDefaultSessionService(this Session session, ISessionService service)
    {
      sessionService = service;
    }
  }

  #endregion

  #region Паттерн Service Locator.

  interface ISessionService
  {
    T GetService<T>();
  }

  class SessionService : ISessionService
  {
    private IDictionary<Type, object> services;

    public SessionService(Session session)
    {
      services = new Dictionary<Type, object>();
      services.Add(typeof(ISessionInstallPhaseExtension), new SessionInstallPhaseExtension(session, this));
      services.Add(typeof(ISessionCustomActionNameGetterExtension), new SessionCustomActionNameGetterExtension(session, this));
      services.Add(typeof(ISessionSerializeCustomActionDataExtension), new SessionSerializeCustomActionDataExtension(session, this));
      services.Add(typeof(ISessionUITypeGetterExtension), new SessionUITypeGetterExtension(session, this));
      services.Add(typeof(ISessionCurrentInstallStatusExtension), new SessionCurrentInstallStatusExtension(session, this));
      services.Add(typeof(ISessionComponentInstallStatusExtension), new SessionComponentInstallStatusExtension(session, this));
      services.Add(typeof(ISessionDataTableExtension), new SessionDataTableExtension(session, this));
      services.Add(typeof(ISessionDatabaseInfoExtension), new SessionDatabaseInfoExtension(session, this));
      services.Add(typeof(ISessionServerInfoExtension), new SessioServerInfoExtension(session, this));
      services.Add(typeof(ISessionTempDirectoryExtension), new SessionTempDirectoryExtension(session, this));
      services.Add(typeof(ISessionInstallReportExtension), new SessionInstallReportExtension(session, this));
    }

    public T GetService<T>()
    {
      Type type = typeof(T);
      try
      {
        return (T)services[type];
      }
      catch (KeyNotFoundException)
      {
        throw new ApplicationException("The requested service is not registered.");
      }
    }
  }

  #endregion

  #region SessionBaseExtension

  class SessionBaseExtension
  {
    protected Session Session { get; private set; }
    protected ISessionService Service { get; private set; }

    public SessionBaseExtension(Session session, ISessionService service)
    {
      Session = session;
      Service = service;
    }
  }

  #endregion

  #region ISessionInstallPhaseExtension

  /// <summary>
  /// Фаза выполнения.
  /// </summary>
  public enum InstallPhase
  {
    /// <summary>
    /// Фаза Client Side.
    /// </summary>
    UISequence,
    /// <summary>
    /// Немедленное выполнение (Server Side).
    /// </summary>
    Immediate,
    /// <summary>
    /// Отложенное выполнение (Server Side).
    /// </summary>
    Deferred,
    /// <summary>
    /// Отмена действия (Server Side).
    /// </summary>
    Rollback
  }

  interface ISessionInstallPhaseExtension
  {
    /// <summary>
    /// Возвращает фазу выполнения.
    /// </summary>
    InstallPhase GetInstallPhase();
  }

  /// <summary>
  /// Расширение для получения фазы выполнения.
  /// </summary>
  class SessionInstallPhaseExtension : SessionBaseExtension, ISessionInstallPhaseExtension
  {
    public SessionInstallPhaseExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Возвращает фазу выполнения.
    /// </summary>
    public InstallPhase GetInstallPhase()
    {
      if (Session.GetMode(InstallRunMode.Scheduled))
        return InstallPhase.Deferred;
      else if (Session.GetMode(InstallRunMode.Rollback))
        return InstallPhase.Rollback;
      else if (Session[ServerSideKey] != string.Empty) 
        return InstallPhase.Immediate;
      else
        return InstallPhase.UISequence;
    }

    // Свойство SERVERSIDE устанавливается в XML через 
    // <SetProperty Id="SERVERSIDE" Value="1" Before="AppSearch" Sequence="execute" />.
    internal static string ServerSideKey = "SERVERSIDE";
  }

  #endregion 

  #region ISessionCustomActionNameGetterExtension

  interface ISessionCustomActionNameGetterExtension
  {
    /// <summary>
    /// Полуечение имени самого последнего метода в стеке с аттрибутом CustomAction.
    /// </summary>
    string GetCustomActionName();
  }

  /// <summary>
  /// Расширение получения имени самого последнего метода в стеке с аттрибутом CustomAction.
  /// </summary>
  class SessionCustomActionNameGetterExtension : SessionBaseExtension, ISessionCustomActionNameGetterExtension
  {
    public SessionCustomActionNameGetterExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Получение имени самого последнего метода в стеке с аттрибутом CustomAction.
    /// </summary>
    public string GetCustomActionName()
    {
      string methodName = null;
      StackTrace stack = new StackTrace();
      for (int i = 0; i < stack.FrameCount; i++)
      {
        MethodBase method = stack.GetFrame(i).GetMethod();
        if (method.GetCustomAttribute<CustomActionAttribute>() != null)
        {
          methodName = method.Name;
        }
      }

      return methodName;
    }
  }

  #endregion

  #region ISessionSerializeCustomActionDataExtension

  /// <summary>
  /// Глобальное хранилище в сесиии.
  /// </summary>
  public class GlobalStore
  {
    /// <summary>
    /// Признак, включено ли глобальное хранилище.
    /// </summary>
    public bool Enable { get; set; }
    /// <summary>
    /// Хранение объекта CustomActionData в строковом формате.
    /// Внутри этого объекта содержаться аналогичные объекты (в строковом формате) с ключом по имени подписчика.
    /// Они в свою очередь содержат опять же объект этого класса сериализованный уже с реальными данными.
    /// </summary>
    public string Data { get; set; }
  }

  interface ISessionSerializeCustomActionDataExtension
  {
    /// <summary>
    /// Сериализует пользовательские данные под определенным ключом и добавляет их заданным подписчикам.
    /// </summary>
    void SerializeCustomActionData<T>(T data, string key, string[] subscribers);

    /// <summary>
    /// Сериализует пользовательские данные под определенным ключом и добавляет 
    /// их всем методам с атрибутом CustomAction выполняемой сборки.
    /// </summary>
    void SerializeCustomActionData<T>(T data, string key);

    /// <summary>
    /// Сериализует пользовательские данные определенного типа и добавляет их для заданных подписчиков.
    /// </summary>
    void SerializeCustomActionData<T>(T data, string[] subscribers);

    /// <summary>
    /// Сериализует пользовательские данные определенного типа и добавляет их всем подписчикам.
    /// </summary>
    void SerializeCustomActionData<T>(T data);

    /// <summary>
    /// Десериализует пользовательские данные из текущего объекта с определенным ключом.
    /// </summary>
    T DeserializeCustomActionData<T>(string key);

    /// <summary>
    /// Десериализует пользовательские данные из текущего объекта.
    /// </summary>
    T DeserializeCustomActionData<T>();
  }

  /// <summary>
  /// Расширение объекта Session для сериализации данных для заданных обработчиков.
  /// </summary>
  class SessionSerializeCustomActionDataExtension : SessionBaseExtension, ISessionSerializeCustomActionDataExtension
  {
    // Хранение информации в сессии зависит от следующих факторов.
    // Режим работы инсталлятора делится на два основных режима:
    // "client side" - работа с пользователем и "server side" - работа
    // самого инсталлятора. Созданные свойства в сессии с именем методов
    // не сохраняются между режимами, с другими именами сохраняются.
    // В свою очередь "server side" делится на три фазы: "immediate",
    // "deferred" и "rollback". В двух последних режимах считывать свойства
    // с сессии запрещено, к ним можно получить доступ только через session.CustomActionData.

    public SessionSerializeCustomActionDataExtension(Session session, ISessionService service) : base(session, service) { }

    internal static string GlobalKey = "GLOBALSTORE"; // Глобальная переменная сессии (большими буквами).

    /// <summary>
    /// Возвращает все методы с аттрибутом CustomAction в текущей исполняемой сборке.
    /// </summary>
    internal string[] GetMethods()
    {
      // По всем типам выбираем методы с атрибутом CustomAction.
      IEnumerable<string> methods = Assembly.GetExecutingAssembly().GetExportedTypes().SelectMany(t =>
        t.GetMethods().Where(m => m.GetCustomAttribute<CustomActionAttribute>() != null).Select(m => m.Name));

      return methods.ToArray();
    }

    /// <summary>
    /// Корректирует имя, заменяя все символы, кроме букв и цифр, на их ASCII коды.    
    /// </summary>
    private string FormatingName(string name)
    {
      // Необходимо для метода CustomActionData.AddObject -> Add -> ValidateKey (там делается проверка на символы ключа).
      for (int i = 0; i < name.Length; i++)
      {
        if (!char.IsLetterOrDigit(name[i]))
          name = name.Replace(name[i].ToString(), ((int)name[i]).ToString());
      }
      return name;
    }

    /// <summary>
    /// Если глобальное хранилище не проинициализировано, создаем его.
    /// </summary>
    private GlobalStore LoadGlobalStore(Session session)
    {
      GlobalStore store;

      if (session[GlobalKey] == "")
        store = new GlobalStore { Enable = true, Data = string.Empty };
      else
      {
        CustomActionData data = new CustomActionData(session[GlobalKey]);
        store = data.GetObject<GlobalStore>(typeof(GlobalStore).ToString()); ;
      }
      return store;
    }

    private void SaveGlobalStore(Session session, GlobalStore store)
    {
      CustomActionData data = new CustomActionData();
      data.AddObject<GlobalStore>(typeof(GlobalStore).ToString(), store);
      session[GlobalKey] = data.ToString();
    }

    /// <summary>
    /// Перенос данных из глобального хранилища сессии в индивидуальные хранилища для каждого метода server side.
    /// Изменения признака активности глобального хранилища.
    /// </summary>
    private void TransferDataFromGlobalStorageToSession(Session session)
    {
      GlobalStore store = LoadGlobalStore(session);
      if (store.Enable)
      {
        CustomActionData subscribersData = new CustomActionData(store.Data);
        foreach (string subscriber in subscribersData.Keys)
        {
          session[subscriber] = subscribersData[subscriber];
        }

        // Очищаем локальное хранилище.
        store.Enable = false;
        store.Data = string.Empty;
        SaveGlobalStore(session, store);
      }
    }

    #region ISessionSerializeCustomActionDataExtension

    /// <summary>
    /// Сериализует пользовательские данные под определенным ключом и добавляет их заданным подписчикам.
    /// </summary>
    public void SerializeCustomActionData<T>(T data, string key, string[] subscribers)
    {
      if (subscribers == null)
        return;

      // Смотрим режим, если это client side то работаем с глобальным хранилищем,
      // если это server side, то работаем с сессией.
      switch (Service.GetService<ISessionInstallPhaseExtension>().GetInstallPhase())
      {
        case InstallPhase.UISequence:
          // Из сессии получаем строку по ключу globalKey.
          // Из строки получаем объект CustomActionData, из него получаем объект GlobalStore (строка -> CustomActionData -> GlobalStore).
          // GlobalStore.Data это строка, из нее получаем CustomActionData (GlobalStore.Data -> CustomActionData).
          // В полученном объекте CustomActionData по ключу (имя подписчика) получаем строку, из нее CustomActionData.
          // В данном CustomActionData содержатся данные для подписчика.
          GlobalStore store = LoadGlobalStore(Session); // Получаем уровень 0, глобальный
          if (store.Enable)
          {
            CustomActionData subscribersData = new CustomActionData(store.Data); // Уровень 1, подписчики.

            // Для каждого подписчика в store.Data хранится CustomActionData в строковом формате.
            foreach (string subscriber in subscribers)
            {
              CustomActionData subscriberData;
              if (subscribersData.ContainsKey(subscriber)) // Уровень 2, множество данных подписчика.
              {
                subscriberData = new CustomActionData(subscribersData.GetObject<string>(subscriber));
                subscribersData.Remove(subscriber);
              }
              else
                subscriberData = new CustomActionData();

              // Если данные с ключем есть, то удаляем.
              if (subscriberData.ContainsKey(key)) // Уровень 3, конкретные данные подписчика.
                subscriberData.Remove(key);
              // Добавляем данные.
              subscriberData.AddObject<T>(key, data);

              // Сохраняем для подписчика.
              subscribersData.AddObject<string>(subscriber, subscriberData.ToString()); // Уровень 2.
            }

            // Сохраняем глобальные данные.
            store.Data = subscribersData.ToString(); // Уровень 1.
            SaveGlobalStore(Session, store); // Уровень 0.
          }
          break;
        case InstallPhase.Immediate:
          TransferDataFromGlobalStorageToSession(Session);

          foreach (string subscriber in subscribers)
          {
            // Для каждого подписчика сохраняем его данные и добавляем новые.
            CustomActionData customData = new CustomActionData(Session[subscriber]);
            // Если данные с ключем есть, то удаляем.
            if (customData.ContainsKey(key))
              customData.Remove(key);
            // Добавляем данные.
            customData.AddObject<T>(key, data);
            Session[subscriber] = customData.ToString();
          }
          break;
      }
    }    

    /// <summary>
    /// Сериализует пользовательские данные под определенным ключом и добавляет 
    /// их всем методам с атрибутом CustomAction выполняемой сборки.
    /// </summary>
    public void SerializeCustomActionData<T>(T data, string key)
    {
      string[] methods = GetMethods();
      SerializeCustomActionData<T>(data, key, methods);
    }

    /// <summary>
    /// Сериализует пользовательские данные определенного типа и добавляет их для заданных подписчиков.
    /// </summary>
    public void SerializeCustomActionData<T>(T data, string[] subscribers)
    {
      string key = FormatingName(typeof(T).Name);
      SerializeCustomActionData(data, key, subscribers);
    }

    /// <summary>
    /// Сериализует пользовательские данные определенного типа и добавляет их всем подписчикам.
    /// </summary>
    public void SerializeCustomActionData<T>(T data)
    {
      string key = FormatingName(typeof(T).Name);
      string[] methods = GetMethods();
      SerializeCustomActionData(data, key, methods);
    }

    /// <summary>
    /// Десериализует пользовательские данные из текущего объекта с определенным ключом.
    /// </summary>
    public T DeserializeCustomActionData<T>(string key)
    {
      T data = default(T);
      CustomActionData customData;

      // Для фазы Immediate данные не передаются через CustomActionData, поэтому их необходимо получать напрямую.
      // Узнаем имя вызванного действия.
      string customActionName = Service.GetService<ISessionCustomActionNameGetterExtension>().GetCustomActionName();
      switch (Service.GetService<ISessionInstallPhaseExtension>().GetInstallPhase())
      {
        case InstallPhase.UISequence:
          // Смотрим активность глобального хранилища.
          GlobalStore store = LoadGlobalStore(Session);
          if (store.Enable)
          {
            CustomActionData subscribersData = new CustomActionData(store.Data);
            customData = subscribersData.ContainsKey(customActionName) ?
              new CustomActionData(subscribersData.GetObject<string>(customActionName)) : new CustomActionData();
          }
          else
            throw new InvalidOperationException("Глобальный кеш отключен в режиме Client Side");
          break;
        case InstallPhase.Immediate:
          TransferDataFromGlobalStorageToSession(Session);
          customData = new CustomActionData(Session[customActionName]);
          break;
        default:
          customData = Session.CustomActionData;
          break;
      }

      if (customData.ContainsKey(key))
        data = customData.GetObject<T>(key);
      return data;
    }

    /// <summary>
    /// Десериализует пользовательские данные из текущего объекта.
    /// </summary>
    public T DeserializeCustomActionData<T>()
    {
      string key = FormatingName(typeof(T).Name);
      return DeserializeCustomActionData<T>(key);
    }

    #endregion
  }

  #endregion

  #region ISessionUITypeGetterExtension

  /// <summary>
  /// Тип установки.
  /// Имена должны строго соответствовать возможным значениям XML-атрибута "Type" XML-элемента "UIType".
  /// </summary>
  [Flags]
  public enum UIType
  {
    Unknow = 0,
    Client = 1,
    Server = 2
  }

  interface ISessionUITypeGetterExtension
  {
    /// <summary>
    /// Получение типа установки (возможены: клиентский и серверный типы).
    /// </summary>
    UIType GetUIType();
    /// <summary>
    /// Сохранение типа установки (возможены: клиентский и серверный типы) в пользовательских данных.
    /// </summary>
    void SetUIType(UIType type);
  }

  /// <summary>
  /// Расширение для работы с типом установки (возможены: клиентский и серверный типы) в пользовательских данных.
  /// </summary>
  class SessionUITypeGetterExtension : SessionBaseExtension, ISessionUITypeGetterExtension
  {
    public SessionUITypeGetterExtension(Session session, ISessionService service) : base(session, service) { }

    internal const string keyUIType = "UIType";

    /// <summary>
    /// Получение типа установки (возможены: клиентский и серверный типы).
    /// </summary>
    public UIType GetUIType()
    {
      return Service.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<UIType>(keyUIType);
    }

    /// <summary>
    /// Сохранение типа установки (возможены: клиентский и серверный типы) в пользовательских данных.
    /// </summary>
    public void SetUIType(UIType type)
    {
      // Сохраняем всем методам.
      Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData<UIType>(type, keyUIType);
    }
  }

  #endregion

  #region ISessionCurrentInstallStatusExtension, ISessionComponentInstallStatusExtension

  /// <summary>
  /// Определение текущего статуса установки.
  /// </summary>
  interface ISessionCurrentInstallStatusExtension
  {
    CurrentInstallStatus GetStatus();
  }

  /// <summary>
  /// Определение статуса установки компонента.
  /// </summary>
  interface ISessionComponentInstallStatusExtension
  {
    ComponentInstallStatus GetStatus(string componentName);
  }

  /// <summary>
  /// Определение текущего статуса установки.
  /// </summary>
  class SessionCurrentInstallStatusExtension : SessionBaseExtension, ISessionCurrentInstallStatusExtension
  {
    public SessionCurrentInstallStatusExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Определение текущего статуса установки.
    /// </summary>
    public CurrentInstallStatus GetStatus()
    {
      InstallStatusDefinerBase<CurrentInstallStatus> definer = new CurrentInstallStatusDefiner(Session);
      return definer.Status;
    }
  }

  /// <summary>
  /// Определение статуса установки компонента.
  /// </summary>
  class SessionComponentInstallStatusExtension : SessionBaseExtension, ISessionComponentInstallStatusExtension
  {
    public SessionComponentInstallStatusExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Определение статуса установки компонента.
    /// </summary>
    public ComponentInstallStatus GetStatus(string componentName)
    {
      InstallStatusDefinerBase<ComponentInstallStatus> definer = new ComponentInstallStatusDefiner(Session, componentName);
      return definer.Status;
    }
  }

  #endregion

  #region ISessionDataTableExtension

  interface ISessionDataTableExtension
  { 
    /// <summary>
    /// Получение копии объекта TableInfo в объекте DataTable.
    /// Если объекта TableInfo не существует, возвращается null.
    /// </summary>
    DataTable CopyTableInfoToDataTable(string name);
  }

  /// <summary>
  /// Расширение получения копии объекта TableInfo в объекте DataTable.
  /// </summary>
  class SessionDataTableExtension : SessionBaseExtension, ISessionDataTableExtension
  {
    public SessionDataTableExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Получение копии объекта TableInfo в объекте DataTable.
    /// Если объекта TableInfo не существует, возвращается null.
    /// </summary>
    public DataTable CopyTableInfoToDataTable(string name)
    {
      TableInfo tableInfo = Session.Database.Tables[name];

      if (tableInfo == null)
        return null;

      // Копируем схему таблицы.
      DataTable dataTable = new DataTable();
      foreach (ColumnInfo ci in tableInfo.Columns)
      {
        // Если поток, то вместо него будем сохранять бинарные данные.
        if (ci.Type == typeof(Stream) || ci.Type.IsSubclassOf(typeof(Stream)))
        {
          dataTable.Columns.Add(new DataColumn(ci.Name, typeof(byte[])));
        }
        else
          dataTable.Columns.Add(new DataColumn(ci.Name, ci.Type));
      }

      // Копируем данные.
      using (View view = Session.Database.OpenView(tableInfo.SqlSelectString))
      {
        view.Execute();
        Record record;
        while ((record = view.Fetch()) != null)
        {
          DataRow row = dataTable.NewRow();
          foreach (DataColumn c in dataTable.Columns)
          {
            // Если поле пустое, пропускаем.
            if (record[c.ColumnName] == null)
            {
              row[c] = DBNull.Value;
              continue;
            }

            // Если поток, то копируем бинарные данные.
            if (record[c.ColumnName].GetType().BaseType == typeof(Stream) || record[c.ColumnName].GetType().IsSubclassOf(typeof(Stream)))
            {
              using (MemoryStream memoryStream = new MemoryStream())
              {
                ((Stream)record[c.ColumnName]).CopyTo(memoryStream);
                row[c.ColumnName] = memoryStream.ToArray();
              }
            }
            // Если поле расчетное, то получаем его значение, иначе копируем.
            else if (record[c.ColumnName] is string && ((string)record[c.ColumnName]).Contains('[') && ((string)record[c.ColumnName]).Contains(']'))
            {
              row[c.ColumnName] = GetComplexCalcField((string)record[c.ColumnName]);
            }
            else
              row[c.ColumnName] = record[c.ColumnName];
          }
          dataTable.Rows.Add(row);
          record.Dispose();
        }
      }

      return dataTable;
    }

    /// <summary>
    /// Получение значения из сложного свойства.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    object GetComplexCalcField(string field)
    {
      // Сложное свойство может содержать сколько угодно расчетных полей
      // вида [NAME] смешанное с обычным текстом.
      // Находим первое поле, вычисляем его, подставляем значение вместо
      // него и повторяем все заново.
      int begin;
      int end;

      while ((begin = field.IndexOf('[')) >= 0 && (end = field.IndexOf(']')) >= 0 && begin < end)
      {
        string str = field.Substring(begin, end - begin + 1);
        string val = Session[str.Substring(1, str.Length - 2)];
        field = field.Replace(str, val);
      }

      return field;
    }
  }

  #endregion

  #region ISessionDаtabaseInfoExtension

  /// <summary>
  /// Информация о базе данных для сериализациии.
  /// </summary>
  public struct DatabaseInfo
  {
    /// <summary>
    /// Имя сервера (экземпляр).
    /// </summary>
    public string Server { get; set; }
    /// <summary>
    /// Имя базы данных.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Признак что база данных нуждается в обновлении.
    /// </summary>
    public bool IsRequiringUpdate { get; set; }
    /// <summary>
    /// Признак что база данных создается.
    /// </summary>
    public bool IsNew { get; set; }
    /// <summary>
    /// Версия базы данных.
    /// </summary>
    public string Version { get; set; }
  }

  interface ISessionDatabaseInfoExtension
  {
    /// <summary>
    /// Добавляет информацию о базе данных в сессию.
    /// </summary>
    void AddDatabaseInfo(DatabaseInfo info);
    /// <summary>
    /// Возвращает массив информаций о баз данных из сессий.
    /// </summary>
    DatabaseInfo[] GetDatabaseInfos();
    /// <summary>
    /// Удаляет информацию о базе данных из сессии.
    /// </summary>
    void DeleteDatabaseInfo(string server, string name);
  }

  /// <summary>
  /// Расширение для работы с информацией о базах данных.
  /// </summary>
  class SessionDatabaseInfoExtension : SessionBaseExtension, ISessionDatabaseInfoExtension
  {
    public SessionDatabaseInfoExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Добавляет информацию о базе данных в сессию.
    /// </summary>
    public void AddDatabaseInfo(DatabaseInfo info)
    {
      DatabaseInfo[] dbs = GetDatabaseInfos();
      if (dbs.Where(v => v.Server == info.Server && v.Name == info.Name).Count() == 0)
      {
        Array.Resize(ref dbs, dbs.Length + 1);
        dbs[dbs.Length - 1] = info;
        Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData(dbs);
      }
    }

    /// <summary>
    /// Возвращает массив информаций о баз данных из сессий.
    /// </summary>
    public DatabaseInfo[] GetDatabaseInfos()
    {
      DatabaseInfo[] dbs = Service.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<DatabaseInfo[]>();
      if (dbs == null)
        dbs = new DatabaseInfo[0];
      return dbs;
    }

    /// <summary>
    /// Удаляет информацию о базе данных из сессии.
    /// </summary>
    public void DeleteDatabaseInfo(string server, string name)
    {
      DatabaseInfo[] dbs = GetDatabaseInfos();
      dbs = dbs.Where(v => v.Server != server || v.Name != name).ToArray();
      Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData(dbs);
    }
  }

  #endregion

  #region ISessionServerInfoExtension

  /// <summary>
  ///  Информация о экземпляре сервера баз данных.
  /// </summary>
  public struct ServerInfo
  {
    /// <summary>
    /// Имя сервера (экземпляр).
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Путь к exe-файлу.
    /// </summary>
    public string Path { get; set; }
  }

  interface ISessionServerInfoExtension
  {
    /// <summary>
    /// Добавляет информацию о сервере базы данных в сессию.
    /// </summary>
    void AddServerInfo(ServerInfo info);
    /// <summary>
    /// Возвращает массив информаций о сервере баз данных из сессий.
    /// </summary>
    ServerInfo[] GetServerInfos();
  }

  /// <summary>
  /// Расширение для работы с информацией о сервере баз данных.
  /// </summary>
  // Реализовано аналогично SessionDаtabaseInfoExtension.
  class SessioServerInfoExtension : SessionBaseExtension, ISessionServerInfoExtension
  {
    public SessioServerInfoExtension(Session session, SessionService service) : base(session, service) { }

    /// <summary>
    /// Добавляет информацию о сервере базы данных в сессию.
    /// </summary>
    public void AddServerInfo(ServerInfo info)
    {
      ServerInfo[] servers = GetServerInfos();
      Array.Resize(ref servers, servers.Length + 1);
      servers[servers.Length - 1] = info;
      Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData(servers);
    }

    /// <summary>
    /// Возвращает массив информаций о сервере баз данных из сессий.
    /// </summary>
    public ServerInfo[] GetServerInfos()
    {
      ServerInfo[] servers = Service.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<ServerInfo[]>();
      if (servers == null)
        servers = new ServerInfo[0];
      return servers;
    }
  }

  #endregion

  #region ISessionTempDirectoryExtension

  interface ISessionTempDirectoryExtension
  {
    /// <summary>
    /// Создает временную директорию и сохраняет данные в CustomActionData для всех обработчиков и в текущей сессии.
    /// </summary>
    void CreateTempDirectory();
    /// <summary>
    /// Возвращает временную директорию. Если директория не создана или удалена, возвращает null.
    /// </summary>
    string GetTempDirectory();
    /// <summary>
    /// Удаляет временную директорию.
    /// </summary>
    void DeleteTempDirectory();
  }

  /// <summary>
  /// Расширение объекта Session для работы с временной директорией.
  /// Создать директорию можно только один раз в фазе Immediate.
  /// </summary>
  class SessionTempDirectoryExtension : SessionBaseExtension, ISessionTempDirectoryExtension
  {
    public SessionTempDirectoryExtension(Session session, ISessionService service) : base(session, service) { }

    /// <summary>
    /// Исключение, что директория была уже создана.
    /// </summary>
    public class TempDirectoryAlreadyCreatedException : Exception { }

    /// <summary>
    /// Исключение генерирующееся при неправильном режиме.
    /// </summary>
    public class InvalidInstallRunModeException : Exception { }

    internal const string TempDirectoryName = "TempDirectoryName";

    /// <summary>
    /// Создает временную директорию и сохраняет данные в CustomActionData для всех обработчиков и в текущей сессии.
    /// </summary>
    public void CreateTempDirectory()
    {
      // Проверяем режим.
      if (Session.GetMode(InstallRunMode.Scheduled) || Session.GetMode(InstallRunMode.Rollback))
        throw new InvalidInstallRunModeException();

      // Проверяем, создавали ли уже директорию.
      if (!string.IsNullOrEmpty(GetTempDirectoryFromData()))
        throw new TempDirectoryAlreadyCreatedException();

      // Создаем временную директорию.
      string path = Path.Combine(Path.GetTempPath(), "ST" + Path.GetRandomFileName());
      Directory.CreateDirectory(path);

      // Передаем данные всем методам.
      Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData<string>(path, TempDirectoryName);
    }

    /// <summary>
    /// Возвращает временную директорию. Если директория не создана или удалена, возвращает null.
    /// </summary>
    public string GetTempDirectory()
    {
      string path = GetTempDirectoryFromData();
      return !string.IsNullOrEmpty(path) && Directory.Exists(path) ? path : null;
    }

    /// <summary>
    /// Удаляет временную директорию.
    /// </summary>
    public void DeleteTempDirectory()
    {
      string path = GetTempDirectoryFromData();
      if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        Directory.Delete(path, true);
    }

    /// <summary>
    /// Возвращает временную директорию. Если директория не создана, возвращает null.
    /// </summary>
    private string GetTempDirectoryFromData()
    {
      string directory = Service.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<string>(TempDirectoryName);
      return directory;
    }
  }

  #endregion

  #region ISessionInstallReportExtension

  enum InstallReportInfoType { Header1, Header2, Description }

  interface ISessionInstallReportExtension
  {
    /// <summary>
    /// Добавить сообщение об ошибке.
    /// </summary>
    /// <param name="info"></param>
    void AddInfo(string info, InstallReportInfoType type);
    /// <summary>
    /// Возвращает путь к файлу ошибок.
    /// </summary>
    string PathToFile { get; }
  }  

  class SessionInstallReportExtension : SessionBaseExtension, ISessionInstallReportExtension
  {
    const string pathKey = "InstallReportFile";

    public SessionInstallReportExtension(Session session, ISessionService service) : base(session, service) { }

    public void AddInfo(string info, InstallReportInfoType type)
    {
      HtmlBuilder builder = new HtmlBuilder(PathToFile);

      switch (type)
      {
        case InstallReportInfoType.Header1:
          builder.AddH1(info);
          break;
        case InstallReportInfoType.Header2:
          builder.AddH2(info);
          break;
        case InstallReportInfoType.Description:
          builder.AddP(info);
          break;
      }
      builder.Save(PathToFile);
    }

    public string PathToFile
    {
      get 
      {
        string path = GetPathToFile();

        // Если файла не существует, создаем.
        if (!File.Exists(path))
        {
          HtmlBuilder builder = new HtmlBuilder();
          builder.Save(path);
        }

        return path; 
      }
    }

    private string GetPathToFile()
    {
      string path = Service.GetService<ISessionSerializeCustomActionDataExtension>().DeserializeCustomActionData<string>(pathKey);
      if (string.IsNullOrEmpty(path))
      {
        path = Path.Combine(Path.GetTempPath(), string.Format("InstallReport{0}.html", DateTime.Now.ToString("yyyyMMddHHmmss")));
        Service.GetService<ISessionSerializeCustomActionDataExtension>().SerializeCustomActionData<string>(path, pathKey);
      }
      return path;
    }

    //создание html
  }

  #endregion
}
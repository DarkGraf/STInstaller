using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace InstallerStudio.Utils
{
  /* Здесь описаны классы-атрибуты описывающие типы проверки введенной пользователем информации,
   * а также класс DataErrorHandler, реализующий интерфейс IDataErrorHandler, выполняющий проверку.
   * Классы атрибуты должны быть самодостаточные, т. е. их функциональность не должна зависить
   * от других классов. Таким образом они реализуют "легкую" проверку, не зависящую от проверяемой
   * бизнес-модели. "Тяжелую" проверку должна реализовать сама модель. */

  interface IDataErrorHandler
  {
    /// <summary>
    /// Проверяет свойство по имени у объекта.
    /// </summary>
    string Check(string propertyName);
    /// <summary>
    /// Возвращает список всех ошибок.
    /// Ошибки фиксируются только у тех свойств, для
    /// которых был вызван метод Check().
    /// </summary>
    string Error { get; }
  }

  #region Атрибуты.

  [AttributeUsage(AttributeTargets.Property)]
  abstract class CheckingBaseAttribute : Attribute 
  {
    /// <summary>
    /// Ошибка выводимая пользователю.
    /// </summary>
    protected string Error { get; private set; }
    /// <summary>
    /// Проверяемый объект.
    /// </summary>
    protected object Obj { get; private set; }
    /// <summary>
    /// Имя проверяемого свойства.
    /// </summary>
    protected string PropertyName { get; private set; }

    public CheckingBaseAttribute(string error)
    {
      Error = error;
    }

    /// <summary>
    /// Установка проверяемого объекта и проверяемого свойства.
    /// Метод необходим для повышения быстродействия, для выполнения 
    /// поиска необходимой информации с помощью рефлексии при инициализации.
    /// Здесь необходимо проводить как можно больше операций, освобождая от
    /// них метод Check().
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    public void Initialize(object obj, string propertyName)
    {
      Obj = obj;
      PropertyName = propertyName;

      InternalInitialize();
    }

    /// <summary>
    /// Проверка атрибута.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <param name="obj">Проверяемый объект.</param>
    /// <returns></returns>
    public abstract string Check();

    protected abstract void InternalInitialize();
  }

  /// <summary>
  /// Атрибут требующий не пустое значение.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  class CheckingRequiredAttribute : CheckingBaseAttribute
  {
    internal const string DefaultError = "\"{0}\" не должен быть пустым.";

    private PropertyInfo propertyInfo;
    private string error;

    public CheckingRequiredAttribute() : base(DefaultError) { }

    public CheckingRequiredAttribute(string error) : base(error) { }

    #region CheckingBaseAttribute

    public override string Check()
    {
      object value = propertyInfo.GetValue(Obj);

      bool isError = value == null;

      // Частный случай для строк.
      if (!isError && value is string)
        isError = (string)value == string.Empty;

      return isError ? error : string.Empty;
    }

    protected override void InternalInitialize()
    {
      propertyInfo = Obj.GetType().GetProperty(PropertyName);
      error = string.Format(Error, PropertyName);
    }

    #endregion
  }

  /// <summary>
  /// Атрибут требующий выбор хотя бы одного значения из группы.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  class CheckingFromGroupAttribute : CheckingBaseAttribute
  {
    internal const string DefaultError = "Необходимо выбрать хотя бы один элемент из группы.";

    readonly string grp;

    /// <summary>
    /// Словарь свойств с заданной группой и значением по умолчанию данного свойства.
    /// </summary>
    Dictionary<PropertyInfo, object> properties;

    public CheckingFromGroupAttribute(string group) : this(DefaultError, group) { }

    public CheckingFromGroupAttribute(string error, string group) : base(error)
    {
      this.grp = group;
    }

    #region CheckingBaseAttribute

    public override string Check()
    {
      string error = Error;

      // Проверим все свойства. Если хотя бы одно из них
      // не равно значению по умолчанию, то все в порядке,
      // иначе ошибка.
      foreach (KeyValuePair<PropertyInfo, object> pair in properties)
      {
        bool ok;
        object def = pair.Value;

        if (pair.Key.PropertyType.IsValueType)
        {
          ok = !pair.Key.GetValue(Obj).Equals(def);
        }
        else
        {
          // Для ссылочных типов, просто проверим на null.
          ok = pair.Key.GetValue(Obj) != null;
          // Частный случай для string.
          if (ok && pair.Key.PropertyType == typeof(string))
            ok = !string.IsNullOrEmpty((string)pair.Key.GetValue(Obj));
        }

        if (ok)
        {
          error = string.Empty;
          break;
        }
      }

      return error;
    }

    protected override void InternalInitialize()
    {
      properties = new Dictionary<PropertyInfo, object>();

      // Получаем все свойства с данным атрибутом и текущей группой.
      var props = from prop in Obj.GetType().GetProperties()
                  let attr = prop.GetCustomAttribute<CheckingFromGroupAttribute>()
                  where attr != null && (attr.grp == grp)
                  select prop;

      // Заполним словарь свойствами и их значениями по умолчанию.
      foreach (PropertyInfo prop in props)
      {
        // Для проверки типов значения будем создавать тестовый объект, 
        // для проверки значения по умолчанию. Объект создаем для каждого 
        // свойства, так как типы свойств в группе могут быть 
        // разными (теоретически).
        // Для ссылочных типов, просто null.
        object def = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
        properties.Add(prop, def);
      }
    }

    #endregion
  }

  #endregion

  class DataErrorHandler : IDataErrorHandler
  {
    object obj;
    /// <summary>
    /// Словарь свойств и соответствующих им атрибутов.
    /// </summary>
    Dictionary<string, CheckingBaseAttribute[]> dictionary;
    /// <summary>
    /// Словарь свойств и последних ошибок.
    /// </summary>
    Dictionary<string, string> allErrors;

    /// <summary>
    /// Создает экземпляр класса.
    /// </summary>
    /// <param name="obj">Проверяемый объект.</param>
    public DataErrorHandler(object obj)
    {
      this.obj = obj;

      dictionary = new Dictionary<string, CheckingBaseAttribute[]>();
      allErrors = new Dictionary<string, string>();

      // Найдем открытые свойства класса.
      PropertyInfo[] propertyes = obj.GetType().GetProperties();
      // Запомним свойства, у которых есть атрибут типа CheckingBaseAttribute.
      foreach (PropertyInfo property in propertyes)
      {
        IEnumerable<CheckingBaseAttribute> attrs = property.GetCustomAttributes<CheckingBaseAttribute>();
        if (attrs.Count() > 0)
        {
          dictionary[property.Name] = attrs.ToArray();
          // Инициализация атрибутов.
          foreach (CheckingBaseAttribute attr in attrs)
            attr.Initialize(obj, property.Name);
        }
      }
    }

    #region IDataErrorHandler

    public string Check(string propertyName)
    {
      string result = string.Empty;

      if (dictionary.ContainsKey(propertyName))
      {
        CheckingBaseAttribute[] attrs = dictionary[propertyName];

        // Проверим каждое правило описываемое атрибутом.
        foreach (CheckingBaseAttribute attr in attrs)
        {
          string error = attr.Check();
          if (!string.IsNullOrEmpty(error))
            result += (result != string.Empty ? Environment.NewLine : "") + error;
        }

        // Если есть ошибка, то меняем (добавляем) в словарь ошибок.
        if (!string.IsNullOrEmpty(result))
        {
          this.allErrors[propertyName] = result;
        }
        // Если ошибок нет, то если ошибка есть в словаре, удалим ее.
        if (string.IsNullOrEmpty(result) && this.allErrors.ContainsKey(propertyName))
        {
          this.allErrors.Remove(propertyName);
        }
      }

      return result;
    }

    public string Error 
    { 
      get
      {
        // Обновим свойства.
        foreach (string propertyName in dictionary.Keys)
        {
          Check(propertyName);
        }

        StringBuilder builder = new StringBuilder();
        // Не будем повторять одинаковые ошибки.
        foreach (var e in allErrors.Values.Distinct())
        {
          if (builder.Length > 0)
            builder.Append(Environment.NewLine);
          builder.Append(e);
        }
        return builder.ToString();
      }
    }
    
    #endregion
  }
}

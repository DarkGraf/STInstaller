using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InstallerStudio.Utils
{
  interface IDataErrorHandler
  {
    string Check(string columnName);
  }

  [AttributeUsage(AttributeTargets.Property)]
  abstract class CheckingBaseAttribute : Attribute 
  {
    protected string Error { get; private set; }

    public CheckingBaseAttribute(string error)
    {
      Error = error;
    }

    /// <summary>
    /// Проверка атрибута.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <param name="obj">Проверяемый объект.</param>
    /// <returns></returns>
    public abstract string Check(string propertyName, object obj);
  }

  /// <summary>
  /// Атрибут требующий не пустое значение.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  class CheckingRequiredAttribute : CheckingBaseAttribute
  {
    internal const string DefaultError = "\"{0}\" не должен быть пустым.";

    public CheckingRequiredAttribute() : base(DefaultError) { }

    public CheckingRequiredAttribute(string error) : base(error) { }

    public override string Check(string propertyName, object obj)
    {
      object value = obj.GetType().GetProperty(propertyName).GetValue(obj);

      bool isError = value == null;

      // Частный случай для строк.
      if (!isError && value is string)
        isError = (string)value == string.Empty;

      return isError ? string.Format(Error, propertyName) : string.Empty;
    }
  }

  /// <summary>
  /// Атрибут требующий выбор хотя бы одного значения из группы.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  class CheckingFromGroup : CheckingBaseAttribute
  {
    internal const string DefaultError = "Необходимо выбрать хотя бы один элемент из группы.";

    readonly string grp;

    public CheckingFromGroup(string group) : this(DefaultError, group) { }

    public CheckingFromGroup(string error, string group) : base(error)
    {
      this.grp = group;
    }

    public override string Check(string propertyName, object obj)
    {
      string error = Error;

      // Получаем все свойства с данным атрибутом и текущей группой.
      var props = from prop in obj.GetType().GetProperties()
                  let attr = prop.GetCustomAttribute<CheckingFromGroup>()
                  where attr != null && (attr.grp == grp)
                  select prop;

      // Для проверки типов значения будем создавать тестовый объект, 
      // для проверки значения по умолчанию. Объект создаем для каждого 
      // свойства, так как типы свойств в группе могут быть 
      // разными (теоретически). В целях оптимизаци будем хэшировать 
      // создаваемый объект, если его тип не будет равен
      // текущему типу свойства, то тогда будем пересоздавать.
      object def = null;

      // Проверим все свойства. Если хотя бы одно из них
      // не равно значению по умолчанию, то все в порядке,
      // иначе ошибка.
      foreach (PropertyInfo prop in props)
      {
        bool ok;

        if (prop.PropertyType.IsValueType)
        {
          if (def == null || def.GetType() != prop.PropertyType)
            def = Activator.CreateInstance(prop.PropertyType);
          ok = !prop.GetValue(obj).Equals(def);
        }
        else
        {
          // Для ссылочных типов, просто проверим на null.
          ok = prop.GetValue(obj) != null;
          // Частный случай для string.
          if (ok && prop.PropertyType == typeof(string))
            ok = !string.IsNullOrEmpty((string)prop.GetValue(obj));
        }

        if (ok)
        {
          error = string.Empty;
          break;
        }
      }

      return error;
    }
  }

  class DataErrorHandler : IDataErrorHandler
  {
    object obj;
    Dictionary<string, CheckingBaseAttribute[]> dictionary;

    public DataErrorHandler(object obj)
    {
      this.obj = obj;
      dictionary = new Dictionary<string, CheckingBaseAttribute[]>();

      // Найдем открытые свойства класса.
      PropertyInfo[] propertyes = obj.GetType().GetProperties();
      // Запомним свойства, у которых есть атрибут типа CheckingBaseAttribute.
      foreach (PropertyInfo property in propertyes)
      {
        IEnumerable<CheckingBaseAttribute> attrs = property.GetCustomAttributes<CheckingBaseAttribute>();
        if (attrs.Count() > 0)
          dictionary[property.Name] = attrs.ToArray();
      }
    }

    public string Check(string columnName)
    {
      string result = string.Empty;

      if (dictionary.ContainsKey(columnName))
      {
        CheckingBaseAttribute[] attrs = dictionary[columnName];

        // Проверим каждое правило описываемое атрибутом.
        foreach (CheckingBaseAttribute attr in attrs)
        {
          string error = attr.Check(columnName, obj);
          if (!string.IsNullOrEmpty(error))
            result += (result != string.Empty ? Environment.NewLine : "") + error;
        }
      }

      return result;
    }
  }
}

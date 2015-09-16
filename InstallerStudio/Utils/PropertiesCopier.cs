using System;
using System.Linq;
using System.Reflection;

namespace InstallerStudio.Utils
{
  /// <summary>
  /// Класс копирования одноименных public свойств из одного объекта в другой.
  /// </summary>
  class PropertiesCopier
  {
    public static void Copy(object dest, object source, string[] propertiesName)
    {
      PropertyInfo[] destProps = dest.GetType().GetProperties();
      PropertyInfo[] sourceProps = source.GetType().GetProperties();

      // Получаем имена пересекающихся свойств.
      var commonPropsNames = destProps.Select(v => v.Name).Intersect(sourceProps.Select(v => v.Name));
      foreach (string propName in commonPropsNames)
      {
        PropertyInfo sourceProp = sourceProps.First(v => v.Name == propName);
        PropertyInfo destProp = destProps.First(v => v.Name == propName);
         
        // Проверим тип и аксессоры.
        if (sourceProp.PropertyType == destProp.PropertyType
          && sourceProp.CanRead 
          && sourceProp.CanWrite
          && destProp.CanRead 
          && destProp.CanWrite)
        {
          object value = sourceProp.GetValue(source);
          destProp.SetValue(dest, value);
        }
      }
    }
  }

  /// <summary>
  /// Класс копирования одноименных свойств у объектов хранения настроек.
  /// </summary>
  class SettingsInfoCopier : PropertiesCopier
  {
    static string[] properties =
    {
      "WixToolsetPath",
      "CandleFileName",
      "LightFileName",
      "TorchFileName",
      "PyroFileName",
      "UIExtensionFileName"
    };

    public static void Copy(object dest, object source)
    {
      PropertiesCopier.Copy(dest, source, properties);
    }
  }
}

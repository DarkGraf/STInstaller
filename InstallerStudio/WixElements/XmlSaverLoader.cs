using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace InstallerStudio.WixElements
{
  static class XmlSaverLoader
  {
    public static void Save<T>(T element, string fileName)
      where T : class
    {
      // Так как может быть в T указан базовый тип, для сериализатора необходимо
      // указать тип объекта, т. е. не typeof(T), а element.GetType().
      DataContractSerializer serializer = new DataContractSerializer(element.GetType());
      XmlWriterSettings settings = new XmlWriterSettings();
      settings.Indent = true;
      using (XmlWriter writer = XmlWriter.Create(fileName, settings))
      {
        serializer.WriteObject(writer, element);
      }
    }

    /// <summary>
    /// Десериализация объекта.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fileName"></param>
    /// <param name="targetType">Целевой тип для загрузки. Может быть не указан.
    /// Если указан, то должен быть наследником от T.</param>
    /// <returns></returns>
    public static T Load<T>(string fileName, Type targetType = null)
      where T : class
    {
      if (targetType != null && !typeof(T).IsAssignableFrom(targetType))
        throw new InvalidDataException();

      Type type = targetType ?? typeof(T);

      DataContractSerializer serializer = new DataContractSerializer(type);
      using (XmlReader reader = XmlReader.Create(fileName))
      {
        return serializer.ReadObject(reader) as T;
      }
    }
  }
}

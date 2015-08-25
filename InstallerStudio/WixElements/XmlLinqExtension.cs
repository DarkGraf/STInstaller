using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace InstallerStudio.WixElements
{
  /// <summary>
  /// Расширение для работы с Xml.Linq.
  /// </summary>
  static class XmlLinqExtension
  {
    /// <summary>
    /// Возвращает XProcessingInstruction с указанным target и строковыми данными начинающимися с data.
    /// Если элемент не найден, возвращается null.
    /// </summary>
    public static XProcessingInstruction GetXProcessingInstructionDataStartsWith(this XElement element, string target, string data)
    {
      return (from node in element.Nodes().OfType<XProcessingInstruction>()
              let instruction = (XProcessingInstruction)node
              where instruction.Target == target
                && instruction.Data.StartsWith(data)
              select node).FirstOrDefault();
    }

    /// <summary>
    /// Получает первый узел XElement с именем localName в узле.
    /// Если не найден, будет null.
    /// </summary>
    /// <returns></returns>
    public static XElement GetXElement(this XContainer container, string localName, XAttribute attribute = null)
    {
      return (from node in container.Descendants()
              where node.Name.LocalName == localName
                && (attribute == null || node.Attribute(attribute.Name).Value == attribute.Value)
              select node).FirstOrDefault();
    }

  }
}

using System.Linq;
using System.Xml.Linq;

namespace LightServerLib.Common
{
  public static class XElementExtension
  {
    /// <summary>
    /// Получает первый узел с именем localName в узле container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="localName"></param>
    /// <returns></returns>
    public static XElement GetNode(this XElement container, string localName, XAttribute attribute = null)
    {
      return (from node in container.Descendants()
              where node.Name.LocalName == localName
                && (attribute == null || node.Attribute(attribute.Name).Value == attribute.Value)
              select node).First();
    }
  }
}

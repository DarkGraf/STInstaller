using System;
using System.Linq;
using System.Xml.Linq;

namespace WixSTActions.Utils
{
  class HtmlBuilder
  {
    const string tagHtml = "html";
    const string tagHead = "head";
    const string tagMeta = "meta";
    const string tagTitle = "title";
    const string tagBody = "body";
    const string tagP = "p";
    const string tagH1 = "h1";
    const string tagH2 = "h2";

    XElement xmlHtml;

    public HtmlBuilder()
    {
      xmlHtml =
        new XElement(tagHtml,
          new XElement(tagHead,
            new XElement(tagMeta, new XAttribute("charset", "utf-8")),
            new XElement(tagTitle, " ")), // Необходимо вставить что то для корректного отображения.
          new XElement(tagBody));
    }

    public HtmlBuilder(string fileName)
    {
      xmlHtml = XElement.Load(fileName);
    }

    /// <summary>
    /// Заголовок документа.
    /// </summary>
    public string Title
    {
      get
      {
        XElement xmlTitle = GetNode(xmlHtml, tagTitle);
        return xmlTitle.Value;
      }
      set
      {
        XElement xmlTitle = GetNode(xmlHtml, tagTitle);
        xmlTitle.Value = value;
      }
    }

    public void AddP(string text)
    {
      GetNode(xmlHtml, tagBody).Add(new XElement(tagP, text));
    }

    public void AddH1(string text)
    {
      GetNode(xmlHtml, tagBody).Add(new XElement(tagH1, text));
    }

    public void AddH2(string text)
    {
      GetNode(xmlHtml, tagBody).Add(new XElement(tagH2, text));
    }

    public void Save(string fileName)
    {
      xmlHtml.Save(fileName);
    }

    public override string ToString()
    {
      return xmlHtml.ToString();
    }

    /// <summary>
    /// Получает первый узел с именем localName в узле container.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="localName"></param>
    /// <returns></returns>
    private XElement GetNode(XContainer container, string localName, XAttribute attribute = null)
    {
      return (from node in container.Descendants()
              where node.Name.LocalName == localName
                && (attribute == null || node.Attribute(attribute.Name).Value == attribute.Value)
              select node).First();
    }
  }
}

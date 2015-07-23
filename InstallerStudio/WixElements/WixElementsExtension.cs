using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using InstallerStudio.Models;

namespace InstallerStudio.WixElements
{
  static class WixElementsExtension
  {
    /// <summary>
    /// Возвращает всех потомков в дереве.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IEnumerable<IWixElement> Descendants(this IEnumerable<IWixElement> items)
    {
      Stack<IWixElement> stack = new Stack<IWixElement>(items);
      while (stack.Any())
      {
        IWixElement item = stack.Pop();
        yield return item;

        foreach (IWixElement i in item.Items)
          stack.Push(i);
      }
    }

    /// <summary>
    /// Ищет родителя переданного элемента начиная с корневого элемента главной сущности.
    /// Если элемент не найден, вернется null.
    /// </summary>
    /// <param name="mainItem"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static IWixElement GetParent(this IWixMainEntity mainItem, IWixElement child)
    {
      Stack<IWixElement> stack = new Stack<IWixElement>();
      stack.Push(mainItem.RootElement);
      while (stack.Any())
      {
        IWixElement item = stack.Pop();

        // Если элемент найден, сразу выходим.
        if (item.Items.Contains(child))
          return item;

        foreach (IWixElement i in item.Items)
          stack.Push(i);
      }

      return null;
    }
  }
}

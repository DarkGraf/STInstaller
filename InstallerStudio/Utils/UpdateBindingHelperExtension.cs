using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace InstallerStudio.Utils
{
  static class UpdateBindingHelperExtension
  {
    /// <summary>
    /// Возвращает последовательность дочерних элементов входящих в визуальное дерево (один уровень дерева).
    /// </summary>
    public static IEnumerable<DependencyObject> EnumerateVisualChildren(this DependencyObject dependencyObject)
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
      {
        yield return VisualTreeHelper.GetChild(dependencyObject, i);
      }
    }

    /// <summary>
    /// Возвращаем сам элемент и рекурсивно все дочерние элементы дерева.
    /// </summary>
    public static IEnumerable<DependencyObject> EnumerateVisualDescendents(this DependencyObject dependencyObject)
    {
      yield return dependencyObject;

      foreach (DependencyObject child in dependencyObject.EnumerateVisualChildren())
      {
        foreach (DependencyObject descendent in child.EnumerateVisualDescendents())
        {
          yield return descendent;
        }
      }
    }

    /// <summary>
    /// Обновляет привязку у дерева элементов.
    /// </summary>
    public static void UpdateBindingSources(this DependencyObject dependencyObject)
    {
      foreach (DependencyObject element in dependencyObject.EnumerateVisualDescendents())
      {
        LocalValueEnumerator localValueEnumerator = element.GetLocalValueEnumerator();
        while (localValueEnumerator.MoveNext())
        {
          BindingExpressionBase bindingExpression = BindingOperations.GetBindingExpressionBase(element, localValueEnumerator.Current.Property);
          if (bindingExpression != null)
          {
            bindingExpression.UpdateSource();
          }
        }
      }
    }
  }
}

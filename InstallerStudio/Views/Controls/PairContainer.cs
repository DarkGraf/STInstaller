using System;
using System.Windows;
using System.Windows.Controls;

namespace InstallerStudio.Views.Controls
{
  /// <summary>
  /// Grid с двумя колонками.
  /// Первый добавляемый объект помещается в первую колонку.
  /// Второй добавляемый объект помещается во вторую колонку.
  /// </summary>
  class PairContainer : Grid
  {
    public PairContainer()
    {
      ColumnDefinitions.Add(new ColumnDefinition());
      ColumnDefinitions.Add(new ColumnDefinition());
    }

    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
      base.OnVisualChildrenChanged(visualAdded, visualRemoved);
      if (Children.Count > 0)
        Grid.SetColumn(Children[0], 0);
      if (Children.Count > 1)
        Grid.SetColumn(Children[1], 1);
    }
  }
}

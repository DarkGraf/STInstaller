using System;
using System.Windows;
using System.Windows.Input;

using DevExpress.Xpf.Grid.TreeList;
using DevExpress.Xpf.Grid;

namespace InstallerStudio.Views.Controls
{
  /// <summary>
  /// Логика взаимодействия для WixTreeListControl.xaml
  /// </summary>
  public partial class WixTreeListControl
  {
    public WixTreeListControl()
    {
      InitializeComponent();
    }

    private void TreeListControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      // Если сделан щелчок не по данным, снять выделение.
      TreeListViewHitInfo hitInfo = treeListView.CalcHitInfo((DependencyObject)e.OriginalSource);
      if (hitInfo.HitTest == TreeListViewHitTest.DataArea)
      {
        treeListControl.SelectedItems.Clear();
        treeListControl.View.FocusedRowHandle = GridControl.InvalidRowHandle;
      }
    }
  }
}

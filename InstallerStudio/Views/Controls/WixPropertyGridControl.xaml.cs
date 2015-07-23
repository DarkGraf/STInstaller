using System.Windows.Controls;

using DevExpress.Xpf.PropertyGrid;

namespace InstallerStudio.Views.Controls
{
  /// <summary>
  /// Логика взаимодействия для WixPropertyGridControl.xaml
  /// </summary>
  public partial class WixPropertyGridControl : UserControl
  {
    public WixPropertyGridControl()
    {
      InitializeComponent();
    }

    private void propertyGridControl_CustomExpand(object sender, CustomExpandEventArgs args)
    {
      // Если выставлена привязка ReadOnly="{Binding Path=SelectedItem.Predefered}",
      // то не зависимо, выставлен флаг ExpandCategoriesWhenSelectedObjectChanged или нет,
      // категории не раскрываются. Как обходное решение, взято ручное раскрытие категорий.
      // Источник: https://www.devexpress.com/Support/Center/Question/Details/T253463.
      args.IsExpanded = true;
    }
  }
}

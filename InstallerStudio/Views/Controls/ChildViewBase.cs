using System;
using System.Windows;
using System.Windows.Controls;

using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Layout.Core;
using DevExpress.Xpf.PropertyGrid;
using DevExpress.Xpf.Bars;

namespace InstallerStudio.Views.Controls
{
  /*
   * Строит следующую структуру:
   *   DockLayoutManager
   *      |---LayoutGroup
   *      |      |---DocumentGroup (есть свойство-коллекция Documents)
   *      |      +---LayoutPanel (есть свойство PropertyPanel)
   *      +---AutoHideGroups
   *             +---LayoutPanel
   *                    +---TextBox
   */
  public class ChildViewBase : DockLayoutManager
  {
    DocumentGroup documentGroup;
    LayoutPanel propLayoutPanel;
    TextBox txtMessages;

    #region Зависимое свойство Documents.

    public static readonly DependencyPropertyKey DocumentsPropertyKey = 
      DependencyProperty.RegisterReadOnly("Documents",
        typeof(FreezableCollection<BaseLayoutItem>),
        typeof(ChildViewBase),
        new FrameworkPropertyMetadata(new FreezableCollection<BaseLayoutItem>(), new PropertyChangedCallback(DocumentsPropertyChangedCallback)));

    public static readonly DependencyProperty DocumentsProperty = DocumentsPropertyKey.DependencyProperty;

    private static void DocumentsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ChildViewBase obj = d as ChildViewBase;
      if (obj != null && obj.documentGroup != null)
      {
        obj.documentGroup.Items.Clear();
        foreach (BaseLayoutItem item in obj.Documents)
        {
          item.AllowDrag = false;
          item.AllowClose = false;
          obj.documentGroup.Items.Add(item);
        }
      }
    }

    public FreezableCollection<BaseLayoutItem> Documents
    {
      get { return (FreezableCollection<BaseLayoutItem>)GetValue(DocumentsProperty); }
    }

    #endregion

    #region Зависимое свойство PropertyPanel.

    public static readonly DependencyProperty PropertyPanelProperty =
      DependencyProperty.Register("PropertyPanel",
      typeof(object),
      typeof(ChildViewBase),
      new FrameworkPropertyMetadata(PropertyPanelPropertyChangedCallback));

    public object PropertyPanel
    {
      get { return (object)GetValue(PropertyPanelProperty); }
      set { SetValue(PropertyPanelProperty, value); }
    }

    private static void PropertyPanelPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ChildViewBase obj = d as ChildViewBase;
      if (obj != null && obj.propLayoutPanel != null)
        obj.propLayoutPanel.Content = obj.PropertyPanel;
    }

    #endregion

    #region Зависимое свойство SelectedDocumentIndex.

    public static readonly DependencyProperty SelectedDocumentIndexProperty =
      DependencyProperty.Register("SelectedDocumentIndex",
      typeof(int),
      typeof(ChildViewBase));

    /// <summary>
    /// Указывает на индекс активного документа в коллекции Documents.
    /// </summary>
    public int SelectedDocumentIndex
    {
      get { return (int)GetValue(SelectedDocumentIndexProperty); }
      set { SetValue(SelectedDocumentIndexProperty, value); }
    }

    #endregion

    #region Зависимое свойство BuildMessages.

    public static readonly DependencyProperty BuildMessagesProperty =
      DependencyProperty.Register("BuildMessages",
      typeof(string),
      typeof(ChildViewBase),
      new FrameworkPropertyMetadata(BuildMessagesPropertyChangedCallback));

    public string BuildMessages
    {
      get { return (string)GetValue(BuildMessagesProperty); }
      set { SetValue(BuildMessagesProperty, value); }
    }

    private static void BuildMessagesPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ChildViewBase obj = d as ChildViewBase;
      if (obj != null)
        obj.txtMessages.Text = (e.NewValue ?? "").ToString();
    }

    #endregion

    public ChildViewBase()
    {
      SetValue(DocumentsPropertyKey, new FreezableCollection<BaseLayoutItem>());

      // Если понадобится зависимый Ribbon для DocumentPanel, то он будет объединяться.
      MDIMergeStyle = MDIMergeStyle.Always;

      LayoutGroup layoutGroup = new LayoutGroup();
      layoutGroup.Orientation = Orientation.Horizontal;
      LayoutRoot = layoutGroup;

      documentGroup = DockController.AddDocumentGroup(DockType.None);
      DockController.Dock(documentGroup, layoutGroup, DockType.Fill);
      documentGroup.ItemWidth = new GridLength(70, GridUnitType.Star);
      // При изменении активного элемента, изменим зависимое свойство.
      documentGroup.SelectedItemChanged += (s, e) => 
      { 
        SelectedDocumentIndex = documentGroup.Items.IndexOf(documentGroup.SelectedItem); 
      };

      propLayoutPanel = DockController.AddPanel(DockType.None);
      DockController.Dock(propLayoutPanel, layoutGroup, DockType.Right);
      propLayoutPanel.Caption = "Свойства";
      propLayoutPanel.AllowDrag = false;
      propLayoutPanel.AllowClose = false;
      propLayoutPanel.ItemWidth = new GridLength(30, GridUnitType.Star);

      AutoHideGroup autoHideGroup = new AutoHideGroup();
      autoHideGroup.DockType = Dock.Bottom;
      AutoHideGroups.Add(autoHideGroup);

      LayoutPanel outLayoutPanel = new LayoutPanel();
      outLayoutPanel.Caption = "Вывод";
      outLayoutPanel.AllowDrag = false;
      outLayoutPanel.AllowClose = false;
      autoHideGroup.Add(outLayoutPanel);

      // Элемент для вывода информации о построении.
      txtMessages = new TextBox();
      txtMessages.TextWrapping = TextWrapping.Wrap;
      txtMessages.AcceptsReturn = true;
      txtMessages.IsReadOnly = true;
      txtMessages.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
      // При изменении содержимого активируем панель и будем прокручивать текст вниз.
      txtMessages.TextChanged += (s, e) => 
      {
        Activate(outLayoutPanel);
        (s as TextBox).ScrollToEnd();
      };
      outLayoutPanel.Content = txtMessages;
    }
  }
}

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Layout.Core;
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
   *                    +---RichTextBox
   */
  public class ChildViewBase : DockLayoutManager
  {
    SynchronizationContext synchronizationContext;
    int managedThreadId;

    DocumentGroup documentGroup;
    LayoutPanel propLayoutPanel;
    RichTextBox txtMessages;

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
      typeof(ObservableCollection<ViewMessage>),
      typeof(ChildViewBase),
      new FrameworkPropertyMetadata(null, BuildMessagesPropertyChangedCallback));

    public ObservableCollection<ViewMessage> BuildMessages
    {
      get { return (ObservableCollection<ViewMessage>)GetValue(BuildMessagesProperty); }
      set { SetValue(BuildMessagesProperty, value); }
    }

    private static void BuildMessagesPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ChildViewBase obj = d as ChildViewBase;
      // Если это инициализация.
      if (e.NewValue != null)
      {
        if (obj != null)
          obj.BuildMessages.CollectionChanged += obj.BuildMessages_CollectionChanged;
      }
    }

    void BuildMessages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      // Этот код может выполнятся не из UI-потока.
      // Поэтому проверим, и если это чужой поток, отошлем UI.
      SendOrPostCallback callback = null;

      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
          callback = delegate
          {
            foreach (var v in e.NewItems)
            {
              ViewMessage m = v as ViewMessage;
              if (m != null)
              {
                TextRange range = new TextRange(txtMessages.Document.ContentEnd, txtMessages.Document.ContentEnd);
                // В RichTextBox есть ошибка, не всегда переносит строки если
                // указывать Environment.NewLine. Укажем "\r".
                range.Text = m.Message + "\r";
                range.ApplyPropertyValue(TextElement.ForegroundProperty, m.Brush);
              }
            }
          };
          break;
        case NotifyCollectionChangedAction.Reset:
          callback = delegate { txtMessages.Document.Blocks.Clear(); };
          break;
      }

      if (callback != null)
      {
        lock (lockObj)
        {
        if (Thread.CurrentThread.ManagedThreadId == managedThreadId)
          callback(null);
        else
          synchronizationContext.Post(callback, null);
        }
      }
    }
    object lockObj = new object();
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
      txtMessages = new RichTextBox();
      txtMessages.IsReadOnly = true;
      txtMessages.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
      // Уберем отступы между параграфами.
      Style noSpaceStyle = new Style(typeof(Paragraph));
      noSpaceStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));
      txtMessages.Resources.Add(typeof(Paragraph), noSpaceStyle);
      // При изменении содержимого активируем панель и будем прокручивать текст вниз.
      txtMessages.TextChanged += (s, e) => 
      {
        Activate(outLayoutPanel);
        (s as RichTextBox).ScrollToEnd();
      };
      outLayoutPanel.Content = txtMessages;

      synchronizationContext = SynchronizationContext.Current;
      managedThreadId = Thread.CurrentThread.ManagedThreadId;
    }
  }

  public class ViewMessage
  {
    public string Message { get; private set; }
    public Brush Brush { get; private set; }

    public ViewMessage(string message, Brush brush)
    {
      Message = message;
      Brush = brush;
    }
  }
}

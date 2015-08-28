using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.PropertyGrid;
using DevExpress.Xpf.PropertyGrid.Internal;

using WinControls = System.Windows.Controls;

namespace InstallerStudio.Views.Controls
{
  /// <summary>
  /// Интерфейс для обеспечения WixInternalPropertyGridControl данными.
  /// </summary>
  public interface IWixPropertyGridControlDataSource
  {
    /// <summary>
    /// Используется для выбора установочных директорий.
    /// </summary>
    ObservableCollection<string> InstallDirectories { get; }
    /// <summary>
    /// Проверка каталога на возможность удаления из списка.
    /// </summary>
    /// <param name="directory">Проверяемая директория.</param>
    /// <returns>Истино, если можно удалить.</returns>
    bool CheckInstallDirectoryForDeleting(string directory);
  }

  /// <summary>
  ///  Паттерн Null-объект.
  /// </summary>
  class NullWixPropertyGridControlDataSource : IWixPropertyGridControlDataSource
  {
    public ObservableCollection<string> InstallDirectories
    {
      get { return new ObservableCollection<string>(); }
    }

    public bool CheckInstallDirectoryForDeleting(string directory)
    {
      return false;
    }
  }

  /// <summary>
  /// Вспомогательный атрибут для атрибута EditorAttribute,
  /// обеспечивающий передачу дополнительной информации.
  /// </summary>
  class EditorInfoAttribute : Attribute
  {
    public string Info { get; private set; }

    public EditorInfoAttribute(string info)
    {
      Info = info;
    }
  }

  /// <summary>
  /// Расширение PropertyGridControl с добавлением собственных редакторов.
  /// В настоящей версии PropertyGridControl от DevExpress не поддерживает
  /// пользовательские редакторы, поэтому реализуем данным способом.
  /// https://www.devexpress.com/Support/Center/Question/Details/S172000
  /// </summary>
  public class WixInternalPropertyGridControl : PropertyGridControl
  {
    #region Зависимое свойство WixDataSource.

    public static readonly DependencyProperty WixDataSourceProperty =
      DependencyProperty.Register("WixDataSource",
      typeof(IWixPropertyGridControlDataSource),
      typeof(WixInternalPropertyGridControl),
      new PropertyMetadata(WixDataSourcePropertyChangedCallback));

    public IWixPropertyGridControlDataSource WixDataSource
    {
      get { return (IWixPropertyGridControlDataSource)GetValue(WixDataSourceProperty); }
      set { SetValue(WixDataSourceProperty, value); }
    }

    private static void WixDataSourcePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      WixInternalPropertyGridControl obj = d as WixInternalPropertyGridControl;
      if (obj != null)
        obj.propertyBuilder.WixDataSource = e.NewValue as IWixPropertyGridControlDataSource;
    }

    #endregion

    WixPropertyBuilder propertyBuilder;

    public WixInternalPropertyGridControl()
    {
      // Получаем свойства для чтения.
      PropertyInfo PropertyBuilderInfo = typeof(PropertyGridControl).GetProperty("PropertyBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
      PropertyInfo DataControllerInfo = typeof(RowDataGenerator).GetProperty("DataController", BindingFlags.Instance | BindingFlags.NonPublic);
      PropertyInfo RowDataGeneratorInfo = typeof(PropertyGridControl).GetProperty("RowDataGenerator", BindingFlags.Instance | BindingFlags.NonPublic);

      // Аналогично this.PropertyBuilder = new WixPropertyBuilder(this).
      PropertyBuilderInfo.SetValue(this, propertyBuilder = new WixPropertyBuilder(this), null);

      RowDataGenerator rowDataGenerator = new RowDataGenerator();

      rowDataGenerator.BeginInit();
      rowDataGenerator.PropertyBuilder = PropertyBuilder;
      rowDataGenerator.View = View;

      // Аналогично rowDataGenerator.DataController = this.DataController.
      DataControllerInfo.SetValue(rowDataGenerator, DataController, null);
      // Аналогично this.RowDataGenerator = rowDataGenerator.
      RowDataGeneratorInfo.SetValue(this, rowDataGenerator, null);

      PropertyDefinitions = new PropertyDefinitionCollection();
      UpdateDataViewByShowCategories();
      rowDataGenerator.EndInit();

      ((IVisualClient)View).Invalidate(RowHandle.Root);
      InitializeInputBindings();
      AddLogicalChild(PropertyBuilder);
    }
  }

  public class WixPropertyBuilder : PropertyBuilder
  {
    PropertyGridControl propertyGridControl;

    public WixPropertyBuilder(PropertyGridControl propertyGridControl)
    {
      this.propertyGridControl = propertyGridControl;
      // Если не будет ничего проинициализировано в дальнейшем, то
      // будем безопасно использовать нулевой объект.
      WixDataSource = new NullWixPropertyGridControlDataSource();
    }

    public IWixPropertyGridControlDataSource WixDataSource { get; set; }

    public override PropertyDefinitionBase GetDefinition(DataController controller, 
      DataViewBase view, RowHandle handle, bool showCategories, bool getStandard = true)
    {
      PropertyDefinitionBase result = base.GetDefinition(controller, view, handle, showCategories, getStandard);

      if (result is PropertyDefinition)
      {
        PropertyInfo propertyInfo = propertyGridControl.SelectedObject.GetType().GetProperty(view.GetDisplayName(handle));
        if (propertyInfo != null)
        {
          EditorAttribute attribute = (EditorAttribute)propertyInfo.GetCustomAttributes(typeof(EditorAttribute), true).FirstOrDefault();
          // Также ищем вспомогательный атрибут с дополнительной информацией.
          EditorInfoAttribute attributeInfo = (EditorInfoAttribute)propertyInfo.GetCustomAttributes(typeof(EditorInfoAttribute), true).FirstOrDefault();
          if (attribute != null)
          {
            string info = attributeInfo != null ? attributeInfo.Info : null;
            BaseEditSettings settings = AttributesDispatcher.GetSettings(attribute.EditorTypeName, info, propertyGridControl, WixDataSource);
            if (settings != null)
              ((PropertyDefinition)result).EditSettings = settings;
          }
        }
      }
      return result;
    }
  }

  /// <summary>
  /// Статический класс содержащий имена редакторов свойств.
  /// </summary>
  public static class WixPropertyEditorsNames
  {
    /// <summary>
    /// Имя редактора выбора файла.
    /// </summary>
    public const string FilePropertyEditor = "FileEdit";
    /// <summary>
    /// Имя редактора выбора директории из списка.
    /// </summary>
    public const string DirectoryComboBoxPropertyEditor = "DirectoryComboBox";
    /// <summary>
    /// Имя редактора со списком collapse, expand или hidden.
    /// </summary>
    public const string FeatureDisplayComboBoxPropertyEditor = "FeatureDisplayComboBox";
    /// <summary>
    /// Имя редактора со списком allow и disallow.
    /// </summary>
    public const string FeatureAbsentComboBoxPropertyEditor = "FeatureAbsentComboBox";
    /// <summary>
    /// Имя редактора для установки целочисленной последовательности.
    /// </summary>
    public const string SqlScriptSequenceSpinEditPropertyEditor = "SqlScriptSequenceSpinEdit";
  }

  /// <summary>
  /// Диспетчер выбора редактора по его имени.
  /// </summary>
  static class AttributesDispatcher
  {
    public static BaseEditSettings GetSettings(string editorTypeName, string info, PropertyGridControl propertyGridControl, IWixPropertyGridControlDataSource dataSource)
    {
      switch (editorTypeName)
      {
        case WixPropertyEditorsNames.FilePropertyEditor:
          return new FilePropertyEditorSettings(propertyGridControl, info);
        case WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor:
          return new DirectoryComboBoxPropertyEditor(dataSource);
        case WixPropertyEditorsNames.FeatureDisplayComboBoxPropertyEditor:
          return new FeatureDisplayComboBoxPropertyEditor();
        case WixPropertyEditorsNames.FeatureAbsentComboBoxPropertyEditor:
          return new FeatureAbsentComboBoxPropertyEditor();
        case WixPropertyEditorsNames.SqlScriptSequenceSpinEditPropertyEditor:
          return new SqlScriptSequenceSpinEditPropertyEditor();
        default:
          return null;
      }
    }
  }

  /// <summary>
  /// Выбор файла.
  /// </summary>
  class FilePropertyEditorSettings : ButtonEditSettings
  {
    PropertyGridControl propertyGridControl;
    string filterExtensions;

    public FilePropertyEditorSettings(PropertyGridControl propertyGridControl, string filterExtensions)
    {
      this.propertyGridControl = propertyGridControl;
      // В info должен быть фильтр расширений для диалога. Если передан null,
      // будем показывать все файлы.
      this.filterExtensions = filterExtensions ?? "*.*|*.*";
      DefaultButtonClick += FilePropertyEditorSettings_DefaultButtonClick;
    }

    void FilePropertyEditorSettings_DefaultButtonClick(object sender, RoutedEventArgs e)
    {
      OpenFileDialog dialog = new OpenFileDialog();
      dialog.Filter = filterExtensions;
      if (dialog.ShowDialog().GetValueOrDefault())
      {
        ButtonEdit edit = sender as ButtonEdit;
        var v = ((RowData)((WinControls.TextBox)edit.EditCore).DataContext);
        if (edit != null)
        {
          edit.EditValue = dialog.FileName;
          // Не найдено решение, как сделать завершение редактирования для данного ButtonEdit,
          // поэтому просто передадим фокус гриду и завершение сделается автоматически.
          propertyGridControl.Focus();
        }
      }
    }
  }

  class DirectoryComboBoxPropertyEditor : ComboBoxEditSettings
  {
    Func<string, bool> checkForDeleting;

    /// <summary>
    /// Класс на основе выделенного значения в Popup компонента ComboBoxEdit
    /// вычисляет, отображать кнопку удаления или нет (делает ее прозрачной).
    /// </summary>
    class ButtonVisibilityManager
    {
      ComboBoxEdit cmb;
      Func<string, bool> checkForDeleting;
      string item = null;

      public ButtonVisibilityManager(ComboBoxEdit cmb, Func<string, bool> checkForDeleting)
      {
        this.cmb = cmb;
        this.checkForDeleting = checkForDeleting;
        cmb.PopupClosed += cmb_PopupClosed;
        cmb.PopupContentSelectionChanged += cmb_PopupContentSelectionChanged;
      }

      public double Opacity
      {
        get 
        { 
          return item == null || !checkForDeleting(item) ? 0 : 1; 
        }
      }

      void cmb_PopupClosed(object sender, ClosePopupEventArgs e)
      {
        item = null;
      }

      void cmb_PopupContentSelectionChanged(object sender, WinControls.SelectionChangedEventArgs e)
      {
        // При удалении будет RemovedItems содержать элементы.
        item = e.AddedItems.Count > 0 ? e.AddedItems[0].ToString() : null;
      }
    }

    public DirectoryComboBoxPropertyEditor(IWixPropertyGridControlDataSource dataSource)
    {
      ItemsSource = dataSource.InstallDirectories;
      checkForDeleting = dataSource.CheckInstallDirectoryForDeleting;
    }

    protected override void AssignToEditCore(IBaseEdit edit)
    {
      ComboBoxEdit cmb;
      if ((cmb = edit as ComboBoxEdit) != null)
      {
        // Чтобы содержимые элементы растягивались на все доступное пространство.
        cmb.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        // Создание ItemTemplate через FrameworkElementFactory.

        FrameworkElementFactory factoryGrid = new FrameworkElementFactory(typeof(WinControls.Grid));
        FrameworkElementFactory factoryColumnOne = new FrameworkElementFactory(typeof(WinControls.ColumnDefinition));
        FrameworkElementFactory factoryColumnTwo = new FrameworkElementFactory(typeof(WinControls.ColumnDefinition));
        FrameworkElementFactory factoryTextBlock = new FrameworkElementFactory(typeof(WinControls.TextBlock));
        FrameworkElementFactory factoryButton = new FrameworkElementFactory(typeof(WinControls.Button));

        // Настроим кнопку.
        // Настройка через factoryButton.SetValue(WinControls.Button.ContentProperty, "X") не работает.
        WinControls.ControlTemplate templateButton = new WinControls.ControlTemplate(typeof(WinControls.Button));
        FrameworkElementFactory factoryTextBlockButton = new FrameworkElementFactory(typeof(WinControls.TextBlock));
        factoryTextBlockButton.SetValue(WinControls.TextBlock.TextProperty, "X");
        factoryTextBlockButton.SetValue(WinControls.TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Red);
        factoryTextBlockButton.SetValue(WinControls.TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        templateButton.VisualTree = factoryTextBlockButton;

        factoryButton.Name = "btn";
        factoryButton.SetValue(WinControls.Button.TemplateProperty, templateButton);
        factoryButton.SetValue(WinControls.Button.OpacityProperty, 0d); // Суффикс обязателен.
        factoryButton.AddHandler(WinControls.Button.ClickEvent, new RoutedEventHandler(DeleteItem));

        // Триггер для кнопки.
        Trigger trig = new Trigger();
        trig.Property = UIElement.IsMouseOverProperty;
        trig.Value = true;

        Setter set = new Setter();
        set.Property = WinControls.Button.OpacityProperty;
        System.Windows.Data.Binding b;
        set.Value = b = new System.Windows.Data.Binding("Opacity");
        b.Source = new ButtonVisibilityManager(cmb, checkForDeleting);

        set.TargetName = "btn";
        trig.Setters.Add(set);

        // Создаем Grid и колонки в нем.
        factoryColumnOne.SetValue(WinControls.ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        factoryColumnTwo.SetValue(WinControls.ColumnDefinition.WidthProperty, new GridLength(15, GridUnitType.Pixel));
        factoryGrid.AppendChild(factoryColumnOne);
        factoryGrid.AppendChild(factoryColumnTwo);

        // Добавляем в Grid элементы.
        factoryTextBlock.SetValue(WinControls.Grid.ColumnProperty, 0);
        factoryButton.SetValue(WinControls.Grid.ColumnProperty, 1);
        factoryGrid.AppendChild(factoryTextBlock);
        factoryGrid.AppendChild(factoryButton);

        factoryTextBlock.SetBinding(WinControls.TextBlock.TextProperty, new System.Windows.Data.Binding());
       
        cmb.ItemTemplate = new DataTemplate();
        cmb.ItemTemplate.VisualTree = factoryGrid;
        cmb.ItemTemplate.Triggers.Add(trig);

        // Возможность добавления нового значения.
        cmb.ProcessNewValue += cmb_ProcessNewValue;
        cmb.AddNewButtonPlacement = EditorPlacement.Popup;
      }
      base.AssignToEditCore(edit);
    }

    void cmb_ProcessNewValue(DependencyObject sender, ProcessNewValueEventArgs e)
    {
      // Добавление елемента в список.
      ObservableCollection<string> itemsSource = ItemsSource as ObservableCollection<string>;
      if (itemsSource != null && !itemsSource.Contains(e.DisplayText))
      {
        itemsSource.Add(e.DisplayText);
        e.Handled = true;
      }
    }

    void DeleteItem(object sender, RoutedEventArgs e)
    {
      // Удаление элемента.
      WinControls.Button btn = e.OriginalSource as WinControls.Button;
      // Удаляем только если кнопка видима.
      if (btn != null && btn.Opacity > 0)
        ((ObservableCollection<string>)(Editor as ComboBoxEdit).ItemsSource).Remove(btn.DataContext as string);
    }
  }

  abstract class SimpleComboBoxPropertyEditor : ComboBoxEditSettings
  {
    public SimpleComboBoxPropertyEditor()
    {
      Items.AddRange(GetItems());
      IsTextEditable = false;
    }

    protected override void AssignToEditCore(IBaseEdit edit)
    {
      ((ComboBoxEdit)edit).NullValueButtonPlacement = EditorPlacement.Popup;
      base.AssignToEditCore(edit);      
    }

    protected abstract object[] GetItems();
  }

  class FeatureDisplayComboBoxPropertyEditor : SimpleComboBoxPropertyEditor
  {
    protected override object[] GetItems()
    {
      return new string[] { "Collapse", "Expand", "Hidden" };
    }
  }

  class FeatureAbsentComboBoxPropertyEditor : SimpleComboBoxPropertyEditor
  {
    protected override object[] GetItems()
    {
      return new string[] { "Allow", "Disallow" };
    }
  }

  class SqlScriptSequenceSpinEditPropertyEditor : SpinEditSettings
  {
    protected override void AssignToEditCore(IBaseEdit edit)
    {
      ((SpinEdit)edit).IsFloatValue = false;
      ((SpinEdit)edit).MinValue = 1;
      ((SpinEdit)edit).MaxValue = 100;
      base.AssignToEditCore(edit);
    }
  }
}

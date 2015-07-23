using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

using DevExpress.Xpf.PropertyGrid;
using DevExpress.Xpf.PropertyGrid.Internal;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Editors;

namespace InstallerStudio.Views.Controls
{
  /// <summary>
  /// Расширение PropertyGridControl с добавлением собственных редакторов.
  /// В настоящей версии PropertyGridControl от DevExpress не поддерживает
  /// пользовательских редакторов, поэтому реализуем данным способом.
  /// https://www.devexpress.com/Support/Center/Question/Details/S172000
  /// </summary>
  public class WixInternalPropertyGridControl : PropertyGridControl
  {
    public WixInternalPropertyGridControl()
    {
      // Получаем свойства для чтения.
      PropertyInfo PropertyBuilderInfo = typeof(PropertyGridControl).GetProperty("PropertyBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
      PropertyInfo DataControllerInfo = typeof(RowDataGenerator).GetProperty("DataController", BindingFlags.Instance | BindingFlags.NonPublic);
      PropertyInfo RowDataGeneratorInfo = typeof(PropertyGridControl).GetProperty("RowDataGenerator", BindingFlags.Instance | BindingFlags.NonPublic);

      // Аналогично this.PropertyBuilder = new WixPropertyBuilder(this).
      PropertyBuilderInfo.SetValue(this, new WixPropertyBuilder(this), null);

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
    }

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
          if (attribute != null)
          {
            BaseEditSettings settings = AttributesDispatcher.GetSettings(attribute.EditorTypeName);
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
  }

  /// <summary>
  /// Диспетчер выбора редактора по его имени.
  /// </summary>
  static class AttributesDispatcher
  {
    public static BaseEditSettings GetSettings(string editorTypeName)
    {
      switch (editorTypeName)
      {
        case WixPropertyEditorsNames.FilePropertyEditor:
          return new FilePropertyEditorSettings();
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
    public FilePropertyEditorSettings()
    {
      DefaultButtonClick += FilePropertyEditorSettings_DefaultButtonClick;
    }

    void FilePropertyEditorSettings_DefaultButtonClick(object sender, RoutedEventArgs e)
    {
      OpenFileDialog dialog = new OpenFileDialog();
      dialog.Filter = "*.*|*.*|*.exe|*.exe|*.dll|*.dll";
      if (dialog.ShowDialog().GetValueOrDefault())
      {
        ButtonEdit edit = sender as ButtonEdit;
        if (edit != null)
        {
          edit.Text = dialog.FileName;
        }
      }
    }
  }
}

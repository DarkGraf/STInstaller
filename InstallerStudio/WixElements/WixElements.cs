using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

using InstallerStudio.Common;
using InstallerStudio.Models;
using InstallerStudio.Views.Utils;
using InstallerStudio.Views.Controls;

namespace InstallerStudio.WixElements
{
  /* Используемые аттрибуты для свойств:
   * Category - обозначение категории свойства для PropertyGrid;
   * Browsable - скрытие свойства для PropertyGrid;
   * ReadOnly - свойство только для чтения в PropertyGrid;
   * Description - подсказка пользователю в PropertyGrid;
   * DataMember - для сериализации свойства.    
   * 
   * Для данных типов, при десериализации конструктор не вызывается,
   * поэтому необходимо предусмотреть метод в базовом типе с аттрибутом [OnDeserializing]
   * и в нем выполнить метод инициализации. Метод инициализации виртуальный.
   */

  #region Поддержка файлов.

  /// <summary>
  /// Поддержка файлов. Если некий элемент должен поддерживать работу с файлами,
  /// он должен реализовать этот интерфейс и генерировать соответствующие события
  /// для уведомления и воспроизводства действиями над файлами.
  /// </summary>
  interface IFileSupport
  {
    /// <summary>
    /// Вызывается при изменение информации о путях файла.
    /// </summary>
    event EventHandler<FileSupportEventArgs> FileChanged;

    /// <summary>
    /// Уведомляет подписчиков о удалении элемента.
    /// </summary>
    void NotifyDeleting();

    /// <summary>
    /// Возвращает массив из установочных директорий.
    /// Используется массив, так как элемент может поддерживать несколько файлов.
    /// </summary>
    string[] GetInstallDirectories();
  }

  class FileSupportEventArgs : EventArgs
  {
    public string OldFileName { get; private set; }
    public string OldDirectory { get; private set; }
    public string ActualFileName { get; private set; }
    public string ActualDirectory { get; private set; }
    public string RawFileName { get; private set; }

    public FileSupportEventArgs(string oldFileName, string oldDirectory,
      string actualFileName, string actualDirectory, string rawFileName)
    {
      OldFileName = oldFileName;
      OldDirectory = oldDirectory;
      ActualFileName = actualFileName;
      ActualDirectory = actualDirectory;
      RawFileName = rawFileName;
    }
  }

  /// <summary>
  /// Вспомогательный класс для инициализации полей имен файла и директорий,
  /// также создает аргумент события FileSupportEventArgs.
  /// Позволяет сократить объем однотипного кода для классов реализующий интерфейс IFileSupport.
  /// </summary>
  static class FileSupportHelper
  {
    public static FileSupportEventArgs FileNameChanged(string rawFileName, ref string fileName, string directory)
    {
      // Значение rawFileName (value) может содержать полный путь к файлу (при выборе 
      // пользователем файла через диалог), а может содержать только имя файла (при 
      // редактировании в PropertyGrid). В любом случае, запомним это значение и 
      // передадим для дальнейшего анализа, а в FileName запомним только имя файла.
      // Директория для инсталляции не изменилась, поэтому передаем ее в двух параметрах.
      string oldFileName = fileName;
      fileName = System.IO.Path.GetFileName(rawFileName);
      return new FileSupportEventArgs(oldFileName, directory, fileName, directory, rawFileName);
    }

    public static FileSupportEventArgs DirectoryChanged(string rawDirectory, ref string directory, string fileName)
    {
      // Имя файла не изменилось, в rawFileName передаем fileName.
      string oldDirectory = directory;
      directory = rawDirectory;
      return new FileSupportEventArgs(fileName, oldDirectory, fileName, directory, fileName);
    }
  }

  #endregion

  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixElementBase : ChangeableObject, IWixElement, IDataErrorInfo
  {
    #region Вложенные типы.

    /// <summary>
    /// Ошибочный тип дочернего элемента.
    /// </summary>
    internal class WrongChildTypeException : Exception { }

    internal class WixElementCollection : ObservableCollection<IWixElement>
    {
      public WixElementBase Parent { get; set; }

      protected override void InsertItem(int index, IWixElement item)
      {
        // Родитель будет равен null во время десериализации, в данном случае
        // ничего не будем проверять.
        if (Parent != null && !Parent.CheckChildType(item.GetType()))
          throw new WrongChildTypeException();

        base.InsertItem(index, item);
      }      
    }

    #endregion

    private string id;

    [DataMember(Name = "Items")]
    private WixElementCollection items;

    public WixElementBase()
    {
      items = new WixElementCollection();
      items.Parent = this;
      Initialize();
    }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context)
    {
      Initialize();
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
      // Если произошла десериализация, то родитель у коллекции равен null,
      // инициализируем его.
      items.Parent = this;
    }

    protected virtual void Initialize()
    {
      // При десериализации DataContractSerializer не вызывается конструктор
      // по умолчанию, поэтому вызываем данный метод из конструктора и из
      // метода OnDeserializing(). В нем производим всю необходимую инициализацию.
    }

    /// <summary>
    /// Проверяет, поддерживает ли текущий тип, дочерний элемент заданного типа.
    /// </summary>
    /// <param name="type">Тип дочернего элемента.</param>
    /// <returns>Истина, если поддерживает, ложь, в противном случае.</returns>
    public bool CheckChildType(Type type)
    {
      return AllowedTypesOfChildren.FirstOrDefault(v => v == type) != null;
    }

    /// <summary>
    /// Делает текущий объект нередактируемым (присваивает его свойству IsFrozen значение true).
    /// </summary>
    public void	Freeze()
    {
      IsFrozen = true;
    }

    public void Predefinition()
    {
      IsPredefined = true;
    }

    /// <summary>
    /// Возвращает разрешенные типы дочерних элементов.
    /// </summary>
    protected virtual IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return new Type[0]; }
    }

    #region IWixElement

    /// <summary>
    /// Идентификатор Wix-элемента.
    /// </summary>
    [Category(StringResources.CategoryMain)]
    [DataMember]
    [Description(StringResources.WixElementBaseIdDescription)]
    public string Id 
    {
      get { return id; }
      set { SetValue(ref id, value); }
    }

    /// <summary>
    /// Краткая сводка про компонент.
    /// </summary>
    [Browsable(false)]
    public virtual string Summary
    {
      // Не будем переопределять метод ToString(), пусть останется для разработки.
      get { return "";  }
    }

    [Browsable(false)]
    public abstract ElementsImagesTypes ImageType { get; }

    /// <summary>
    /// Дочерние элементы.
    /// </summary>
    [Browsable(false)]
    // Сериализуется через поле items. Если сериализовать данное
    // поле, то из-за типа IList<IWixElement> при десериализации
    // во внутреннем представлении будет задействован массив.
    // Определять в интерфейсе поле с типом WixElementCollection 
    // нехорошо, так как увеличивается связанность.
    public IList<IWixElement> Items 
    { 
      get { return items; } 
    }

    [Browsable(false)]
    public abstract string ShortTypeName { get; }

    /// <summary>
    /// Признак зафиксированного элемента.
    /// </summary>
    [Category(StringResources.CategoryAuxiliary)]
    [ReadOnly(true)]
    [DataMember]
    [Description(StringResources.WixElementBaseIsFrozenDescription)]
    public bool IsFrozen { get; protected set; }

    [Category(StringResources.CategoryAuxiliary)]
    [ReadOnly(true)]
    [DataMember]
    [Description(StringResources.WixElementBaseIsPredefinedDescription)]
    public bool IsPredefined { get; protected set; }

    public virtual bool AvailableForRun(Type type, IWixElement rootItem)
    {
      return CheckChildType(type);
    }

    [Browsable(false)]
    public bool IsReadOnly
    {
      get { return IsFrozen || IsPredefined; }
    }

    #endregion

    #region IDataErrorInfo

    [Browsable(false)]
    public string Error
    {
      get { return string.Empty; }
    }

    public virtual string this[string columnName]
    {
      get 
      {
        string result = string.Empty;
        switch (columnName)
        {
          case "Id":
            if (string.IsNullOrEmpty(Id))
              result = "Идентификатор не должен быть пустым.";
            break;
        }

        return result;
      }
    }

    #endregion    
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixFeatureElement : WixElementBase
  {
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixFeatureElementTitleDescription)]
    [DataMember]
    public string Title { get; set; }

    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixFeatureElementDescriptionDescription)]
    [DataMember]
    public string Description { get; set; }

    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixFeatureElementDisplayDescription)]
    [Editor(WixPropertyEditorsNames.FeatureDisplayComboBoxPropertyEditor, WixPropertyEditorsNames.FeatureDisplayComboBoxPropertyEditor)]
    [DataMember]
    public string Display { get; set; }

    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();

      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixFeatureElement), 
        typeof(WixComponentElement),
        typeof(WixDbComponentElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Feature; }
    }

    public override string ShortTypeName
    {
      get { return "Feature"; }
    }

    public override bool AvailableForRun(Type type, IWixElement rootItem)
    {
      // Особое бизнес правило: компонент DbComponent должен быть один.
      if (type == typeof(WixDbComponentElement) && rootItem.Items.Descendants().FirstOrDefault(v => v.GetType() == type) != null)
        return false;

      return base.AvailableForRun(type, rootItem);
    }

    #endregion
  }

  class WixPatchFamilyElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.SqlScript; }
    }

    public override string ShortTypeName 
    { 
      get { return "PatchFamily"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixComponentElement : WixElementBase
  {
    private Type[] allowedTypesOfChildren;

    public WixComponentElement()
    {
      // Присваемваем Guid только при явном создании.
      // При десериализации берем из файла.
      Guid = Guid.NewGuid();
    }

    [Category(StringResources.CategoryMain)]
    [DataMember]
    public Guid Guid { get; private set; }

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixFileElement)
      };
    }

    public override bool AvailableForRun(Type type, IWixElement rootItem)
    {
      // Особое бизнес правило: элемент File должен быть один для одного компонента.
      // Это необходимо чтобы формировать обновления для каждого файла (см. ТЗ).
      if (type == typeof(WixFileElement) && Items.Descendants().FirstOrDefault(v => v.GetType() == type) != null)
        return false;

      return base.AvailableForRun(type, rootItem);
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Component; }
    }

    public override string ShortTypeName
    {
      get { return "Component"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixDbComponentElement : WixComponentElement, IFileSupport
  {
    private Type[] allowedTypesOfChildren;
    private string mdfFile;
    private string ldfFile;
    private string directory;

    private void OnFileChanged(FileSupportEventArgs e)
    {
      if (FileChanged != null)
        FileChanged(this, e);
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.mdf|*.mdf|*.*|*.*")]
    [Category(StringResources.CategoryFiles)]
    public string MdfFile 
    {
      get { return mdfFile; }
      set 
      {
        FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref mdfFile, directory);
        NotifyPropertyChanged();
        OnFileChanged(e);
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.ldf|*.ldf|*.*|*.*")]
    [Category(StringResources.CategoryFiles)]
    public string LdfFile 
    {
      get { return ldfFile; }
      set 
      {
        FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref ldfFile, directory);
        NotifyPropertyChanged();
        OnFileChanged(e);
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    [Category(StringResources.CategoryFiles)]
    public string Directory
    {
      get { return directory; }
      set
      {
        if (directory != value)
        {
          // Тут два файла и одна директория, значит надо уведомить об изменении два раза.
          // Сохраним старое значение директории (где сейчас находятся файлы).
          string oldDirectory = directory;
          FileSupportEventArgs e = FileSupportHelper.DirectoryChanged(value, ref directory, mdfFile);
          NotifyPropertyChanged();
          OnFileChanged(e);
          // Также надо переместить ldf-файл, передаем в oldDirectory где он сейчас находится.
          e = FileSupportHelper.DirectoryChanged(value, ref oldDirectory, ldfFile);
          OnFileChanged(e);
        }
      }
    }

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();

      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixSqlScriptElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.DbComponent; }
    }

    public override string ShortTypeName
    {
      get { return "DbComponent"; }
    }

    #endregion

    #region IFileSupport

    public event EventHandler<FileSupportEventArgs> FileChanged;

    public void NotifyDeleting()
    {
      throw new NotImplementedException();
    }

    public string[] GetInstallDirectories()
    {
      return new string[] 
      { 
        Directory ?? ""
      };
    }

    #endregion
  }

  class WixSqlScriptElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.SqlScript; }
    }

    public override string ShortTypeName
    {
      get { return "SQLScript"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixFileElement : WixElementBase, IFileSupport
  {
    private Type[] allowedTypesOfChildren;
    private string fileName;
    private string installDirectory;

    private void OnFileChanged(FileSupportEventArgs e)
    {
      if (FileChanged != null)
        FileChanged(this, e);
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.*|*.*|*.exe|*.exe|*.dll|*.dll")]
    [Category(StringResources.CategoryFiles)]
    public string FileName
    {
      get { return fileName; }
      set 
      { 
        if (fileName != value)
        {
          FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref fileName, installDirectory);
          NotifyPropertyChanged();
          OnFileChanged(e);
        }
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    [Category(StringResources.CategoryFiles)]
    public string InstallDirectory
    {
      get { return installDirectory; }
      set 
      {
        if (installDirectory != value)
        {
          FileSupportEventArgs e = FileSupportHelper.DirectoryChanged(value, ref installDirectory, fileName);
          NotifyPropertyChanged();
          OnFileChanged(e);
        }
      }
    }

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixShortcutElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.File; }
    }

    public override string ShortTypeName
    {
      get { return "File"; }
    }

    public override string Summary
    {
      get { return FileName; }
    }

    #endregion

    #region IFileSupport

    public event EventHandler<FileSupportEventArgs> FileChanged;

    public void NotifyDeleting()
    {
      // Перед удалением элемента вызовется этот метод, передадим нулевые актуальные значения.
      OnFileChanged(new FileSupportEventArgs(fileName, installDirectory, null, null, null));
    }

    public string[] GetInstallDirectories()
    {
      return new string[] { InstallDirectory ?? "" };
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixShortcutElement : WixElementBase, IFileSupport
  {
    private string icon;

    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixShortcutElementNameDescription)]
    public string Name { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementDescriptionDescription)]
    public string Description { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixShortcutElementDirectoryDescription)]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    public string Directory { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementIconDescription)]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.ico|*.ico")]
    public string Icon 
    {
      get { return icon; }
      set 
      {
        if (icon != value)
        {
          FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref icon, null);
          NotifyPropertyChanged();
          OnFileChanged(e);
        }
      }
    }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementArgumentsDescription)]
    public string Arguments { get; set; }

    private void OnFileChanged(FileSupportEventArgs e)
    {
      if (FileChanged != null)
        FileChanged(this, e);
    }

    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Shortcut; }
    }

    public override string ShortTypeName
    {
      get { return "Shortcut"; }
    }

    #endregion

    #region IFileSupport

    public event EventHandler<FileSupportEventArgs> FileChanged;

    public void NotifyDeleting()
    {
      throw new NotImplementedException();
    }

    public string[] GetInstallDirectories()
    {
      throw new NotImplementedException();
    }

    #endregion
  }

  class WixSqlExtentedProceduresElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.SqlExtentedProcedures; }
    }

    public override string ShortTypeName
    {
      get { return "SqlExtentedProcedures"; }
    }

    #endregion
  }

  class WixMefPluginElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.MefPlugin; }
    }

    public override string ShortTypeName
    {
      get { return "MefPlugin"; }
    }

    #endregion
  }
}

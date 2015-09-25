using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;

using InstallerStudio.Common;
using InstallerStudio.Models;
using InstallerStudio.Views.Utils;
using InstallerStudio.Views.Controls;
using InstallerStudio.Utils;

namespace InstallerStudio.WixElements
{
  /* Используемые аттрибуты для свойств:
   * Category - обозначение категории свойства для PropertyGrid;
   * Browsable - скрытие свойства для PropertyGrid;
   * ReadOnly - свойство только для чтения в PropertyGrid;
   * Description - подсказка пользователю в PropertyGrid;
   * DataMember - для сериализации свойства.
   * KnownType - для сериализации дочерних элементов, при сериализаци конкретного типа.
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

    /// <summary>
    /// Получает файлы с относительными путями (пути инсталляции).
    /// </summary>
    /// <returns></returns>
    string[] GetFilesWithRelativePath();
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
      fileName = Path.GetFileName(rawFileName);
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
  [KnownType(typeof(WixFeatureElement))]
  [KnownType(typeof(WixComponentElement))]
  [KnownType(typeof(WixDbComponentElement))]
  [KnownType(typeof(WixSqlScriptElement))]
  [KnownType(typeof(WixFileElement))]
  [KnownType(typeof(WixShortcutElement))]
  [KnownType(typeof(WixSqlExtentedProceduresElement))]
  [KnownType(typeof(WixMefPluginElement))]
  [KnownType(typeof(WixLicenseElement))]
  [KnownType(typeof(WixSimpleFileSupportElement))]
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

    IDataErrorHandler errorHandler;

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
      errorHandler = new DataErrorHandler(this);
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
    [CheckingRequired(StringResources.IdCheckingRequired)]
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
      // Если будут использоваться в наследниках свойства в данном свойстве,
      // то для них необходимо реализовать поле и делать уведомление для обновления
      // данного свойства.
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
      get { return errorHandler.Error; }
    }

    public virtual string this[string columnName]
    {
      get { return errorHandler.Check(columnName); }
    }

    #endregion    
  }

  /// <summary>
  /// Простейшая реализация хранения файла в корне текущей временной директории. 
  /// Реализовано только свойство имя файла, директория принимается нулевой
  /// и не подразумевает изменение.
  /// </summary>
  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixSimpleFileSupportElement : WixElementBase, IFileSupport
  {
    private string fileName;

    protected string GetFileName()
    {
      return fileName;
    }

    protected void SetFileName(string value, string propertyName)
    {
      if (fileName != value)
      {
        FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref fileName, null);
        NotifyPropertyChanged(propertyName);
        OnFileChanged(e);
      }
    }

    private void OnFileChanged(FileSupportEventArgs e)
    {
      if (FileChanged != null)
        FileChanged(this, e);
    }

    public override string Summary
    {
      get { return fileName; }
    }

    #region IFileSupport

    public event EventHandler<FileSupportEventArgs> FileChanged;

    public void NotifyDeleting()
    {
      // Перед удалением элемента вызовется этот метод, передадим нулевые актуальные значения.
      OnFileChanged(new FileSupportEventArgs(fileName, null, null, null, null));
    }

    public string[] GetInstallDirectories()
    {
      return new string[] { "" };
    }

    public string[] GetFilesWithRelativePath()
    {
      return new string[] { fileName };
    }

    #endregion
  }

  #region Элементы Msi.

  [DataContract(Namespace = StringResources.Namespace)]
  class WixFeatureElement : WixElementBase
  {
    string descrition;

    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixFeatureElementTitleDescription)]
    [DataMember]
    public string Title { get; set; }

    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixFeatureElementDescriptionDescription)]
    [DataMember]
    public string Description 
    {
      get { return descrition; }
      set { SetValue(ref descrition, value); }
    }

    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixFeatureElementDisplayDescription)]
    [Editor(WixPropertyEditorsNames.FeatureDisplayComboBoxPropertyEditor, WixPropertyEditorsNames.FeatureDisplayComboBoxPropertyEditor)]
    [DataMember]
    public string Display { get; set; }

    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixFeatureElementAbsentDescription)]
    [Editor(WixPropertyEditorsNames.FeatureAbsentComboBoxPropertyEditor, WixPropertyEditorsNames.FeatureAbsentComboBoxPropertyEditor)]
    [DataMember]
    public string Absent { get; set; }

    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    public override string Summary
    {
      get 
      { 
        ;
        return string.Format("{0}Вложенных элементов: {1} шт.", 
          string.IsNullOrWhiteSpace(Description) ? "" : Description + ". ", Items.Count); 
      }
    }

    protected override void Initialize()
    {
      base.Initialize();

      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixFeatureElement), 
        typeof(WixComponentElement),
        typeof(WixDbComponentElement),
        // Разрешим добавлять WixMefPluginElement только в корневую Feature.
        // На самом деле он будет добавлен в Product.wxs.
        typeof(WixMefPluginElement),
        // Разрешим добавлять WixLicenseElement только в корневую Feature 
        // и только один элемент.
        // На самом деле он будет добавлен в Product.wxs.
        typeof(WixLicenseElement)
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

      // Разрешим добавлять WixMefPluginElement только в корневую Feature.
      // На самом деле он будет добавлен в Product.wxs.
      if (type == typeof(WixMefPluginElement) && rootItem != this)
        return false;

      // Разрешим добавлять WixLicenseElement только в корневую Feature
      // и только один элемент.
      // На самом деле он будет добавлен в Product.wxs.
      if (type == typeof(WixLicenseElement) 
        && (rootItem != this || rootItem.Items.Descendants().FirstOrDefault(v => v.GetType() == type) != null))
        return false;

      return base.AvailableForRun(type, rootItem);
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

    private void OnFileChanged(FileSupportEventArgs e)
    {
      if (FileChanged != null)
        FileChanged(this, e);
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.mdf|*.mdf|*.*|*.*")]
    [Category(StringResources.CategoryDBFiles)]
    [Description(StringResources.WixDbComponentElementMdfFileDescription)]
    [CheckingRequired(StringResources.DbFileCheckingRequired)]
    public string MdfFile 
    {
      get { return mdfFile; }
      set 
      {
        if (mdfFile != value)
        {
          FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref mdfFile, null);
          NotifyPropertyChanged();
          OnFileChanged(e);
        }
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.ldf|*.ldf|*.*|*.*")]
    [Category(StringResources.CategoryDBFiles)]
    [Description(StringResources.WixDbComponentElementLdfFileDescription)]
    [CheckingRequired(StringResources.DbFileCheckingRequired)]
    public string LdfFile 
    {
      get { return ldfFile; }
      set 
      {
        if (ldfFile != value)
        {
          FileSupportEventArgs e = FileSupportHelper.FileNameChanged(value, ref ldfFile, null);
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
        typeof(WixSqlScriptElement),
        typeof(WixSqlExtentedProceduresElement)
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
      // Перед удалением элемента вызовется этот метод, передадим нулевые актуальные значения.
      OnFileChanged(new FileSupportEventArgs(mdfFile, null, null, null, null));
      OnFileChanged(new FileSupportEventArgs(ldfFile, null, null, null, null));
    }

    public string[] GetInstallDirectories()
    {
      return new string[] { "" };
    }

    public string[] GetFilesWithRelativePath()
    {
      return new string[] { mdfFile, ldfFile };
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixSqlScriptElement : WixSimpleFileSupportElement
  {
    int sequence;
    // Связанные параметры. При изменении одного из них
    // необходимо обновить другие (нужно для отображение ошибок).
    private bool executeOnInstall;
    private bool executeOnReinstall;
    private bool executeOnUninstall;

    public WixSqlScriptElement()
    {
      // Инициализируем только при создании (не при десериализации).
      Sequence = 1;
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.sql|*.sql|*.*|*.*")]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixSqlScriptElementScriptDescription)]
    [CheckingRequired(StringResources.ScriptCheckingRequired)]
    public string Script
    {
      get { return GetFileName(); }
      set { SetFileName(value, "Script"); }
    }

    [DataMember]
    [Category(StringResources.CategoryRunModes)]
    [Description(StringResources.WixSqlScriptElementExecuteOnInstallDescription)]
    [CheckingFromGroup(StringResources.ExecuteCheckingFromGroup)]
    public bool ExecuteOnInstall 
    {
      get { return executeOnInstall; }
      set 
      { 
        executeOnInstall = value;
        NotifyPropertyChanged("ExecuteOnInstall");
        NotifyPropertyChanged("ExecuteOnReinstall");
        NotifyPropertyChanged("ExecuteOnUninstall");
      }
    }

    [DataMember]
    [Category(StringResources.CategoryRunModes)]
    [Description(StringResources.WixSqlScriptElementExecuteOnReinstallDescription)]
    [CheckingFromGroup(StringResources.ExecuteCheckingFromGroup)]
    public bool ExecuteOnReinstall
    {
      get { return executeOnReinstall; }
      set
      {
        executeOnReinstall = value;
        NotifyPropertyChanged("ExecuteOnInstall");
        NotifyPropertyChanged("ExecuteOnReinstall");
        NotifyPropertyChanged("ExecuteOnUninstall");
      }
    }

    [DataMember]
    [Category(StringResources.CategoryRunModes)]
    [Description(StringResources.WixSqlScriptElementExecuteOnUninstallDescription)]
    [CheckingFromGroup(StringResources.ExecuteCheckingFromGroup)]
    public bool ExecuteOnUninstall
    {
      get { return executeOnUninstall; }
      set
      {
        executeOnUninstall = value;
        NotifyPropertyChanged("ExecuteOnInstall");
        NotifyPropertyChanged("ExecuteOnReinstall");
        NotifyPropertyChanged("ExecuteOnUninstall");
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.SqlScriptSequenceSpinEditPropertyEditor, WixPropertyEditorsNames.SqlScriptSequenceSpinEditPropertyEditor)]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixSqlScriptElementSequenceDescription)]
    public int Sequence 
    {
      get { return sequence; }
      set { SetValue(ref sequence, value); }
    }

    #region WixElementBase

    public override string Summary
    {
      get { return string.Format("{0}. Последовательность выполнения: {1}", base.Summary, Sequence); }
    }

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
    [Description(StringResources.WixFileElementFileNameDescription)]
    [CheckingRequired(StringResources.FileCheckingRequired)]
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

          // Если value содержит полный путь к файлу, то значит идет добавление или замена
          // файла. Обновим версию, размер и дату файла.
          if (value != null && value == Path.GetFullPath(value))
          {
            FileInfo f = new FileInfo(value);
            CreationDate = f.CreationTime.ToString();
            ChangeDate = f.LastWriteTime.ToString();
            Size = SizeAutoFormatting.Format(f.Length);

            FileVersionInfo v = FileVersionInfo.GetVersionInfo(value);
            Version = v.FileVersion;

            NotifyPropertyChanged("CreationDate");
            NotifyPropertyChanged("ChangeDate");
            NotifyPropertyChanged("Size");
            NotifyPropertyChanged("Version");
          }
        }
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    [Category(StringResources.CategoryFiles)]
    [Description(StringResources.WixFileElementInstallDirectoryDescription)]
    [CheckingRequired(StringResources.InstallDirectoryCheckingRequired)]
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

    // Будем сериализовать вспомогательные свойства Version, CreationDate, ChangeDate
    // и Size. Это освобождает нас при открытии инициализировать эти свойства.

    [DataMember]
    [Category(StringResources.CategoryFilesProperties)]
    [Description(StringResources.WixFileElementVersionDescription)]
    public string Version { get; private set; }

    [DataMember]
    [Category(StringResources.CategoryFilesProperties)]
    [Description(StringResources.WixFileElementCreationDateDescription)]
    public string CreationDate { get; private set; }

    [DataMember]
    [Category(StringResources.CategoryFilesProperties)]
    [Description(StringResources.WixFileElementChangeDateDescription)]
    public string ChangeDate { get; private set; }

    [DataMember]
    [Category(StringResources.CategoryFilesProperties)]
    [Description(StringResources.WixFileElementSizeDescription)]
    public string Size { get; private set; }

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
      get 
      { 
        string str = FileName;
        str += string.IsNullOrEmpty(Size) ? "" : ", размер: " + Size;
        str += string.IsNullOrEmpty(Version) ? "" : ", версия: " + Version;
        return str;
      }
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
      return new string[] { installDirectory ?? "" };
    }

    public string[] GetFilesWithRelativePath()
    {
      return new string[] { Path.Combine(installDirectory ?? "", fileName) };
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixShortcutElement : WixSimpleFileSupportElement
  {
    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixShortcutElementNameDescription)]
    [CheckingRequired(StringResources.DisplayNameCheckingRequired)]
    public string Name { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementDescriptionDescription)]
    public string Description { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixShortcutElementDirectoryDescription)]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    [CheckingRequired(StringResources.DirectoryCheckingRequired)]
    public string Directory { get; set; }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementIconDescription)]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.ico|*.ico")]
    public string Icon 
    {
      get { return GetFileName(); }
      set { SetFileName(value, "Icon"); }
    }

    [DataMember]
    [Category(StringResources.CategoryMiscellaneous)]
    [Description(StringResources.WixShortcutElementArgumentsDescription)]
    public string Arguments { get; set; }

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
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixSqlExtentedProceduresElement : WixSimpleFileSupportElement
  {
    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixSqlExtentedProceduresElementFileNameDescription)]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.dll|*.dll|*.*|*.*")]
    [CheckingRequired(StringResources.FileCheckingRequired)]
    public string FileName 
    {
      get { return GetFileName(); }
      set { SetFileName(value, "FileName"); }
    }

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

  [DataContract(Namespace = StringResources.Namespace)]
  class WixMefPluginElement : WixSimpleFileSupportElement
  {
    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixMefPluginElementFileNameDescription)]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.dll|*.dll")]
    [CheckingRequired(StringResources.FileCheckingRequired)]
    public string FileName
    {
      get { return GetFileName(); }
      set { SetFileName(value, "FileName"); }
    }

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

  [DataContract(Namespace = StringResources.Namespace)]
  class WixLicenseElement : WixSimpleFileSupportElement
  {
    [DataMember]
    [Category(StringResources.CategoryMain)]
    [Description(StringResources.WixLicenseElementFileNameDescription)]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    [EditorInfo("*.rtf|*.rtf")]
    [CheckingRequired(StringResources.FileCheckingRequired)]
    public string FileName
    {
      get { return GetFileName(); }
      set { SetFileName(value, "FileName"); }
    }

    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.License; }
    }

    public override string ShortTypeName
    {
      get { return "License"; }
    }

    #endregion
  }

  #endregion

  #region Элементы Msp.

  [DataContract(Namespace = StringResources.Namespace)]
  // Аналог в Wix отсутствует.
  class WixPatchRootElement : WixElementBase
  {
    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixPatchElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Patch; }
    }

    public override string ShortTypeName
    {
      get { return "PatchRoot"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixPatchElement : WixElementBase
  {
    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixPatchComponentElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Patch; }
    }

    public override string ShortTypeName
    {
      get { return "Patch"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixPatchComponentElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Component; }
    }

    public override string ShortTypeName
    {
      get { return "PatchComponent"; }
    }

    #endregion
  }

  #endregion
}
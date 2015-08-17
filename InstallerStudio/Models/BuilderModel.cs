using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using InstallerStudio.Common;
using InstallerStudio.Views.Utils;
using InstallerStudio.WixElements;
using InstallerStudio.Utils;

namespace InstallerStudio.Models
{
  public class CommandMetadata
  {
    public string Group { get; private set; }
    public string Caption { get; private set; }
    public Type WixElementType { get; private set; }
    public ElementsImagesTypes ImageType { get; private set; }

    public CommandMetadata(string group, Type wixElementType)
    {
      Group = group;
      WixElementType = wixElementType;
      
      if (!typeof(IWixElement).IsAssignableFrom(WixElementType))
        throw new NotSupportedException();

      // Создаем элемент через рефлексию и узнаем тип изображения.
      IWixElement wixElementSample = Activator.CreateInstance(wixElementType) as IWixElement;
      Caption = wixElementSample.ShortTypeName;
      ImageType = wixElementSample.ImageType;
    }
  }

  abstract class BuilderModel : NotifyObject, IDisposable
  {
    #region Константы.
    
    /// <summary>
    /// Имя файла содержащее полное описание пакета.
    /// </summary>
    private const string DescriptionFileName = "_Content.dsc";

    #endregion

    #region Частные поля.

    /// <summary>
    /// Выделенный элемент или null, если подразумевается выделенный элемент RootItem.
    /// </summary>
    private IWixElement selectedItem;
    /// <summary>
    /// Хранилилще файлов.
    /// </summary>
    private IFileStore fileStore;

    /// <summary>
    /// Словарь количества экзепляров по типам для генерации уникального имени IWixElement.
    /// </summary>
    private Dictionary<Type, int> itemsCountDictionaryByType;

    #endregion

    public BuilderModel()
    {
      selectedItem = null;
      MainItem = CreateMainEntity();
      itemsCountDictionaryByType = new Dictionary<Type, int>();
      // Создаем хранилище файлов.
      fileStore = FileStoreCreator.Create();
    }

    /// <summary>
    /// Создание самой главной сущности Wix.
    /// </summary>
    /// <returns></returns>
    protected abstract IWixMainEntity CreateMainEntity();

    /// <summary>
    /// Получение комманд для работы с элементами.
    /// </summary>
    /// <returns></returns>
    public abstract CommandMetadata[] GetElementCommands();

    /// <summary>
    /// Добавление элемента в SelectedItem типа wixElementType.
    /// </summary>
    /// <param name="wixElementType"></param>
    public IWixElement AddItem(Type wixElementType)
    {
      if (!typeof(IWixElement).IsAssignableFrom(wixElementType))
        throw new NotSupportedException();

      // Создаем элемент через рефлексию.
      IWixElement item = Activator.CreateInstance(wixElementType) as IWixElement;

      // Генерируем уникальное имя дочернего элемента.
      if (!itemsCountDictionaryByType.ContainsKey(wixElementType))
        itemsCountDictionaryByType.Add(wixElementType, 0);
      item.Id = item.ShortTypeName + ++itemsCountDictionaryByType[wixElementType];

      // Если выбранный элемент пустой, то считаем что выбран корневой элемент.
      (SelectedItem ?? RootItem).Items.Add(item);

      SelectedItem = item;

      // Если добавленный элемент реализует интерфейс работы с файлом, добавим обработчик.
      // Незабываем удалить его при удалении элемента.
      if (item is IFileSupport)
        (item as IFileSupport).FileChanged += BuilderModel_FileChanged;

      return item;
    }

    /// <summary>
    /// Удаление выделенного элемента.
    /// После удаления SelectedItem будет равен null.
    /// </summary>
    public void RemoveSelectedItem()
    {
      if (SelectedItem != null)
      {
        // Если элемент реализует интерфейс работы с файлом удалим обработчик.
        // Добавлен при создании объекта.
        if (SelectedItem is IFileSupport)
        {
          // Уведомим подписчиков об удалении элемента.
          (SelectedItem as IFileSupport).NotifyDeleting();
          (SelectedItem as IFileSupport).FileChanged -= BuilderModel_FileChanged;
        }

        // Поиск родителя текущего элемента начиная с IWixMainEntity.RootElement.
        IWixElement parent = MainItem.GetParent(SelectedItem);
        if (parent != null)
          parent.Items.Remove(SelectedItem);

        SelectedItem = null;
      }
    }

    /// <summary>
    /// Сохранение главной сущности.
    /// </summary>
    public void Save(string fileName)
    {
      // Сохранаяем в хранилище файл описания проекта. 
      string descriptionFileName = Path.Combine(fileStore.StoreDirectory, DescriptionFileName);
      XmlSaverLoader.Save(MainItem, descriptionFileName);
      // Добавим описание в коллекцию хранилища.
      // Не путаем физическое и логическое имена файлов.
      fileStore.AddFile(descriptionFileName, DescriptionFileName);

      fileStore.Save(fileName);
    }

    /// <summary>
    /// Загрузка главной сущности.
    /// </summary>
    public void Load(string fileName)
    {
      if (fileStore != null)
        fileStore.Dispose();

      fileStore = FileStoreCreator.Create(fileName);

      string descriptionFileName = Path.Combine(fileStore.StoreDirectory, DescriptionFileName);
      MainItem = XmlSaverLoader.Load<IWixMainEntity>(descriptionFileName, MainItem.GetType());

      // Для элементов не для чтения, если они поддерживают работу с файлом, добавим обработчик события.
      foreach (IFileSupport item in RootItem.Items.Descendants().Where(v => !v.IsReadOnly).OfType<IFileSupport>())
        item.FileChanged += BuilderModel_FileChanged;
    }

    public void Build()
    {
      MainItem.Build();
    }

    /// <summary>
    /// Самая главная сущность Wix.
    /// </summary>
    public IWixMainEntity MainItem { get; private set; }

    /// <summary>
    /// Корневой элемент.
    /// </summary>
    public IWixElement RootItem 
    {
      get { return MainItem.RootElement; }
    }

    /// <summary>
    /// Дочерние элементы (RootItem.Items).
    /// </summary>
    public IList<IWixElement> Items
    {
      get { return RootItem.Items; }
    }

    /// <summary>
    /// Выделенный элемент или null, если подразумевается выделенный элемент RootItem.
    /// </summary>
    public IWixElement SelectedItem
    {
      get { return selectedItem; }
      set 
      {
        // Не будем использовать метод SetValue<T>(...), ради оптимизации реализуем сами.
        if (selectedItem != value)
        {
          if (value != null)
          {
            // Определяем, что присваемый элемент принадлежит в конечном итоге RootItem.
            // Если элемент не нашли, то это ошибочная ситуация.
            if (Items.Descendants().FirstOrDefault(v => v == value) == null)
              throw new IndexOutOfRangeException();
          }

          selectedItem = value;
          NotifyPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Обработка события изменения информации о добавлении файла.
    /// Здесь происходит работа с файлами в файловом хранилище.
    /// </summary>
    void BuilderModel_FileChanged(object sender, FileSupportEventArgs e)
    {
      FileStoreSynchronizer.Synchronize(fileStore, e);
    }

    #region IDisposable
    
    public void Dispose()
    {
      fileStore.Dispose();
    }

    #endregion
  }

  // Паттерн "Абстрактная фабрика".
  abstract class BuilderModelFactory
  {
    public abstract BuilderModel Create();
  }

  class FileStoreCreator
  {
    static bool silent = false;

    public static IFileStore Create()
    {
      return new ZipFileStore(silent);
    }

    public static IFileStore Create(string fileName)
    {
      return new ZipFileStore(fileName, silent);
    }
  }

  static class FileStoreSynchronizer
  {
    public static void Synchronize(IFileStore store, FileSupportEventArgs e)
    {
      // Если не известно имя файла, выходим.
      if (string.IsNullOrEmpty(e.ActualFileName) && string.IsNullOrEmpty(e.OldFileName))
        return;

      // Формируем путь к файлу в хранилище с учетом устанавливаемой директории.
      // Если имя файла не известно, оно не должно использоваться ниже.
      string actualRelativePath = e.ActualFileName != null ? Path.Combine(e.ActualDirectory ?? "", e.ActualFileName) : null;
      string oldRelativePath = e.OldFileName != null ? Path.Combine(e.OldDirectory ?? "", e.OldFileName) : null;

      // За один вызов может измениться либо имя файла, либо директория.

      // Новый файл.
      // Если в "сырых данных" указано имя файла с полным путем и старое имя пустое, то значит это новый файл.
      if (Path.IsPathRooted(e.RawFileName) && string.IsNullOrEmpty(e.OldFileName) && File.Exists(e.RawFileName))
      {
        store.AddFile(e.RawFileName, actualRelativePath);
      }
      // Удаление файла.
      // Если актуальное имя пустое, а старое есть в хранилище, удалим файл.
      else if (string.IsNullOrEmpty(e.ActualFileName) && store.Files.Contains(oldRelativePath))
      {
        store.DeleteFile(oldRelativePath);
      }
      // Переименование файла.
      else if (!string.IsNullOrEmpty(e.ActualFileName) && !string.IsNullOrEmpty(e.OldFileName) 
        && e.ActualFileName != e.OldFileName && e.ActualFileName == e.RawFileName)
      {
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
      // Перемещение файла в другой каталог.
      else if (e.ActualFileName == e.OldFileName && e.ActualDirectory != e.OldDirectory && e.ActualFileName == e.RawFileName)
      {
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
      // Замена на файл с тем же именем.
      else if (Path.IsPathRooted(e.RawFileName) && !string.IsNullOrEmpty(e.OldFileName) && !string.IsNullOrEmpty(e.ActualFileName) 
        && e.ActualDirectory == e.OldDirectory && File.Exists(e.RawFileName))
      {
        // Заменяем файл, имя остается тем же.
        store.ReplaceFile(oldRelativePath, e.RawFileName);
        // Меняем имя файла.
        store.MoveFile(oldRelativePath, actualRelativePath);
      }
    }
  }
}

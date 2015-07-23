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

  abstract class BuilderModel : NotifyObject
  {
    #region Константы.
    
    /// <summary>
    /// Имя файла содержащее полное описание пакета.
    /// </summary>
    private const string DescriptionFileName = "_Content.dsc";

    #endregion

    /// <summary>
    /// Выделенный элемент или null, если подразумевается выделенный элемент RootItem.
    /// </summary>
    private IWixElement selectedItem;

    /// <summary>
    /// Словарь количества экзепляров по типам для генерации уникального имени IWixElement.
    /// </summary>
    private Dictionary<Type, int> itemsCountDictionaryByType;

    public BuilderModel()
    {
      selectedItem = null;
      MainItem = CreateMainEntity();
      itemsCountDictionaryByType = new Dictionary<Type, int>();
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
      // Создаем хранилище файлов.
      using (IFileStore store = new ZipFileStore())
      {
        // Сохранаяем в хранилище файл описания проекта. 
        string descriptionFileName = Path.Combine(store.StoreDirectory, DescriptionFileName);
        XmlSaverLoader.Save(MainItem, descriptionFileName);
        // Добавим описание в коллекцию хранилища.
        // Не путаем физическое и логическое имена файлов.
        store.AddFile(descriptionFileName, DescriptionFileName);

        store.Save(fileName);
      }
    }

    /// <summary>
    /// Загрузка главной сущности.
    /// </summary>
    public void Load(string fileName)
    {
      MainItem = XmlSaverLoader.Load<IWixMainEntity>(fileName, MainItem.GetType());
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
  }

  // Паттерн "Абстрактная фабрика".
  abstract class BuilderModelFactory
  {
    public abstract BuilderModel Create();
  }
}

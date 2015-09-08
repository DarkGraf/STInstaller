using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using InstallerStudio.Models;
using InstallerStudio.ViewModels.Utils;
using InstallerStudio.WixElements;
using InstallerStudio.Utils;
using InstallerStudio.Views.Controls;

namespace InstallerStudio.ViewModels
{
  /// <summary>
  /// Данный интерфейс нужен только для определения функциональности модели представления
  /// для связи с GUI.
  /// </summary>
  interface IBuilderViewModel
  {
    bool ComponentsCategoryIsVisible { get; set; }
    IWixMainEntity MainItem { get; }
    IList<IWixElement> Items { get; }
    IWixElement SelectedItem { get; set; }
    bool IsBuilding { get; }

    /// <summary>
    /// Удаление выделенного элемента.
    /// Используется в TreeListControl в контекстном меню.
    /// </summary>
    ICommand RemoveSelectedItemCommand { get; }

    IList<BuildMessage> BuildMessages { get; }
  }

  struct FileDescription
  {
    public string FileExtension { get; set; }
    public string Description { get; set; }
  }

  abstract class BuilderViewModel : BaseViewModel, IBuilderViewModel
  {
    /// <summary>
    /// Уникальное имя транзакции для очистки Ribbon после вызова Dispose()
    /// модели представления.
    /// </summary>
    private readonly string ribbonTransactionName;
    private IRibbonCustomCategory componentsCategory;

    protected IRibbonManager RibbonManager { get; private set; }

    /// <summary>
    /// Модель архитектуры MVVM.
    /// </summary>
    protected BuilderModel Model { get; private set; }

    public BuilderViewModel(IRibbonManager ribbonManager)
    {
      ribbonTransactionName = Guid.NewGuid().ToString();

      // Начинаем транзакцию Ribbon. Удаление в Dispose()->DisposeUnmanagedResources().
      RibbonManager = ribbonManager;
      RibbonManager.BeginTransaction(ribbonTransactionName);

      // Создаем модель.
      Model = BuilderModelFactory.Create();
      // Все измененные свойства пересылаем View.
      Model.PropertyChanged += (sender, e) => { NotifyPropertyChanged(e.PropertyName); };

      CustomizeRibbon();

      // Удаление выделенного элемента.
      // Используется в TreeListControl в контекстном меню.
      // Запрещаем удаление для элеметов "только чтение".
      RemoveSelectedItemCommand = new RelayCommand(param => RemoveSelectedItem(), (obj) => { return !SelectedItem.IsReadOnly; });
    }

    #region Виртуальные и абстрактные члены.

    /// <summary>
    /// Получение фабрики модели MVVM.
    /// </summary>
    protected abstract BuilderModelFactory BuilderModelFactory { get; }

    /// <summary>
    /// Возвращает описание файла (расширение, описание и прочее).
    /// </summary>
    public abstract FileDescription FileDescription { get; }

    protected virtual void CustomizeRibbon()
    {
      componentsCategory = RibbonManager.Add("Содержание пакета");
      IRibbonPage componentsPage = componentsCategory.Add("Панель элементов");

      CommandMetadata[] commands = Model.GetElementCommands();
      foreach (IGrouping<string, CommandMetadata> group in commands.GroupBy(v => v.Group))
      {
        IRibbonGroup componentsGroup = componentsPage.Add(group.Key);
        foreach (CommandMetadata command in group)
        {
          // Создаем WPF команду и привязываем ее к кнопке.
          // Команда доступна, если выбранный элемент поддерживает текущий тип (описан в CommandMetadata)
          // и плюс частные условия реализованные в AvailableForRun. Также элемнт должен быть не "Только для чтения".
          ICommand relayCommand = new RelayCommand(param => Model.AddItem(command.WixElementType),
            delegate 
            { 
              IWixElement element = SelectedItem ?? Model.RootItem;
              return element.AvailableForRun(command.WixElementType, Model.RootItem) 
                && !element.IsReadOnly; 
            });
          componentsGroup.Add(command.Caption, relayCommand, RibbonButtonType.Small).SetImage(command.ImageType);
        }
      }
    }

    #endregion

    #region Методы для команд.

    private void RemoveSelectedItem()
    {
      Model.RemoveSelectedItem();
    }

    #endregion

    #region BaseViewModel

    protected override void DisposeManagedResources()
    {
      RibbonManager.RollbackTransaction(ribbonTransactionName);
      Model.Dispose();
      base.DisposeManagedResources();
    }

    #endregion

    #region IBuilderViewModel

    // Если убрать аксессор get, приложение завершается с ошибкой при
    // пересоздании дочернего документа.
    public bool ComponentsCategoryIsVisible
    {
      get { return componentsCategory.IsVisible; }
      set { componentsCategory.IsVisible = value; }
    }

    /// <summary>
    /// Содержит основные свойства пакета.
    /// </summary>
    public IWixMainEntity MainItem 
    {
      get { return Model.MainItem; }
    }

    public IList<IWixElement> Items
    {
      get { return Model.Items; }
    }

    public IWixElement SelectedItem 
    {
      get { return Model.SelectedItem; }
      set { Model.SelectedItem = value; }
    }

    public bool IsBuilding
    {
      get { return Model.IsBuilding; }
    }

    public ICommand RemoveSelectedItemCommand { get; private set; }

    public IList<BuildMessage> BuildMessages 
    {
      get { return Model.BuildMessages; }
    }

    #endregion

    public void Save(string fileName)
    {
      Model.Save(fileName);
    }

    public virtual void Load(string fileName)
    {
      Model.Load(fileName);
    }

    public void Build(ISettingsInfo settingsInfo)
    {
      Model.Build(settingsInfo);
    }

    public string LoadedFileName { get { return Model.LoadedFileName; } }

    public ModelState State { get { return Model.State; } }
  }

  // Паттерн "Абстрактная фабрика".
  abstract class BuilderViewModelFactory
  {
    public abstract BuilderViewModel Create(IRibbonManager ribbonManager);
  }
}

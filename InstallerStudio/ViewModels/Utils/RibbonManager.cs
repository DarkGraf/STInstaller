using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using InstallerStudio.Common;
using InstallerStudio.Views.Utils;

namespace InstallerStudio.ViewModels.Utils
{
  /// <summary>
  /// Базовый класс содержимого Ribbon.
  /// </summary>
  class RibbonItem : ChangeableObject, IRibbonItem
  {
    public RibbonItem(string caption)
    {
      Caption = caption;
    }

    public string Caption { get; private set; }
  }

  /// <summary>
  /// Представление в Ribbon кнопки.
  /// </summary>
  abstract class RibbonButton : RibbonItem, IRibbonButton
  {
    public RibbonButton(string caption, ICommand command)
      : base(caption) 
    {
      Command = command;
    }

    public ICommand Command { get; private set; }

    public ImageSource Image { get; private set; }

    public IRibbonButton SetImage(Enum type)
    {
      Image = ImageResourceFactory.CreateImageResource(type.GetType())[type];
      return this;
    }
  }

  class RibbonLargeButton : RibbonButton, IRibbonLargeButton
  {
    public RibbonLargeButton(string caption, ICommand command) : base(caption, command) { }
  }

  class RibbonSmallButton : RibbonButton, IRibbonSmallButton
  {
    public RibbonSmallButton(string caption, ICommand command) : base(caption, command) { }
  }

  /// <summary>
  /// Кнопка, с поддержкой выпадающего списка других кнопок.
  /// </summary>
  class RibbonSplitButton : RibbonButton, IRibbonSplitButton
  { 
    public RibbonSplitButton(string caption, ICommand command) : base(caption, command) 
    {
      Buttons = new ObservableCollection<IRibbonSmallButton>();
    }

    public ObservableCollection<IRibbonSmallButton> Buttons { get; private set; }

    public IRibbonSmallButton Add(string caption, ICommand command)
    {
      IRibbonSmallButton button = new RibbonSmallButton(caption, command);
      Buttons.Add(button);
      return button;
    }
  }

  class RibbonSplitLargeButton : RibbonSplitButton, IRibbonSplitLargeButton 
  {
    public RibbonSplitLargeButton(string caption, ICommand command) : base(caption, command) { }
  }

  class RibbonSplitSmallButton : RibbonSplitButton, IRibbonSplitSmallButton 
  { 
    public RibbonSplitSmallButton(string caption, ICommand command) : base(caption, command) { }
  }

  /// <summary>
  /// Поддержка элемента Ribbon транзакции.
  /// </summary>
  interface IRibbonTransactionItem
  {
    void BeginTransaction(string transactionName);

    void RollbackTransaction(string transactionName);
  }
  
  /// <summary>
  /// Базовый элемент Ribbon с возможностью содержать другие элементы Ribbon.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  class RibbonContainerItem<T> : RibbonItem, IRibbonTransactionItem
    where T : IRibbonItem
  {
    protected RibbonTransactionItemCollection<T> Items { get; private set; }

    public RibbonContainerItem(string caption) : base(caption)
    {
      Items = new RibbonTransactionItemCollection<T>();
    }

    #region IRibbonTransactionItem

    public void BeginTransaction(string transactionName)
    {
      Items.BeginTransaction(transactionName);
      // Уведомляем дочерние элементы, если они транзакционные.
      foreach (IRibbonTransactionItem child in Items.OfType<IRibbonTransactionItem>())
        child.BeginTransaction(transactionName);
    }

    public void RollbackTransaction(string transactionName)
    {
      Items.RollbackTransaction(transactionName);
      // Уведомляем дочерние элементы, если они транзакционные.
      foreach (IRibbonTransactionItem child in Items.OfType<IRibbonTransactionItem>())
        child.RollbackTransaction(transactionName);
    }

    #endregion
  }

  /// <summary>
  /// Представление в Ribbon группы.
  /// </summary>
  class RibbonGroup : RibbonContainerItem<IRibbonButton>, IRibbonGroup
  {
    public RibbonGroup(string caption) : base(caption) { }

    #region IRibbonGroup

    public ObservableCollection<IRibbonButton> Buttons { get { return Items; } }

    public IRibbonButton Add(string caption, ICommand command, RibbonButtonType type)
    {
      IRibbonButton button;
      switch (type)
      {
        case RibbonButtonType.Large:
          button = new RibbonLargeButton(caption, command);
          break;
        case RibbonButtonType.Small:
          button = new RibbonSmallButton(caption, command);
          break;
        default:
          throw new NotImplementedException();
      }

      Buttons.Add(button);
      return button;
    }

    public IRibbonSplitButton AddSplit(string caption, ICommand command, RibbonButtonType type)
    {
      IRibbonSplitButton button;
      switch (type)
      {
        case RibbonButtonType.Large:
          button = new RibbonSplitLargeButton(caption, command);
          break;
        case RibbonButtonType.Small:
          button = new RibbonSplitSmallButton(caption, command);
          break;
        default:
          throw new NotImplementedException();
      }

      Buttons.Add(button);
      return button;
    }

    #endregion
  }

  /// <summary>
  /// Представление в Ribbon страницы.
  /// </summary>
  class RibbonPage : RibbonContainerItem<IRibbonGroup>, IRibbonPage
  {
    public RibbonPage(string caption) : base(caption) { }

    #region IRibbonPage

    public ObservableCollection<IRibbonGroup> Groups { get { return Items; } }

    public IRibbonGroup Add(string caption)
    {
      IRibbonGroup group = new RibbonGroup(caption);
      Groups.Add(group);
      return group;
    }

    #endregion
  }

  /// <summary>
  /// Представление в Ribbon категории.
  /// </summary>
  abstract class RibbonCategory : RibbonContainerItem<IRibbonPage>, IRibbonCategory
  {
    public RibbonCategory(string caption) : base(caption) { }

    #region IRibbonCategory

    public ObservableCollection<IRibbonPage> Pages { get { return Items; } }

    public IRibbonPage Add(string caption)
    {
      IRibbonPage page = new RibbonPage(caption);
      Pages.Add(page);
      return page;
    }

    #endregion
  }

  /// <summary>
  /// Категория по умолчанию.
  /// </summary>
  class RibbonDefaultCategory : RibbonCategory, IRibbonDefaultCategory
  {
    public RibbonDefaultCategory(string caption) : base(caption) { }
  }

  /// <summary>
  /// Дополнительные категории.
  /// </summary>
  class RibbonCustomCategory : RibbonCategory, IRibbonCustomCategory
  {
    private bool isVisible;

    public RibbonCustomCategory(string caption) : base(caption) 
    {
      IsVisible = true;
    }
    
    public bool IsVisible 
    {
      get { return isVisible; }
      set { SetValue(ref isVisible, value); }
    }
  }

  /// <summary>
  /// Управление Ribbon.
  /// </summary>
  class RibbonManager : RibbonContainerItem<IRibbonCategory>, IRibbonManager
  {
    public RibbonManager() : base(null) 
    {      
      // Категория по умолчанию.
      DefaultCategory = new RibbonDefaultCategory("");
      Categories.Add(DefaultCategory);
    }

    #region IRibbonMananger

    public ObservableCollection<IRibbonCategory> Categories { get { return Items; } }

    /// <summary>
    /// Категория по умолчанию, создается автоматически.
    /// </summary>
    public IRibbonDefaultCategory DefaultCategory { get; private set; }

    /// <summary>
    /// Добавление дополнительной категории.
    /// </summary>
    /// <param name="caption"></param>
    /// <returns></returns>
    public IRibbonCustomCategory Add(string caption)
    {
      IRibbonCustomCategory category = new RibbonCustomCategory(caption);
      Categories.Add(category);
      return category;
    }

    #endregion
  }

  /// <summary>
  /// Коллекция для элементов Ribbon с поддержкой транзакций.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  class RibbonTransactionItemCollection<T> : ObservableCollection<T>
    where T : IRibbonItem
  {
    RibbonTransaction transaction;

    public RibbonTransactionItemCollection() : base()
    {
      transaction = new RibbonTransaction(this);
    }

    public void BeginTransaction(string transactionName)
    {
      transaction.Begin(transactionName);
    }

    public void RollbackTransaction(string transactionName)
    {
      transaction.Rollback(transactionName);
    }
  }

  /// <summary>
  /// Класс фиксирует добавление элементов в коллекцию в транзакции и удаляет их
  /// из коллекции при ее отмене.
  /// </summary>
  class RibbonTransaction
  {
    class WrongNameRibbonTransactionExistException : Exception { }

    IList collection;

    /// <summary>
    /// Соответствие имени транзакции и объектов.
    /// </summary>
    Dictionary<string, List<object>> dictionary;
    /// <summary>
    /// Стек вызовов транзакций.
    /// </summary>
    Stack<string> transactionStack;

    public RibbonTransaction(INotifyCollectionChanged collection)
    {
      dictionary = new Dictionary<string, List<object>>();
      transactionStack = new Stack<string>();

      collection.CollectionChanged += CollectionChanged;
      this.collection = (IList)collection;
    }    

    public void Begin(string transactionName)
    {
      if (string.IsNullOrEmpty(transactionName) || transactionStack.Where(v => v == transactionName).Count() > 0)
        throw new WrongNameRibbonTransactionExistException();

      transactionStack.Push(transactionName);
      dictionary.Add(transactionName, new List<object>());
    }

    public void Rollback(string transactionName)
    {
      if (transactionStack.Peek() != transactionName)
        throw new WrongNameRibbonTransactionExistException();

      // Удаляем объекты из оригинальной коллекции.
      foreach (object obj in dictionary[transactionName])
        collection.Remove(obj);

      transactionStack.Pop();
      dictionary.Remove(transactionName);
    }

    private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Add)
      {
        if (transactionStack.Count > 0)
        {
          foreach (object obj in e.NewItems)
            dictionary[transactionStack.Peek()].Add(obj);
        }
      }
    }
  }

  #region Template Selectors

  public class RibbonCategoryTemplateSelector : DataTemplateSelector
  {
    public DataTemplate DefaultCategoryTemplate { get; set; }

    public DataTemplate CustomCategoryTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is RibbonCategory)
        return item is RibbonDefaultCategory ? DefaultCategoryTemplate : CustomCategoryTemplate;
      else
        return base.SelectTemplate(item, container);
    }
  }

  public class RibbonButtonTemplateSelector : DataTemplateSelector
  {
    public DataTemplate LargeButtonTemplate { get; set; }

    public DataTemplate SmallButtonTemplate { get; set; }

    public DataTemplate SplitLargeButtonTemplate { get; set; }

    public DataTemplate SplitSmallButtonTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is RibbonButton)
      {
        if (item is RibbonSplitButton)
          return item is RibbonSplitLargeButton ? SplitLargeButtonTemplate : SplitSmallButtonTemplate;
        else
          return item is RibbonLargeButton ? LargeButtonTemplate : SmallButtonTemplate;
      }
      else
        return base.SelectTemplate(item, container);
    }
  }

  #endregion
}
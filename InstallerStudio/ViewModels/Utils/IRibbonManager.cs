using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

using InstallerStudio.Views.Utils;

namespace InstallerStudio.ViewModels.Utils
{
  enum RibbonButtonType { Large, Small }

  /// <summary>
  /// Дочерний базовый элемент Ribbon.
  /// </summary>
  interface IRibbonItem
  {
    /// <summary>
    /// Заголовок элемента.
    /// </summary>
    string Caption { get; }
  }

  interface IRibbonButton : IRibbonItem
  {
    ICommand Command { get; }

    ImageSource Image { get; }

    IRibbonButton SetImage(Enum type);
  }

  interface IRibbonLargeButton : IRibbonButton { }

  interface IRibbonSmallButton : IRibbonButton { }

  interface IRibbonSplitButton : IRibbonButton
  {
    ObservableCollection<IRibbonSmallButton> Buttons { get; }

    IRibbonSmallButton Add(string caption, ICommand command);
  }

  interface IRibbonSplitSmallButton : IRibbonSplitButton { }

  interface IRibbonSplitLargeButton : IRibbonSplitButton { }

  interface IRibbonGroup : IRibbonItem
  {
    ObservableCollection<IRibbonButton> Buttons { get; }

    IRibbonButton Add(string caption, ICommand command, RibbonButtonType type);

    IRibbonSplitButton AddSplit(string caption, ICommand command, RibbonButtonType type);
  }

  interface IRibbonPage : IRibbonItem
  {
    ObservableCollection<IRibbonGroup> Groups { get; }

    IRibbonGroup Add(string caption);
  }

  interface IRibbonCategory : IRibbonItem
  {
    ObservableCollection<IRibbonPage> Pages { get; }

    IRibbonPage Add(string caption);
  }

  interface IRibbonDefaultCategory : IRibbonCategory { }

  interface IRibbonCustomCategory : IRibbonCategory 
  {
    bool IsVisible { get; set; }
  }

  /// <summary>
  /// Управление элементами Ribbon.
  /// </summary>
  interface IRibbonManager
  {
    ObservableCollection<IRibbonCategory> Categories { get; }

    IRibbonCustomCategory Add(string caption);

    IRibbonDefaultCategory DefaultCategory { get; }

    void BeginTransaction(string transactionName);

    void RollbackTransaction(string transactionName);
  }
}

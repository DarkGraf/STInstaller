using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;

using LightServerLib.Commands;

using LightServerLib.Common;
using LightServerLib.Models;
using LightServerInstaller.Models;

namespace LightServerInstaller.ViewModels
{
  interface IMainView
  {
    /// <summary>
    /// Активизировать окно с результатами.
    /// </summary>
    void ToActivateOutSpace();
    void StartProgress();
    void StopProgress();
  }

  interface IMainViewModelListener
  {
    IMainView View { get; set; }
  }

  /// <summary>
  /// Данный интерфейс нужен только для логического определения интерфейса ViewModel.
  /// </summary>
  interface IMainViewModel
  {
    Guid ProductId { get; set; }
    Guid ProductUpgradeCode { get; set; }
    string ProductName { get; set; }
    string ProductManufacturer { get; set; }
    AppVersion ProductVersion { get; set; }

    string MdfFile { get; set; }
    string LdfFile { get; set; }
    string SpDllFile { get; set; }
    string SpIniFile { get; set; }
    string SpSqlFile { get; set; }
    string SqlFile { get; set; }
    string PluginDllFile { get; set; }

    ObservableCollection<string> Messages { get; }
  }

  class MainViewModel : NotifyObject, IMainViewModel, IMainViewModelListener
  {
    private MainModel model;

    /// <summary>
    /// Последний путь для использования в диалогах.
    /// </summary>
    private string defaultPath;
    private bool isEnabledControls;

    public ICommand CloseCommand { get; private set; }
    public ICommand FindCommand { get; private set; }
    public ICommand CreateCommand { get; private set; }
    public ICommand OpenCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand BuildCommand { get; private set; }

    public MainViewModel()
    {
      model = new MainModel();
      model.PropertyChanged += (@this, property) =>
      {
        // Если изменилось свойство "Product", то сигнализируем об изменении всех свойств.
        switch (property.PropertyName)
        {
          case "Product":
            NotifyPropertyChanged(null);
            break;
          default:
            NotifyPropertyChanged(property.PropertyName);
            break;
        }
      };

      defaultPath = Environment.CurrentDirectory;

      isEnabledControls = true;
      Predicate<object> checkIsEnabledControls = delegate { return isEnabledControls; };

      CloseCommand = new RelayCommand(param => CloseApplication(), checkIsEnabledControls);
      FindCommand = new RelayCommand(param => Find(param), checkIsEnabledControls);
      CreateCommand = new RelayCommand(param => CreateProduct(), checkIsEnabledControls);
      OpenCommand = new RelayCommand(param => OpenProduct(), checkIsEnabledControls);
      SaveCommand = new RelayCommand(param => SaveProduct(), checkIsEnabledControls);
      BuildCommand = new RelayCommand(param => Build(), checkIsEnabledControls);

      model.CreateProduct();
    }

    private void CloseApplication()
    {
      Application.Current.MainWindow.Close();
    }

    private void Find(object param)
    {
      if (!(param is string))
        return;

      string extension;
      
      new Dictionary<string, string>
      {
        { "MdfFile", "*.mdf" },
        { "LdfFile", "*.ldf" },
        { "SpDllFile", "*.dll" },
        { "SpIniFile", "*.ini" },
        { "SpSqlFile", "*.sql" },
        { "SqlFile", "*.sql" },
        { "PluginDllFile", "*.dll" }
      }.TryGetValue((string)param, out extension);

      if (extension == null)
        return;

      // Получаем путь к файлу соответствующий выбранной кнопки.
      PropertyInfo prop = GetType().GetProperty((string)param);
      string path = (string)prop.GetValue(this, null);

      OpenFileDialog dialog = new OpenFileDialog();
      // Если путь уже выбран, то используем его, иначе используем путь по умолчанию.
      dialog.InitialDirectory = string.IsNullOrWhiteSpace(path) ? defaultPath : Path.GetDirectoryName(path);
      // Если путь был относительный, то преобразуется в абсолютный.
      dialog.InitialDirectory = Path.GetFullPath(dialog.InitialDirectory);
      dialog.Filter = string.Format("{0}|{0}|*.*|*.*", extension);
      if (dialog.ShowDialog() == true)
      {        
        // Преобразуем абсолютный путь в относительный.
        Uri appPath = new Uri(AppDomain.CurrentDomain.BaseDirectory);
        Uri filePath = new Uri(dialog.FileName);
        string relativePath = appPath.MakeRelativeUri(filePath).ToString();

        prop.SetValue(this, relativePath);
        defaultPath = Path.GetDirectoryName(relativePath);
        defaultPath = Path.GetFullPath(defaultPath);

        NotifyPropertyChanged(prop.Name);
      }
    }

    private void CreateProduct()
    {
      model.CreateProduct();
    }

    private void OpenProduct()
    {
      OpenFileDialog dialog = new OpenFileDialog();
      dialog.Filter = "*.xml|*.xml";
      dialog.InitialDirectory = defaultPath;
      if (dialog.ShowDialog() == true)
      {
        model.LoadFromFile(dialog.FileName);
        defaultPath = Path.GetDirectoryName(dialog.FileName);
      }
    }

    private void SaveProduct()
    {
      SaveFileDialog dialog = new SaveFileDialog();
      dialog.Filter = "*.xml|*.xml";
      dialog.InitialDirectory = defaultPath;
      if (dialog.ShowDialog() == true)
      {
        model.SaveToFile(dialog.FileName);
        defaultPath = Path.GetDirectoryName(dialog.FileName);
      }
    }

    private void Build()
    {
      View.StartProgress();
      View.ToActivateOutSpace();
      isEnabledControls = false;

      Task.Factory.StartNew(delegate
      {
        model.Build();
        isEnabledControls = true;
      }).ContinueWith(
        delegate
        {
          View.StopProgress();
          CommandManager.InvalidateRequerySuggested();
        }, 
        CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
    }

    #region IMainViewModel

    public Guid ProductId
    {
      get { return model.Product.Id; }
      set { model.Product.Id = value; }
    }

    public Guid ProductUpgradeCode
    {
      get { return model.Product.UpgradeCode; }
      set { model.Product.UpgradeCode = value; }
    }

    public string ProductName
    {
      get { return model.Product.Name; }
      set { model.Product.Name = value; }
    }

    public string ProductManufacturer
    {
      get { return model.Product.Manufacturer; }
      set { model.Product.Manufacturer = value; }
    }

    public AppVersion ProductVersion
    {
      get { return model.Product.Version; }
      set { model.Product.Version = value; }
    }

    public string MdfFile
    {
      get { return model.Product.MdfFile; }
      set { model.Product.MdfFile = value; }
    }

    public string LdfFile
    {
      get { return model.Product.LdfFile; }
      set { model.Product.LdfFile = value; }
    }

    public string SpDllFile
    {
      get { return model.Product.SpDllFile; }
      set { model.Product.SpDllFile = value; }
    }

    public string SpIniFile
    {
      get { return model.Product.SpIniFile; }
      set { model.Product.SpIniFile = value; }
    }

    public string SpSqlFile
    {
      get { return model.Product.SpSqlFile; }
      set { model.Product.SpSqlFile = value; }
    }

    public string SqlFile
    {
      get { return model.Product.SqlFile; }
      set { model.Product.SqlFile = value; }
    }

    public string PluginDllFile
    {
      get { return model.Product.PluginDllFile; }
      set { model.Product.PluginDllFile = value; }
    }

    public ObservableCollection<string> Messages
    {
      get { return model.Messages; }
    }

    #endregion

    #region IMainViewModelListener

    public IMainView View { get; set; }

    #endregion
  }
}

using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;

using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;
using InstallerStudio.Utils;

namespace InstallerStudio.ViewModels
{
  /// <summary>
  /// Данный интерфейс нужен только для определения функциональности модели представления
  /// для связи с GUI.
  /// </summary>
  interface IMainViewModel
  {
    string ApplicationTitle { get; }

    BuilderViewModel BuilderViewModel { get; }
    bool IsBuilding { get; }

    ICommand CreateMsiCommand { get; }
    ICommand CreateMspCommand { get; }
    ICommand OpenCommand { get; }
    ICommand SaveCommand { get; }
    ICommand SaveAsCommand { get; }
    ICommand CloseCommand { get; }
    ICommand SettingsCommand { get; }
    ICommand CheckCommand { get; }
    ICommand BuildCommand { get; }
    IRibbonManager RibbonManager { get; }
  }

  enum CreatedBuilderType
  {
    Msi,
    Msp
  }

  class MainViewModel : BaseViewModel, IMainViewModel, IDialogServiceSetter
  {
    BuilderViewModel builderViewModel;
    /// <summary>
    /// Настройки программы.
    /// Загружаем в конструкторе, сохраняем после изменения настроек.
    /// </summary>
    ISettingsInfo settingsInfo;

    public MainViewModel()
    {
      // Получение исполняемой сборки для обращения к атрибутам.
      Assembly assembly = Assembly.GetExecutingAssembly();

      // Получение заголовка программы.
      AssemblyTitleAttribute title = (AssemblyTitleAttribute)assembly.
        GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];
      ApplicationTitle = title.Title;

      // Получение директории для настроек.
      AssemblyProductAttribute product = (AssemblyProductAttribute)assembly.
        GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
      SettingsManager.Directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ST", product.Product);
      settingsInfo = new SettingsManager().Load();

      Predicate<object> canExecute = (obj) => { return BuilderViewModel != null; };

      CreateMsiCommand = new RelayCommand(param => CreateBuilder(CreatedBuilderType.Msi));
      CreateMspCommand = new RelayCommand(param => CreateBuilder(CreatedBuilderType.Msp));
      OpenCommand = new RelayCommand(param => Open());
      SaveCommand = new RelayCommand(param => Save(true), canExecute);
      SaveAsCommand = new RelayCommand(param => Save(false));
      CloseCommand = new RelayCommand(param => Close());
      SettingsCommand = new RelayCommand(param => ChangeSettings());
      CheckCommand = new RelayCommand(param => Check(), canExecute);
      BuildCommand = new RelayCommand(param => Build(), canExecute);

      CreateRibbon();
    }

    #region IMainViewModel

    public string ApplicationTitle { get; private set; }

    public BuilderViewModel BuilderViewModel 
    {
      get { return builderViewModel; }
      private set { SetValue(ref builderViewModel, value); }
    }

    public bool IsBuilding
    {
      get { return builderViewModel == null ? false : builderViewModel.IsBuilding; }
    }

    public ICommand CreateMsiCommand { get; private set; }
    public ICommand CreateMspCommand { get; private set; }
    public ICommand OpenCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand SaveAsCommand { get; private set; }
    public ICommand CloseCommand { get; private set; }
    public ICommand SettingsCommand { get; private set; }
    public ICommand CheckCommand { get; private set; }
    public ICommand BuildCommand { get; private set; }

    public IRibbonManager RibbonManager { get; private set; }

    #endregion

    private void CreateRibbon()
    {
      RibbonManager = new RibbonManager();
      IRibbonPage pageMain = RibbonManager.DefaultCategory.Add("Главная");
      
      // Не работает дизайнер с привязкой со split-кнопками. В run-time все впорядке.
      // Проблема изветная в DevExpress, они ссылаются на внутреннюю работу Visual Studio:
      // https://www.devexpress.com/Support/Center/Question/Details/B158154
      // https://www.devexpress.com/Support/Center/Question/Details/Q451319
      // https://www.devexpress.com/Support/Center/Question/Details/T164384
      // В последней ссылке есть решение проблемы с помощью сеттеров.
      // Пока не будем создавать в design-time эти кнопки.
#if DEBUG
      if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
#endif
      {
        IRibbonGroup groupFile = pageMain.Add("Файл");

        IRibbonSplitButton btnNew = groupFile.AddSplit("Создать MSI", CreateMsiCommand, RibbonButtonType.Large);
        btnNew.SetImage(MenusImagesTypes.New);
        btnNew.Add("Создать MSI", CreateMsiCommand).SetImage(MenusImagesTypes.New);
        btnNew.Add("Создать MSP", CreateMspCommand).SetImage(MenusImagesTypes.New);

        groupFile.Add("Открыть", OpenCommand, RibbonButtonType.Large).SetImage(MenusImagesTypes.Open);

        IRibbonSplitButton btnSave = groupFile.AddSplit("Сохранить", SaveCommand, RibbonButtonType.Large);
        btnSave.SetImage(MenusImagesTypes.Save);
        btnSave.Add("Сохранить", SaveCommand).SetImage(MenusImagesTypes.Save);
        btnSave.Add("Сохранить как...", SaveAsCommand).SetImage(MenusImagesTypes.Save);
      }

      IRibbonGroup groupAssembly = pageMain.Add("Сборка");
      groupAssembly.Add("Проверить", CheckCommand, RibbonButtonType.Large).SetImage(MenusImagesTypes.Check);
      groupAssembly.Add("Построить", BuildCommand, RibbonButtonType.Large).SetImage(MenusImagesTypes.Build);
    }

    private void DisposeBuilderViewModel()
    {
      if (BuilderViewModel != null)
        BuilderViewModel.Dispose();
    }

    #region Методы для команд.

    private void CreateBuilder(CreatedBuilderType type)
    {
      DisposeBuilderViewModel();
      switch (type)
      { 
        case CreatedBuilderType.Msi:
          BuilderViewModel = new MsiViewModelFactory().Create(RibbonManager);
          break;
        case CreatedBuilderType.Msp:
          BuilderViewModel = new MsiViewModelFactory().Create(RibbonManager);
          break;
        default:
          BuilderViewModel = null;
          break;
      }

      if (BuilderViewModel != null)
      {
        // Подписываемся на уведомление о построении.
        BuilderViewModel.PropertyChanged += (s, e) =>
        {
          if (e.PropertyName == "IsBuilding")
            NotifyPropertyChanged("IsBuilding");
        };
      }
    }

    private void Open()
    {
      IOpenSaveFileDialog dialog = DialogService.OpenFileDialog;
      dialog.Filter = "Msi Zip (*.msizip)|*.msizip|Msp Zip (*.mspzip)|*.mspzip|All Files (*.*)|*.*";
      dialog.FilterIndex = 1;
      dialog.Title = "Открытие";
      dialog.FileName = "";
      if (dialog.Show().GetValueOrDefault())
      {
        // Диспетчер открываемых файлов.
        switch (Path.GetExtension(dialog.FileName))
        {
          case ".msizip":
            CreateBuilder(CreatedBuilderType.Msi);
            BuilderViewModel.Load(dialog.FileName);
            break;
          case ".mspzip":
            CreateBuilder(CreatedBuilderType.Msp);
            BuilderViewModel.Load(dialog.FileName);
            break;
        }
      }
    }

    private void Save(bool withoutQuery)
    {
      IOpenSaveFileDialog dialog = DialogService.SaveFileDialog;
      FileDescription fileDescription = BuilderViewModel.FileDescription;
      dialog.Filter = string.Format("{0} (*.{1})|*.{1}", fileDescription.Description, fileDescription.FileExtension);
      dialog.FilterIndex = 1;
      dialog.Title = "Сохранение";
      dialog.FileName = "Default";
      if (dialog.Show().GetValueOrDefault())
      {
        BuilderViewModel.Save(dialog.FileName);
      }
    }

    private void Close()
    {
    }

    private void ChangeSettings()
    {
      ISettingsDialog dialog = DialogService.SettingsDialog;
      dialog.WixToolsetPath = settingsInfo.WixToolsetPath;
      dialog.CandleFileName = settingsInfo.CandleFileName;
      dialog.LightFileName = settingsInfo.LightFileName;
      dialog.UIExtensionFileName = settingsInfo.UIExtensionFileName;
      if (dialog.Show().GetValueOrDefault())
      {
        settingsInfo.WixToolsetPath = dialog.WixToolsetPath;
        settingsInfo.CandleFileName = dialog.CandleFileName;
        settingsInfo.LightFileName = dialog.LightFileName;
        settingsInfo.UIExtensionFileName = dialog.UIExtensionFileName;
        new SettingsManager().Save(settingsInfo);
      }
    }

    private void Check()
    {

    }

    private void Build()
    {
      builderViewModel.Build(settingsInfo);
    }

    #endregion

    #region IDialogServiceSetter

    public IDialogService DialogService { get; set; }

    #endregion
  }
}

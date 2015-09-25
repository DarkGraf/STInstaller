using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;

using InstallerStudio.ViewModels.Utils;
using InstallerStudio.Views.Utils;
using InstallerStudio.Utils;
using InstallerStudio.Models;
using InstallerStudio.WixElements;

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
    ICommand ExitCommand { get; }
    ICommand SettingsCommand { get; }
    ICommand CheckCommand { get; }
    ICommand BuildCommand { get; }
    IRibbonManager RibbonManager { get; }
  }

  /// <summary>
  /// Интерфейс для связи в View.
  /// </summary>
  interface IMainView
  {
    /// <summary>
    /// Закрыть View.
    /// </summary>
    void Close();
    /// <summary>
    /// Возвращает строковый массив, содержащий аргументы командной строки.
    /// </summary>
    string[] CommandLineArgs { get; }
    /// <summary>
    ///  Возвращает директорию запуска приложения.
    /// </summary>
    string ApplicationDirectory { get; }
    /// <summary>
    /// Завершает редактирование в элементах управления.
    /// </summary>
    void EditEnd();
  }

  interface IMainViewSetter
  {
    IMainView MainView { get; set; }
    /// <summary>
    /// Вызывается во View в конце работы конструктора.
    /// </summary>
    void ViewInitialized();
  }

  enum CreatedBuilderType
  {
    Msi,
    Msp
  }

  class ApplicationInfo : IApplicationInfo
  {
    public ApplicationInfo(string applicationDirectory)
    {
      ApplicationDirectory = applicationDirectory;
    }

    public string ApplicationDirectory { get; private set; }
  }

  class MainViewModel : BaseViewModel, IMainViewModel, IDialogServiceSetter, IMainViewSetter
  {
    /// <summary>
    /// Базовый заголовок программы.
    /// </summary>
    private readonly string applicationTitleBase;

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
      applicationTitleBase = title.Title;

      // Получение директории для настроек.
      AssemblyProductAttribute product = (AssemblyProductAttribute)assembly.
        GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
      SettingsManager.Directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ST", product.Product);
      settingsInfo = new SettingsManager().Load();

      Predicate<object> canExecute = (obj) => { return BuilderViewModel != null; };

      CreateMsiCommand = new RelayCommand(param => CreateBuilder(CreatedBuilderType.Msi, true));
      CreateMspCommand = new RelayCommand(param => CreateBuilder(CreatedBuilderType.Msp, true));
      OpenCommand = new RelayCommand(param => Open());
      SaveCommand = new RelayCommand(param => Save(true), canExecute);
      SaveAsCommand = new RelayCommand(param => Save(false), canExecute);
      CloseCommand = new RelayCommand(param => Close(), canExecute);
      ExitCommand = new RelayCommand(param => Exit());
      SettingsCommand = new RelayCommand(param => ChangeSettings());
      CheckCommand = new RelayCommand(param => Build(true), canExecute);
      BuildCommand = new RelayCommand(param => Build(false), canExecute);

      CreateRibbon();
    }

    #region IMainViewModel

    public string ApplicationTitle
    {
      get
      {
        string postfix;
        // Если модель не создана, то показываем заголовок без названия файла.
        // Если модель создана и имя файла в ней определено, то показываем
        // с этим именем. Если имя неопределено, покажем "*** Без названия ***".
        // Имя файла может измениться: при создании нового, при открытии при сохранении 
        // и при закрытии документа.
        if (BuilderViewModel == null)
          postfix = "";
        else
          postfix = " - " + (Path.GetFileName(BuilderViewModel.LoadedFileName) ?? "*** Без названия ***");
        return applicationTitleBase + postfix;
      }
      // Если нет сеттера, то в некоторых ситуациях заголовок не обновляется,
      // например: создать msi, сохранить, создать msi, открыть сохраненную.
      // Возможно ошибка DevExpress. На формуме информации не найдено.
      // Также в свойстве привязке установлено Mode=TwoWay.
      set { }
    }

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
    public ICommand ExitCommand { get; private set; }
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

    private void CreateBuilder(CreatedBuilderType type, bool isNew)
    {
      DisposeBuilderViewModel();
      BuilderViewModel = null;
      switch (type)
      { 
        case CreatedBuilderType.Msi:
          BuilderViewModel = new MsiViewModelFactory().Create(RibbonManager);
          break;
        case CreatedBuilderType.Msp:
          // Если флаг isNew установлен, то это создание нового, иначе открытие.
          // Показываем диалог для выбора параметров для заполенния модели.
          if (isNew)
          {
            IMspWizardDialog dialog = DialogService.MspWizardDialog;
            if (dialog.Show().GetValueOrDefault())
            {
              // Для разделения модели и представления реализованы два типа перечисления
              // с одинаковыми по смыслу элементами.
              MspCreationTypes creationtype = (MspCreationTypes)Enum.Parse(typeof(MspCreationTypes), dialog.ContentType.ToString());

              IMspModelLoadingParamters parameters = new MspModelLoadingParameters(dialog.PathToBaseSource, dialog.PathToTargetSource, creationtype);
              BuilderViewModel = new MspViewModelFactory().Create(RibbonManager);
              ((MspViewModel)BuilderViewModel).Load(parameters);
            }
          }
          else
          {
            BuilderViewModel = new MspViewModelFactory().Create(RibbonManager);
          }
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

      // Чтобы не произошло, перегрузим заголовок программы.
      NotifyPropertyChanged("ApplicationTitle");
    }

    private void InternalOpen(string fileName)
    {
      // Диспетчер открываемых файлов.
      switch (Path.GetExtension(fileName))
      {
        case ".msizip":
          CreateBuilder(CreatedBuilderType.Msi, false);
          BuilderViewModel.Load(fileName);
          break;
        case ".mspzip":
          CreateBuilder(CreatedBuilderType.Msp, false);
          BuilderViewModel.Load(fileName);
          break;
      }

      // Чтобы не произошло, перегрузим заголовок программы.
      NotifyPropertyChanged("ApplicationTitle");
    }

    private void Open()
    {
      // Если BuilderViewModel нулевой или открыт Msi, то фильтр формируем в
      // следующем порядке: Msi, Msp, All.
      // Если открыт Msp - Msp, Msi, All.
      IOpenSaveFileDialog dialog = DialogService.OpenFileDialog;
      dialog.Filter = BuilderViewModel == null || BuilderViewModel is MsiViewModel ? 
        "Msi Zip (*.msizip)|*.msizip|Msp Zip (*.mspzip)|*.mspzip|All Files (*.*)|*.*" :
        "Msp Zip (*.mspzip)|*.mspzip|Msi Zip (*.msizip)|*.msizip|All Files (*.*)|*.*";
      dialog.FilterIndex = 1;
      dialog.Title = "Открытие";
      dialog.FileName = "";
      if (dialog.Show().GetValueOrDefault())
      {
        InternalOpen(dialog.FileName);
      }
    }

    private void Save(bool withoutQuery)
    {
      // Обновим привязки.
      MainView.EditEnd();

      if (withoutQuery && BuilderViewModel.LoadedFileName != null)
        BuilderViewModel.Save(BuilderViewModel.LoadedFileName);
      else
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

      // Чтобы не произошло, перегрузим заголовок программы.
      NotifyPropertyChanged("ApplicationTitle");
    }

    private void Close()
    {
      DisposeBuilderViewModel();
      BuilderViewModel = null;
      // Перегнузим заголовок программы.
      NotifyPropertyChanged("ApplicationTitle");
    }

    private void Exit()
    {
      MainView.Close();
    }

    private void ChangeSettings()
    {
      ISettingsDialog dialog = DialogService.SettingsDialog;
      SettingsInfoCopier.Copy(dialog, settingsInfo);
      if (dialog.Show().GetValueOrDefault())
      {
        SettingsInfoCopier.Copy(settingsInfo, dialog);
        new SettingsManager().Save(settingsInfo);
      }
    }

    private void Build(bool onlyCheck)
    {
      ApplicationInfo applicationInfo = new ApplicationInfo(MainView.ApplicationDirectory);
      builderViewModel.Build(settingsInfo, applicationInfo, onlyCheck);
    }

    #endregion

    #region IDialogServiceSetter

    public IDialogService DialogService { get; set; }

    #endregion

    #region IMainViewSetter

    public IMainView MainView { get; set; }

    public void ViewInitialized()
    {
      // Если указаны дополнительные агрументы.
      // Проверим существование файла.
      if (MainView.CommandLineArgs.Length > 1)
      {
        string fileName = MainView.CommandLineArgs[1];
        if (File.Exists(fileName))
        {
          // Возьмем второй аргумент и откроем.
          InternalOpen(fileName);
        }
        else
        {
          IMessageBoxDialog dialog = DialogService.MessageBoxInfo;
          dialog.Type = MessageBoxDialogTypes.Exclamation;
          dialog.Message = string.Format("Не найден файл \"{0}\" или к нему нет доступа.", fileName);
          dialog.Show();
        }
      }
    }

    #endregion
  }
}

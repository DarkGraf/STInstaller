using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;

using InstallerStudio.ViewModels;
using InstallerStudio.ViewModels.Utils;

namespace InstallerStudioTest.ViewModels
{
  [TestClass]
  public class MainViewModelTest
  {
    IDisposable disposable;

    [TestInitialize]
    public void TestInitialize()
    {
      disposable = ShimsContext.Create();
      // Используется при создании модели при загрузке ресурсов.
      // Также используется при загрузке параметров внутри DataContractSerializer.ReadObject,
      // поэтому будем создавать реальный объект, кроме тех случаев
      // где uri начинается для загрузки ресурсов.
      System.Fakes.ShimUri.ConstructorString = (@this, uriString) =>
      {
        ShimsContext.ExecuteWithoutShims(() =>
        {
          if (!uriString.StartsWith("pack://application"))
          {
            ConstructorInfo ctor = typeof(Uri).GetConstructor(new Type[] { typeof(string) });
            ctor.Invoke(@this, new object[] { uriString });
          }
        });
      };
    }

    [TestCleanup]
    public void TestCleanup()
    {
      disposable.Dispose();
    }

    class TestDialogService : IDialogService
    {
      public static string TestFileName;

      OpenSaveFileDialog fileDialog = new OpenSaveFileDialog();

      class OpenSaveFileDialog : IOpenSaveFileDialog
      {
        public string Filter { get; set; }

        public int FilterIndex { get; set; }

        public string Title { get; set; }

        public string FileName { get; set; }

        public bool? Show()
        {
          FileName = TestFileName;
          return true;
        }
      }

      class MessageBoxInfoDialog : IMessageBoxDialog
      {
        public MessageBoxDialogTypes Type { get; set; }

        public string Message { get; set; }

        public bool? Show() 
        {
          return true;
        }
      }

      public IOpenSaveFileDialog OpenFileDialog
      {
        get { return fileDialog; }
      }

      public IOpenSaveFileDialog SaveFileDialog
      {
        get { return fileDialog; }
      }

      public IMessageBoxDialog MessageBoxInfo
      {
        get { return new MessageBoxInfoDialog(); }
      }

      #region Не используется.

      public ISettingsDialog SettingsDialog
      {
        get { throw new NotImplementedException(); }
      }

      public IMspWizardDialog MspWizardDialog
      {
        get { throw new NotImplementedException(); }
      }

      #endregion
    }

    class TestMainView : IMainView
    {
      public TestMainView(params string[] commandLineArgs)
      {
        CommandLineArgs = commandLineArgs;
      }

      public void Close() { }

      public void EditEnd() { }

      public string[] CommandLineArgs { get; private set; }

      public string ApplicationDirectory  
      {
        get { throw new NotImplementedException(); }
      }
    }

    [TestMethod]
    public void MainViewModelApplicationTitle()
    {
      string expectedApplicationTitle = "Installer Studio";
      string notifyApplicationTitle = null;

      TestDialogService dialogService = new TestDialogService();
      MainViewModel model = new MainViewModel();
      model.DialogService = dialogService;
      // Нужно для сохранения.
      model.MainView = new TestMainView(null);

      // При загрузке модели должен быть обычный заголовок без файла.
      Assert.AreEqual(expectedApplicationTitle, model.ApplicationTitle);
      model.PropertyChanged += (s, e) =>
      {
        if (e.PropertyName == "ApplicationTitle")
          notifyApplicationTitle = model.ApplicationTitle;
      };

      // Создаем новый документ.
      model.CreateMsiCommand.Execute(null);
      Assert.AreEqual(expectedApplicationTitle + " - *** Без названия ***", notifyApplicationTitle);

      // Сохраним.
      TestDialogService.TestFileName = "Test.msizip";
      model.SaveAsCommand.Execute(null);
      Assert.AreEqual(expectedApplicationTitle + " - Test.msizip", notifyApplicationTitle);

      // Переименуем файл и откроем его.
      if (File.Exists("NewTest.msizip"))
        File.Delete("NewTest.msizip");
      File.Move("Test.msizip", "NewTest.msizip");
      TestDialogService.TestFileName = "NewTest.msizip";
      model.OpenCommand.Execute(null);
      Assert.AreEqual(expectedApplicationTitle + " - NewTest.msizip", notifyApplicationTitle);

      // Закроем документ.
      model.CloseCommand.Execute(null);
      Assert.AreEqual(expectedApplicationTitle, notifyApplicationTitle);

      if (File.Exists("NewTest.msizip"))
        File.Delete("NewTest.msizip");
    }

    /// <summary>
    /// Открытие файла через Command Line.
    /// </summary>
    [TestMethod]
    public void MainViewModelOpenFileFromCommandLine()
    {
      // Создаем модель и необходимую инфраструктуру.
      TestDialogService dialogService = new TestDialogService();
      MainViewModel model = new MainViewModel();
      model.DialogService = dialogService;
      model.MainView = new TestMainView("C:\\InstallerStudio.exe");
      // Вызовем инициализацию.
      model.ViewInitialized();
      // Ни чего не должно открыться.
      Assert.IsNull(model.BuilderViewModel);

      // Создадим файл.
      model.CreateMsiCommand.Execute(null);
      // Сохраним.
      TestDialogService.TestFileName = "Test.msizip";
      model.SaveAsCommand.Execute(null);

      // Создадим новую модель.
      model = new MainViewModel();
      model.DialogService = dialogService;
      model.MainView = new TestMainView("C:\\InstallerStudio.exe", "Test.msizip");
      // Вызовем инициализацию.
      model.ViewInitialized();
      // Должен открыться файл.
      Assert.IsNotNull(model.BuilderViewModel);

      // Создадим новую модель.
      model = new MainViewModel();
      model.DialogService = dialogService;
      model.MainView = new TestMainView("C:\\InstallerStudio.exe", "Errror.msizip");
      // Вызовем инициализацию.
      model.ViewInitialized();
      // Ни чего не должно открыться.
      Assert.IsNull(model.BuilderViewModel);

      if (File.Exists("Test.msizip"))
        File.Delete("Test.msizip");
    }
  }
}

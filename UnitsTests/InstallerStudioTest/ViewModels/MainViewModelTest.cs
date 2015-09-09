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

      public IOpenSaveFileDialog OpenFileDialog
      {
        get { return fileDialog; }
      }

      public IOpenSaveFileDialog SaveFileDialog
      {
        get { return fileDialog; }
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

    [TestMethod]
    public void MainViewModelApplicationTitle()
    {
      using (ShimsContext.Create())
      {
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

        string expectedApplicationTitle = "Installer Studio";
        string notifyApplicationTitle = null;

        TestDialogService dialogService = new TestDialogService();
        MainViewModel model = new MainViewModel();        
        model.DialogService = dialogService;

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
    }
  }
}

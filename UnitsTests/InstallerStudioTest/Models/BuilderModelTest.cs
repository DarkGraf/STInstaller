using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Models;
using InstallerStudio.WixElements;
using InstallerStudio.Common;

namespace InstallerStudioTest.Models
{
  /************************************************************************************
                                                           Изменение()
                                                      ┌───────────────┐
                                    Сохранение()      ▼                 │
     Создание() ───► ModelState.New ───────► ModelState.Saved    ModelState.Changed
                                                 ▲    │                 ▲
                                                 │    └───────────────┘
                                             Открытие()    Сохранение()
   ************************************************************************************/

  [TestClass]
  public class BuilderModelTest
  {
    #region Тестовые элементы.

    [DataContract]
    class TestElement : WixElementBase
    {
      public override InstallerStudio.Views.Utils.ElementsImagesTypes ImageType
      {
        get { throw new NotImplementedException(); }
      }

      public override string ShortTypeName
      {
        get { throw new NotImplementedException(); }
      }
    }

    [DataContract]
    [KnownType(typeof(TestElement))]
    class TestProduct : ChangeableObject, IWixMainEntity
    {
      string id;
      IWixElement element = new TestElement();

      [DataMember]
      public IWixElement RootElement
      {
        get { return element; }
        set { element = value; }
      }

      // Для тестирования изменения поля главной сущности.
      [DataMember]
      public string Id
      {
        get { return id; }
        set { SetValue(ref id, value); }
      }

      public void Build(IBuildContext buildContext)
      {
        throw new NotImplementedException();
      }
    }

    class TestModel : BuilderModel
    {
      TestProduct product = new TestProduct();

      protected override IWixMainEntity CreateMainEntity()
      {
        return product;
      }

      public override CommandMetadata[] GetElementCommands()
      {
        throw new NotImplementedException();
      }
    }

    #endregion

    string fileName = "Test";

    [TestCleanup]
    public void TestCleanup()
    {
      if (File.Exists(fileName))
        File.Delete(fileName);
    }

    /// <summary>
    /// Тест создания и сохранения модели.
    /// </summary>
    [TestMethod]
    public void BuilderModelNewSaveAndChangeState()
    {
      // Создадим и сохраним.
      TestModel model = new TestModel();
      Assert.AreEqual(ModelState.New, model.State);
      Assert.IsNull(model.LoadedFileName);
      model.Save(fileName);
      Assert.AreEqual(ModelState.Saved, model.State);
      Assert.AreEqual(fileName, model.LoadedFileName);

      // Загрузим существующую.
      model = new TestModel();
      model.Load(fileName);
      Assert.AreEqual(ModelState.Saved, model.State);
      Assert.AreEqual(fileName, model.LoadedFileName);

      // Изменим поле главной сущности и сохраним.
      ((TestProduct)model.MainItem).Id = "777";
      Assert.AreEqual(ModelState.Changed, model.State);
      Assert.AreEqual(fileName, model.LoadedFileName);
      model.Save(fileName);
      Assert.AreEqual(ModelState.Saved, model.State);
      Assert.AreEqual(fileName, model.LoadedFileName);
    }

    [TestMethod]
    public void BuilderModelLoadAndChangeState()
    {
      // Создадим и изменим.
      TestModel model = new TestModel();
      ((TestProduct)model.MainItem).Id = "777";
      Assert.AreEqual(ModelState.New, model.State);
      Assert.IsNull(model.LoadedFileName);
    }
  }
}

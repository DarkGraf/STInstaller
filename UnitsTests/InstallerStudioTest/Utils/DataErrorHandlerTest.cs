using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Utils;

namespace InstallerStudioTest.Utils
{
  [TestClass]
  public class DataErrorHandlerTest
  {
    #region Тестовые классы.

    class TestDataErrorInfoWithoutAttributes { }

    class TestDataErrorInfoBase : IDataErrorInfo
    {
      readonly IDataErrorHandler errorHandler;

      public TestDataErrorInfoBase()
      {
        errorHandler = new DataErrorHandler(this);
      }

      #region IDataErrorInfo

      public string Error
      {
        get { return string.Empty; }
      }

      public string this[string columnName]
      {
        get { return errorHandler.Check(columnName); }
      }

      #endregion
    }

    class TestDataErrorInfo : TestDataErrorInfoBase
    {
      public const string Id2Error = "Идентификатор не должен быть пустым.";

      [CheckingRequired]
      public string Id1 { get; set; }
      [CheckingRequired(Id2Error)]
      public string Id2 { get; set; }
    }

    class TestDataErrorInfoDerived : TestDataErrorInfo { }

    class TestDataFromGroup : TestDataErrorInfoBase
    {
      public const string Error = "Необходимо выбрать хотя бы один параметр из группы BA.";

      [CheckingFromGroup(Error, "BA")]
      public bool BA1 { get; set; }
      [CheckingFromGroup(Error, "BA")]
      public bool BA2 { get; set; }
      [CheckingFromGroup(Error, "BA")]
      public bool BA3 { get; set; }

      [CheckingFromGroup("BB")]
      public int BB1 { get; set; }
      [CheckingFromGroup("BB")]
      public string BB2 { get; set; }
      [CheckingFromGroup("BB")]
      public object BB3 { get; set; }
    }

    #endregion

    /// <summary>
    /// Тестирование класса без реализации интерфейса IDataErrorInfo.
    /// </summary>
    [TestMethod]
    public void DataErrorHandlerTestWithoutDataErrorInfo()
    {
      TestDataErrorInfoWithoutAttributes obj = new TestDataErrorInfoWithoutAttributes();
      DataErrorHandler handler = new DataErrorHandler(obj);
      // Передадим не существующее поле.
      Assert.AreEqual(string.Empty, handler.Check("Id"));
    }

    /// <summary>
    /// Проверка атрибута требующего не пустое значение.
    /// </summary>
    [TestMethod]
    public void DataErrorHandlerCheckingRequired()
    {
      TestDataErrorInfo obj = new TestDataErrorInfo();
      // Передадим не существующее поле.
      Assert.AreEqual(string.Empty, obj["Id"]);
      // Поля нулевые, проверим.
      Assert.AreEqual(string.Format(CheckingRequiredAttribute.DefaultError, "Id1"), obj["Id1"]);
      Assert.AreEqual(TestDataErrorInfo.Id2Error, obj["Id2"]);

      // Сделаем поля пустые, проверим.
      obj.Id1 = "";
      obj.Id2 = "";
      Assert.AreEqual(string.Format(CheckingRequiredAttribute.DefaultError, "Id1"), obj["Id1"]);
      Assert.AreEqual(TestDataErrorInfo.Id2Error, obj["Id2"]);

      // Заполним поля, проверим.
      obj.Id1 = "qwerty";
      obj.Id2 = "qwerty";
      Assert.AreEqual(string.Empty, obj["Id1"]);
      Assert.AreEqual(string.Empty, obj["Id2"]);

      // Проверим производный класс.
      obj = new TestDataErrorInfoDerived();
      // Поля нулевые, проверим.
      Assert.AreEqual(string.Format(CheckingRequiredAttribute.DefaultError, "Id1"), obj["Id1"]);
    }

    /// <summary>
    /// Проверка атрибута требующего выбора одного элемента из группы.
    /// </summary>
    [TestMethod]
    public void DataErrorHandlerCheckingFromGroup()
    {
      TestDataFromGroup obj = new TestDataFromGroup();

      // Проверим не существующее свойство.
      Assert.AreEqual(string.Empty, obj["Nothing"]);

      // Проверим не установленные свойства.
      Assert.AreEqual(TestDataFromGroup.Error, obj["BA1"]);

      // Установим одно свойство.
      obj.BA1 = true;
      Assert.AreEqual(string.Empty, obj["BA1"]);

      // Проверим работу по группе: установим элемент из другой группы.
      obj.BA1 = false;
      obj.BB1 = 1;
      Assert.AreEqual(TestDataFromGroup.Error, obj["BA1"]);

      // Проверим группу с разными типами, целое установлено выше.
      Assert.AreEqual(string.Empty, obj["BB1"]);

      // Сбросим целое, установим строку.
      obj.BB1 = 0;
      obj.BB2 = "Test";
      Assert.AreEqual(string.Empty, obj["BB1"]);

      // Сбросим все.
      obj.BB2 = null;
      Assert.AreEqual(CheckingFromGroup.DefaultError, obj["BB1"]);

      // Проверим частный случай для строки.
      obj.BB2 = "";
      Assert.AreEqual(CheckingFromGroup.DefaultError, obj["BB2"]);

      // Проверим ссылочный тип.
      obj.BB3 = new object();
      Assert.AreEqual(string.Empty, obj["BB3"]);
    }
  }
}

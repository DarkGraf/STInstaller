using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InstallerStudio.Models;
using InstallerStudio.WixElements;

namespace InstallerStudioTest.Models
{
  [TestClass]
  public class MspModelTest
  {
    /// <summary>
    /// Проверка корневого элемента.
    /// </summary>
    [TestMethod]
    public void MspModelRootElement()
    {
      BuilderModel model = new MspModel();
      Assert.IsInstanceOfType(model.RootItem, typeof(WixPatchFamilyElement));
    }
  }
}

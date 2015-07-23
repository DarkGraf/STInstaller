using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using WixSTActions.WixControl;

namespace WixSTActionsTest
{
  [TestClass]
  public class WixListViewTest
  {
    void Check(IList<Record> expected, IList<WixListItem> actual)
    {
      Assert.AreEqual(expected.Count, actual.Count);
      for (int i = 0; i < expected.Count; i++)
      {
        Assert.AreEqual(expected[i].GetString(1), "viewProperty");
        Assert.AreEqual(expected[i].GetString(4), actual[i].Text);
        Assert.AreEqual(expected[i].GetString(3), actual[i].Value);
        Assert.AreEqual(expected[i].GetString(5), actual[i].Icon);
      }
    }

    [TestMethod]
    [TestCategory("WixControls")]
    public void WixListViewTesting()
    {
      using (ShimsContext.Create())
      {
        List<Record> list = new List<Record>(); // Используется для хранения добавленных и удаленных объетков.        

        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.DatabaseGet = delegate 
        { 
          return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase().Instance; 
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.OpenViewStringObjectArray = (@this, sqlFormat, args) => 
        {
          // Логика удаления реализована здесь.
          if (sqlFormat.StartsWith("DELETE") && sqlFormat.Contains("Value"))
          {            
            Record r = list.FirstOrDefault(v => v.GetString(3) == args[1].ToString());
            list.Remove(r);
          }
          // Логика очистки.
          if (sqlFormat.StartsWith("DELETE") && !sqlFormat.Contains("Value"))
          {
            list.Clear();
          }

          return new Microsoft.Deployment.WindowsInstaller.Fakes.ShimView().Instance;
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimDatabase.AllInstances.CreateRecordInt32 = (@this, fieldCount) => { return new Record(fieldCount); };
        int index = -1; // Для View.Fetch().
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimView.AllInstances.Fetch = (@this) =>
        {
          // Сюда заходим при получении списка объектов (Items) и при получении максимального номера вставки.
          // Если в списке есть элементы, то передать их.
          if (++index < list.Count)
            return list[index];
          else
          {
            index = -1;
            return null;
          }
        };
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimView.AllInstances.ModifyViewModifyModeRecord = (@this, mode, record) => { list.Add(record); }; // Логика добавления реализована здесь.
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimInstallerHandle.AllInstances.Dispose = delegate { }; // Чтобы не освобождался Record.
        
        // Начало теста.
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        
        WixListView view = new WixListView(session, "viewProperty");
        Assert.AreEqual(0, list.Count); // Дополнительная проверка.
        Check(list, view.Items);
        
        view.AddItem(new WixListItem("Text1", "Value1", "Icon1"));
        Assert.AreEqual(1, list.Count); // Дополнительная проверка.
        Check(list, view.Items);

        view.AddItems(new WixListItem[] { new WixListItem("Text2", "Value2", "Icon2"), new WixListItem("Text3", "Value3", "Icon3") });
        Assert.AreEqual(3, list.Count); // Дополнительная проверка.
        Check(list, view.Items);

        // Удаление.
        view.RemoveItem("Value2");
        Assert.AreEqual(2, list.Count); // Дополнительная проверка.
        Check(list, view.Items);

        // Очистка.
        view.ClearItems();
        Assert.AreEqual(0, list.Count); // Дополнительная проверка.
        Check(list, view.Items);
      }
    }
  }
}

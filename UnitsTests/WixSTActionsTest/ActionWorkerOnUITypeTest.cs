using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions;
using WixSTActions.Utils;
using WixSTActions.ActionWorker;

namespace WixSTActionsTest
{
  /// <summary>
  /// Тесты всех ActionWorker на возможные режимы UIType (установка сервера и клиента).
  /// </summary>
  [TestClass]
  public class ActionWorkerOnUITypeTest
  {
    static Dictionary<object, bool> dependencyProperties = new Dictionary<object, bool>();

    static void SetValue(object obj, bool value)
    {
      dependencyProperties[obj] = value;
    }

    static bool GetValue(object obj)
    {
      return dependencyProperties.ContainsKey(obj) && dependencyProperties[obj];
    }

    #region Тестируемые классы.

    class StubActionCheckConnectionWorker : ActionCheckConnectionWorker
    {
      public StubActionCheckConnectionWorker(Session session) : base(session, CheckConnectionType.DatabaseMustExist, null, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionDatabaseUIControlWorker : ActionDatabaseUIControlWorker
    {
      public StubActionDatabaseUIControlWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionDefineSqlServerPathWorker : ActionDefineSqlServerPathWorker
    {
      public StubActionDefineSqlServerPathWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionInitializationFinishInfoWorker : ActionInitializationFinishInfoWorker
    {
      public StubActionInitializationFinishInfoWorker(Session session) : base(session, null, false) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionInstallingExtendedProceduresWorker : ActionInstallingExtendedProceduresWorker
    {
      public StubActionInstallingExtendedProceduresWorker(Session session) : base(session) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionWidgetCreaterWorker : ActionWidgetCreaterWorker
    {
      public StubActionWidgetCreaterWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionInstallReportWorker : ActionInstallReportWorker
    {
      public StubActionInstallReportWorker(Session session) : base(session, ActionInstallReportWorkerMode.Start) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionMefControlWorker : ActionMefControlWorker
    {
      public StubActionMefControlWorker(Session session) : base(session) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionRestoringDatabaseWorker : ActionRestoringDatabaseWorker
    {
      public StubActionRestoringDatabaseWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionRunSqlScriptNewDbWorker : ActionRunSqlScriptNewDbWorker
    {
      public StubActionRunSqlScriptNewDbWorker(Session session) : base(session, null, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionRunSqlScriptExistingDbWorker : ActionRunSqlScriptExistingDbWorker
    {
      public StubActionRunSqlScriptExistingDbWorker(Session session) : base(session, null, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionSelectDatabasesWorker : ActionSelectDatabasesWorker
    {
      public StubActionSelectDatabasesWorker(Session session) : base(session, ActionSelectDatabasesWorkerMode.Initialization, null, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionServerUIControlWorker : ActionServerUIControlWorker
    {
      public StubActionServerUIControlWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionTempDirectoryControlWorker : ActionTempDirectoryControlWorker
    {
      public StubActionTempDirectoryControlWorker(Session session) : base(session, ActionTempDirectoryControlWorkerMode.Create) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    class StubActionUITypeWorker : ActionUITypeWorker
    {
      public StubActionUITypeWorker(Session session) : base(session, null) { }
      protected override bool IsAllowed { get { SetValue(this, true); return false; } }
      protected override ActionResult DoWork() { throw new NotImplementedException(); }
    }

    #endregion

    void Check(IActionWorker worker, bool serverMode, bool clientMode)
    {
      CustomActionData customActionData = new CustomActionData();
      Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.CustomActionDataGet = delegate { return customActionData; };

      // Проверка режима сервера.
      SetValue(worker, false);
      customActionData.AddObject<UIType>(SessionUITypeGetterExtension.keyUIType, UIType.Server);
      worker.Execute();
      Assert.AreEqual(serverMode, GetValue(worker), worker.GetType().ToString() + " Server");

      // Проверка режима клиента.
      SetValue(worker, false);
      customActionData.Clear();
      customActionData.AddObject<UIType>(SessionUITypeGetterExtension.keyUIType, UIType.Client);
      worker.Execute();
      Assert.AreEqual(clientMode, GetValue(worker), worker.GetType().ToString() + " Client");
    }

    [TestMethod]
    [TestCategory("ActionWorker")]
    public void ActionWorkerAllOnUITypeTesting()
    {
      using (ShimsContext.Create())
      {
        Session session = new Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession().Instance;
        Microsoft.Deployment.WindowsInstaller.Fakes.ShimSession.AllInstances.GetModeInstallRunMode = 
          (@this, mode) => { return mode == InstallRunMode.Scheduled; };

        // Установим по умолчанию сессию, иначе будут вызываться с других тестом Stub-объекты.
        // Не понятно почему, разобрать позже...
        session.SetDefaultSessionService(null);
        
        var workers = new[]
        {
          new { Worker = (IActionWorker)new StubActionCheckConnectionWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionDatabaseUIControlWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionDefineSqlServerPathWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionInstallingExtendedProceduresWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionMefControlWorker(session), ServerMode = true, ClientMode = true },
          new { Worker = (IActionWorker)new StubActionRestoringDatabaseWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionRunSqlScriptNewDbWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionRunSqlScriptExistingDbWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionSelectDatabasesWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionServerUIControlWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionTempDirectoryControlWorker(session), ServerMode = true, ClientMode = true },
          new { Worker = (IActionWorker)new StubActionUITypeWorker(session), ServerMode = true, ClientMode = true },

          new { Worker = (IActionWorker)new StubActionInitializationFinishInfoWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionWidgetCreaterWorker(session), ServerMode = true, ClientMode = false },
          new { Worker = (IActionWorker)new StubActionInstallReportWorker(session), ServerMode = true, ClientMode = true }
        };

        // Берем все не абстрактные типы с реализацией IActionWorker и проверяем количество в тестируемом списке.
        var workerTypes = typeof(IActionWorker).Assembly.GetTypes().
          Where(v => !v.IsAbstract && v.GetInterface(typeof(IActionWorker).Name) != null).ToArray();

        Assert.AreEqual(workerTypes.Count(), workers.Length, string.Format("Не все тесты IActionWorker реализованы ({0} из {1}).", 
          workers.Length, workerTypes.Count()));

        foreach (var type in workerTypes)
        {
          Assert.IsNotNull(workers.FirstOrDefault(v => v.Worker.GetType().IsSubclassOf(type)), 
            string.Format("Нет реализации для {0}.", type.Name));
        }

        // Проверяем функциональность.
        foreach (var worker in workers)
          Check(worker.Worker, worker.ServerMode, worker.ClientMode);
      }
    }
  }
}

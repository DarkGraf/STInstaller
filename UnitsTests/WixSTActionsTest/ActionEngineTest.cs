using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Deployment.WindowsInstaller;

using WixSTActions;

namespace WixSTActionsTest
{
  #region Классы комманд для тестов

  class Counter
  {
    public Counter()
    {
      Count = 0;
    }

    public int Count { get; set; }
  }

  class SuccessCommand : IActionWorker
  {
    Counter counter;

    public SuccessCommand(Counter counter)
    {
      this.counter = counter;
    }

    public ActionResult Execute()
    {
      counter.Count++;
      return ActionResult.Success;
    }
  }

  class FailureCommand : IActionWorker
  {
    Counter counter;

    public FailureCommand(Counter counter)
    {
      this.counter = counter;
    }

    public ActionResult Execute()
    {
      counter.Count++;
      return ActionResult.Failure;
    }
  }

  #endregion

  [TestClass]
  public class ActionEngineTest
  {
    // Так как ActionEngine реализован как одиночка, 
    // у него могут остаться задания. Очистим их после теста.
    [TestCleanup]
    [TestCategory("Other")]
    public void TestCleanup()
    {
      ActionEngine.Instance.ResetWorkers();
    }

    /// <summary>
    /// Тест ActionEngine с одной успешной командой.
    /// </summary>
    [TestMethod]
    [TestCategory("Other")]
    public void ActionEngineOneCommandSuccess()
    {
      Counter c = new Counter();
      ActionEngine engine = ActionEngine.Instance;
      engine.AddWorker(new SuccessCommand(c));
      Assert.AreEqual(ActionResult.Success, engine.Run());
      Assert.AreEqual(1, c.Count);
    }

    /// <summary>
    /// Тест ActionEngine с двумя успешными командами.
    /// </summary>
    [TestMethod]
    [TestCategory("Other")]
    public void ActionEngineTwoCommandSuccess()
    {
      Counter c = new Counter();
      ActionEngine engine = ActionEngine.Instance;
      engine.AddWorker(new SuccessCommand(c));
      engine.AddWorker(new SuccessCommand(c));
      Assert.AreEqual(ActionResult.Success, engine.Run());
      Assert.AreEqual(2, c.Count);
    }

    /// <summary>
    /// Тест ActionEngine с одной неуспешной командой.
    /// </summary>
    [TestMethod]
    [TestCategory("Other")]
    public void ActionEngineOneCommandFailure()
    {
      Counter c = new Counter();
      ActionEngine engine = ActionEngine.Instance;
      engine.AddWorker(new FailureCommand(c));
      Assert.AreEqual(ActionResult.Failure, engine.Run());
      Assert.AreEqual(1, c.Count);
    }

    /// <summary>
    /// Тест ActionEngine с двумя неуспешными командами.
    /// </summary>
    [TestMethod]
    [TestCategory("Other")]
    public void ActionEngineTwoCommandFailure()
    {
      Counter c = new Counter();
      ActionEngine engine = ActionEngine.Instance;
      engine.AddWorker(new FailureCommand(c));
      engine.AddWorker(new FailureCommand(c));
      Assert.AreEqual(ActionResult.Failure, engine.Run());
      Assert.AreEqual(1, c.Count);
    }

    /// <summary>
    /// Тест ActionEngine с одной успешной и с одной неуспешной командами.
    /// </summary>
    [TestMethod]
    [TestCategory("Other")]
    public void ActionEngineTwoCommandSuccessAndFailure()
    {
      Counter c = new Counter();
      ActionEngine engine = ActionEngine.Instance;
      engine.AddWorker(new SuccessCommand(c));
      engine.AddWorker(new FailureCommand(c));
      Assert.AreEqual(ActionResult.Failure, engine.Run());
      Assert.AreEqual(2, c.Count);
    }
  }
}

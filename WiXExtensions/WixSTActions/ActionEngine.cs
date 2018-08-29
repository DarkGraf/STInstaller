using System;
using System.Collections.Generic;

using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions
{
    /// <summary>
    /// Паттрен "Команда".
    /// </summary>
    interface IActionWorker
    {
        ActionResult Execute();
    }

    /// <summary>
    /// Паттерн "Активный объект".
    /// Паттерн "Одиночка".
    /// Обработка команд типа IActionWorker.
    /// </summary>
    class ActionEngine
    {
        #region Паттерн "Активный объект".

        private Queue<IActionWorker> commands = new Queue<IActionWorker>();

        public void AddWorker(IActionWorker worker)
        {
            commands.Enqueue(worker);
        }

        public ActionResult Run()
        {
            System.Diagnostics.Debugger.Launch();
            ActionResult result = ActionResult.Success;

            while (commands.Count > 0 && result == ActionResult.Success)
            {
                IActionWorker worker = commands.Dequeue();
                result = worker.Execute();
            }

            return result;
        }

        #endregion

        #region Паттерн "Одиночка".

        private static ActionEngine instance = null;

        private ActionEngine() { }

        public static ActionEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new ActionEngine();
                return instance;
            }
        }

        #endregion

        internal void ResetWorkers()
        {
            commands.Clear();
        }
    }
}

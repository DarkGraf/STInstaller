using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSTActions.ActionWorker;

namespace WixSTActions
{
  public partial class CustomAction
  {
    private static void CommonInitialization(Session session, CustomActionProperties properties)
    {
      // Определения режима инсталлятора: клиент или сервер на основе элемента UIType.
      // При повторном вызове (т. е. если тип установки определен), действие не будет выполнятся.
      ActionEngine.Instance.AddWorker(new ActionUITypeWorker(session, properties.UIType));
    }
  }
}

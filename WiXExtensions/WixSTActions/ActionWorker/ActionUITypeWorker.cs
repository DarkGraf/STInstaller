using System;
using System.Data;

using Microsoft.Deployment.WindowsInstaller;

using WixSTActions.Utils;

namespace WixSTActions.ActionWorker
{
  interface IActionUITypeSessionProperties
  {
    string Mode { get; set; }
  }

  /// <summary>
  /// Определяет тип установки: клиент или сервер.
  /// Инициализирует переданное свойство сессии в "Client" в случае клиента, в "Server" в случае сервера.
  /// Сохраняет тип установки для всех действий.
  /// </summary>
  class ActionUITypeWorker : ActionWorkerBase
  {
    /// <summary>
    /// Исключение о неправильном использовании элемента "UIType".
    /// </summary>
    internal class MisuseOfUITypeException : Exception { }

    IActionUITypeSessionProperties properties;

    public ActionUITypeWorker(Session session, IActionUITypeSessionProperties properties) : base(session)
    {
      this.properties = properties;
    }

    #region ActionWorkerBase

    protected override UIType UITypeMode
    {
      get { return UIType.Unknow; }
    }

    protected override bool IsAllowed
    {
      // Если properties.Mode не пустое, то значит уже определили тип установки
      // и дальнейшая работа не требуется.
      get { return string.IsNullOrEmpty(properties.Mode); }
    }

    protected override ActionResult DoWork()
    {
      DataTable tbl = Session.GetService<ISessionDataTableExtension>().CopyTableInfoToDataTable("UIType");
      if (tbl != null)
      {
        // Если строка не одна, выбрасываем исключение.
        if (tbl.Rows.Count != 1)
          throw new MisuseOfUITypeException();

        UIType type;
        // Если не правильное значение, выбрасываем исключение.
        if (!Enum.TryParse<UIType>(tbl.Rows[0]["Type"].ToString(), out type))
          throw new MisuseOfUITypeException();

        // Запоминаем для использования в Wix.
        properties.Mode = type.ToString();
        // Запоминаем для использования в CustomAction.
        Session.GetService<ISessionUITypeGetterExtension>().SetUIType(type);
      }
      return ActionResult.Success;
    }

    #endregion    
  }
}

using System;
using System.Collections.Generic;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSTActions.Utils
{
  /// <summary>
  /// Типы установочных состояний.
  /// </summary>
  [Flags]
  public enum CurrentInstallStatus
  {
    /// <summary>
    /// Не определен. Должен быть строго нуль, т. к. используется для проверки флагов.
    /// </summary>
    Unknow = 0,
    /// <summary>
    /// Первичная установка.
    /// </summary>
    Install = 1,
    /// <summary>
    /// Изменение в режиме обслуживания (переустановка этого же продукта).
    /// </summary>
    Change = 2,
    /// <summary>
    /// Восстановление в режиме обслуживания.
    /// </summary>
    Repair = 4,
    /// <summary>
    /// Полное удаление.
    /// </summary>
    Uninstall = 8,
    /// <summary>
    /// Обновление.
    /// </summary>
    Update = 16,
    /// <summary>
    /// Патч.
    /// </summary>
    Patch = 32
  }

  // Флаги Install, Reinstall, Uninstall должны совпадать с внутренними константами
  // описанными в WixSTSqlExtension.STSqlCompiler, тем самым обеспечивается
  // единое кодирование установочных состояний.
  /// <summary>
  /// Типы установочных состояний.
  /// </summary>
  [Flags]
  public enum ComponentInstallStatus
  {
    /// <summary>
    /// Не определен. Должен быть строго нуль, т. к. используется для проверки флагов.
    /// </summary>
    Unknow = 0,
    /// <summary>
    /// Компонент отсутствует на диске и будет установлен на диск.
    /// </summary>
    Install = 1,
    /// <summary>
    /// Компонент установлен на диске и будет установлен на диск.
    /// </summary>
    Reinstall = 2,
    /// <summary>
    /// Удаление.
    /// </summary>
    Uninstall = 4,
    /// <summary>
    /// Компонент уже установлен и действия нет.
    /// </summary>
    AlreadyInstalled = 8
  }
  
  abstract class InstallStatusDefinerBase<T>
    where T : struct
  {
    protected Session Session { get; private set; }
    protected readonly IDictionary<string, T> conditions;

    public InstallStatusDefinerBase(Session session)
    {
      Session = session;
      conditions = new Dictionary<string, T>();
    }

    protected void AddCondition(string condition, T status)
    {
      conditions.Add(condition, status);
    }

    public T Status
    {
      get
      {
        // Должно быть только одно уникальное значение состояния, если их несколько, то генерируем исключение.
        T status = default(T);
        // Вычисляем все состояние.
        foreach (string key in conditions.Keys)
        {
          if (Session.EvaluateCondition(key))
          {
            if (status.Equals(default(T)))
              status = conditions[key];
            else
              throw new InvalidOperationException(string.Format("Обнаружено несколько истинных статусов в {0}.", GetType().Name));
          }
        }
        return status;
      }
    }
  }

  /// <summary>
  /// Определение текущего статуса установки.
  /// </summary>
  class CurrentInstallStatusDefiner : InstallStatusDefinerBase<CurrentInstallStatus>
  {
    #region Строковые константы описания состояний.

    /*-------------------------------------------------------------------------------------------------------------------------------------------------------
    |                         |Installed|PATCH    |REMOVE|REINSTALL           |UPGRADINGPRODUCTCODE|ACTION |ADDLOCAL            |REINSTALLMODE|Preselected|RESUME|
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Новая установка UI       |         |         |      |                    |                    |INSTALL|                    |             |           |      |
    |Новая установка Immediate|         |         |      |                    |                    |INSTALL|CommonFeature,      |             |1          |      |
    |                         |         |         |      |                    |                    |       |RootFeature,        |             |           |      |
    |                         |         |         |      |                    |                    |       |DataBaseFilesFeature|             |           |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Изменение UI             |00:00:00 |         |      |                    |                    |INSTALL|                    |             |           |      |
    |Изменение Immediate      |00:00:00 |         |      |                    |                    |INSTALL|	                  |             |           |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Восстановление UI*       |00:00:00 |         |      |                    |                    |INSTALL|                    |             |           |      |
    |Восстановление Immediate |00:00:00 |         |      |CommonFeature,      |                    |INSTALL|                    |ecmus        |1          |      |
    |                         |         |         |      |RootFeature,        |                    |       |                    |             |           |      |
    |                         |         |         |      |DataBaseFilesFeature|                    |       |                    |             |           |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Удаление UI*             |00:00:00 |         |      |                    |                    |INSTALL|                    |             |           |      |
    |Удаление Immediate       |00:00:00 |         |ALL   |                    |                    |INSTALL|                    |             |1          |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Обновление UI            |00:00:00 |         |      |ALL                 |                    |INSTALL|                    |vomus        |1          |      |
    |Обновление Immediate     |00:00:00 |         |      |CommonFeature,      |                    |INSTALL|                    |vomus        |1          |      |
    |                         |         |         |      |RootFeature,        |                    |       |                    |             |           |      |
    |                         |         |         |      |DataBaseFilesFeature|                    |       |                    |             |           |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
    |Патч UI                  |00:00:00 |Путь к   |      |                    |                    |INSTALL|                    |             |           |      |
    |                         |         |файлу msp|      |                    |                    |       |                    |             |           |      |
    |Патч Immediate           |00:00:00 |Путь к   |      |                    |                    |INSTALL|                    |             |           |      |
    |                         |         |файлу msp|      |                    |                    |       |                    |             |           |      |
    |--------------------------------------------------------------------------------------------------------------------------------------------------------|
     
     *Состояние UI изменяется после подтверждения установки компонент и они становятся равными Immediate.*/


    // Первичная установка. Взято из условий MSI-установщика.
    internal const string InstallCondition = "NOT Installed AND NOT PATCH";
    // Изменения этого же продукта.
    internal const string ChangeCondition = "Installed AND NOT REMOVE AND NOT REINSTALL AND NOT PATCH";
    // Восстановление.
    internal const string RepairCondition = @"Installed AND NOT REMOVE AND REINSTALL AND REINSTALLMODE=""ecmus""";
    // Полное удаление продукта.
    internal const string UninstallCondition = @"Installed AND REMOVE=""ALL""";
    // Обновление.
    internal const string UpdateCondition = @"Installed AND NOT REMOVE AND REINSTALL AND REINSTALLMODE=""vomus""";
    // Патч.
    internal const string PatchCondition = "Installed AND PATCH";

    #endregion

    public CurrentInstallStatusDefiner(Session session) : base(session)
    {
      AddCondition(InstallCondition, CurrentInstallStatus.Install);
      AddCondition(ChangeCondition, CurrentInstallStatus.Change);
      AddCondition(RepairCondition, CurrentInstallStatus.Repair);
      AddCondition(UninstallCondition, CurrentInstallStatus.Uninstall);
      AddCondition(UpdateCondition, CurrentInstallStatus.Update);
      AddCondition(PatchCondition, CurrentInstallStatus.Patch);
    }
  }

  class ComponentInstallStatusDefiner : InstallStatusDefinerBase<ComponentInstallStatus>
  {
    public ComponentInstallStatusDefiner(Session session, string componentName) : base(session)
    {
      /* %  environment variable (name is case insensitive)
       * $  action state of component
       * ?  installed state of component
       * &  action state of feature
       * !  installed state of feature
       * 
       *-1  no action to be taken
       * 1  advertised (only for features)
       * 2  not present
       * 3  on the local computer
       * 4  run from the source */

      AddCondition(string.Format("${0}=3 AND ?{0}=2", componentName), ComponentInstallStatus.Install);
      AddCondition(string.Format("${0}=3 AND ?{0}=3", componentName), ComponentInstallStatus.Reinstall);
      AddCondition(string.Format("${0}=2 AND ?{0}=3", componentName), ComponentInstallStatus.Uninstall);
      AddCondition(string.Format("${0}=-1 AND ?{0}=3", componentName), ComponentInstallStatus.AlreadyInstalled);
    }
  }
}
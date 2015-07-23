using System;
using System.Runtime.CompilerServices;

namespace InstallerStudio.Common
{
  public enum PropertyState { Changed, Unchanged }

  /// <summary>
  /// Объект с возможностью изменения свойств.
  /// </summary>
  [Serializable]
  public class ChangeableObject : NotifyObject
  {
    protected virtual PropertyState SetValue<T>(ref T actualValue, T newValue, [CallerMemberName]string propertyName = "")
    {
      PropertyState result = PropertyState.Unchanged;

      // actualValue = null и newValue = null - не выполняем.
      // actualValue = null и newValue != null - выполняем.
      // actualValue != null и newValue = null - проверяем дальше.
      // actualValue != null и newValue != null - проверяем дальше.
      if (actualValue == null && newValue != null || actualValue != null && !actualValue.Equals(newValue))
      {
        actualValue = newValue;
        NotifyPropertyChanged(propertyName);
        result = PropertyState.Changed;
      }

      return result;
    }
  }
}

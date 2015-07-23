using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InstallerStudio.Common
{
  /// <summary>
  /// Базовый объект с уведомлением клиентов об изменении значения свойств.
  /// </summary>
  // Помечаем атрибутом сериализации, чтобы наследники могли сериализоваться.
  [Serializable]
  public class NotifyObject : INotifyPropertyChanged
  {
    #region INotifyPropertyChanged

    // Для исключение сериализации события.
    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    protected virtual void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
#if DEBUG
      System.Diagnostics.Trace.WriteLine(string.Format("Property changed: {1} ({0})", GetType(), propertyName));
#endif
    }
  }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using InstallerStudio.Common;

namespace InstallerStudio.ViewModels
{
  class BaseViewModel : ChangeableObject, IDisposable
  {
    #region Для отладки

    public bool ThrowOnInvalidPropertyName { get; set; }

    /// <summary>
    /// Проверяет, что свойство с данным именем действительно существует в объекте модели представления.
    /// </summary>
    /// <param name="propertyName"></param>
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public void VerifyPropertyName(string propertyName)
    {
      // Если равно null - уведомление всех свойств.
      if (propertyName != null && TypeDescriptor.GetProperties(this)[propertyName] == null)
      {
        string msg = "Недействительное свойство: " + propertyName;
        if (ThrowOnInvalidPropertyName)
          throw new Exception(msg);
        else
          Debug.Fail(msg);
      }
    }

    #endregion

    #region NotifyObject

    protected override void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
    {
      VerifyPropertyName(propertyName);
      base.NotifyPropertyChanged(propertyName);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion

    #region Очистка объекта

    protected virtual void DisposeManagedResources() { }

    protected virtual void DisposeUnmanagedResources() { }

    bool disposed = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing">Если true, то освободить управляемые ресурсы.</param>
    private void Dispose(bool disposing)
    {
      if (!disposed)
      {
        if (disposing)
        {
          // Освободить управляемые ресурсы.
          DisposeManagedResources();
        }

        // Освободить неуправляемые ресурсы.
        DisposeUnmanagedResources();
      }
      disposed = true;
    }

    ~BaseViewModel()
    {
      Dispose(false);
    }

    #endregion
  }
}

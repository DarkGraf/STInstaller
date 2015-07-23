﻿using System;
using System.Diagnostics;
using System.Windows.Input;

namespace InstallerStudio.ViewModels.Utils
{
  class RelayCommand : ICommand
  {
    private readonly Action<object> execute;
    private readonly Predicate<object> canExecute;

    public RelayCommand(Action<object> execute) : this(execute, null) { }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
      if (execute == null)
        throw new ArgumentNullException("execute");

      this.execute = execute;
      this.canExecute = canExecute;
    }

    #region ICommand

    [DebuggerStepThrough]
    public bool CanExecute(object parameter)
    {
      return canExecute == null ? true : canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
      execute(parameter);
    }

    #endregion
  }
}

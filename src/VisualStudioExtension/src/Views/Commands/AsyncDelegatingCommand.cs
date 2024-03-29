﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Tanzu.Toolkit.VisualStudio.Views.Commands
{
    public class AsyncDelegatingCommand : ICommand
    {
        public Func<object, Task> Action { get; private set; }

        private readonly Predicate<object> _canExecute;
        private EventHandler _eventHandler;

        public AsyncDelegatingCommand(Func<object, Task> action)
            : this(action, null)
        {
        }

        public AsyncDelegatingCommand(Func<object, Task> action, Predicate<object> canExecute)
        {
            Action = action;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _eventHandler += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _eventHandler -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _eventHandler?.Invoke(this, new EventArgs());
        }

        internal bool IsExecuting { get; set; }

        public bool CanExecute(object parameter)
        {
            if (IsExecuting)
            {
                return false;
            }

            return _canExecute == null || _canExecute(parameter);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "All exceptions handled (ignored); no risk of process crash")]
        public async void Execute(object parameter)
        {
            IsExecuting = true;

            try
            {
                await Action(parameter);
            }
            catch
            {
            }

            IsExecuting = false;
        }
    }
}

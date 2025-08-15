using System;
using System.Windows.Input;

namespace Tanzu.Toolkit.VisualStudio.Views.Commands
{
    public class DelegatingCommand : ICommand
    {
        public Action<object> Action { get; private set; }

        private readonly Predicate<object> _canExecute;
        private EventHandler _eventHandler;

        public DelegatingCommand(Action<object> action) : this(action, null)
        {
        }

        public DelegatingCommand(Action<object> action, Predicate<object> canExecute)
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
            _eventHandler?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            Action(parameter);
        }
    }
}
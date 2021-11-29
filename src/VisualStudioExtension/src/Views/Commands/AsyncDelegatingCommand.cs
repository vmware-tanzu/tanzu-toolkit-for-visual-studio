using System;
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

            return _canExecute == null ? true : _canExecute(parameter);
        }

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

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TanzuForVS.WpfViews.Commands
{
    public class AsyncDelegatingCommand : ICommand
    {
        internal readonly Predicate<object> canExecute;

        internal readonly Func<object, Task> action;

        private EventHandler eventHandler;

        public AsyncDelegatingCommand(Func<object, Task> action) 
            : this(action, null)
        {
        }

        public AsyncDelegatingCommand(Func<object, Task> action, Predicate<object> canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                eventHandler += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                eventHandler -= value;
                CommandManager.RequerySuggested -= value;
            }
        }
        public void RaiseCanExecuteChanged()
        {
            eventHandler?.Invoke(this, new EventArgs());
        }

        internal bool IsExecuting { get; set; }

        public bool CanExecute(object parameter)
        {
            if (IsExecuting)
            {
                return false;
            }

            return canExecute == null ? true : canExecute(parameter);
        }

        public async void Execute(object parameter)
        {
            IsExecuting = true;

            try
            {
                await action(parameter);
            }
            catch (Exception e)
            {
                // Assume exceptions caught in view model
                // TODO: Log when not
            }

            IsExecuting = false;
        }
    }
}

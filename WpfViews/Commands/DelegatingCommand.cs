using System;
using System.Windows.Input;

namespace TanzuForVS.WpfViews.Commands
{
    public class DelegatingCommand : ICommand
    {
        internal readonly Action<object> action;
        internal readonly Predicate<object> canExecute;
        private EventHandler eventHandler;

        public DelegatingCommand(Action<object> action) : this(action, null) { }

    
        public DelegatingCommand(Action<object> action, Predicate<object> canExecute)
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

        public bool CanExecute(object parameter)
        {
            return canExecute == null ? true : canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            action(parameter);
        }
    }
}

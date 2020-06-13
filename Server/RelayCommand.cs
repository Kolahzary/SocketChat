using System;
using System.Windows.Input;

namespace SocketChat
{
    public class RelayCommand : ICommand
    {
        protected readonly Func<Boolean> canExecute;

        protected readonly Action execute;

        public RelayCommand(Action execute, Func<Boolean> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        public virtual Boolean CanExecute(Object parameter)
        {
            return this.canExecute == null ? true : this.canExecute();
        }

        public virtual void Execute(Object parameter)
        {
            this.execute();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        protected readonly Func<T, Boolean> canExecute;

        protected readonly Action<T> execute;

        public RelayCommand(Action<T> execute, Func<T, Boolean> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        public virtual Boolean CanExecute(Object parameter)
        {
            return this.canExecute == null ? true : this.canExecute((T)parameter);
        }

        public virtual void Execute(Object parameter)
        {
            this.execute((T)parameter);
        }
    }
}
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mechanical_Keyboard.Helpers
{
    public partial class AsyncRelayCommand<T>(Func<T?, Task> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        private readonly Func<T?, Task> _execute = execute;
        private readonly Func<T?, bool>? _canExecute = canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);
        }

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            OnCanExecuteChanged();

            try
            {
                await _execute((T?)parameter);
            }
            finally
            {
                _isExecuting = false;
                OnCanExecuteChanged();
            }
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

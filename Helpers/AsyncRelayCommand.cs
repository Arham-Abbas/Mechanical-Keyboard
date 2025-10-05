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
            // The entire operation must be wrapped in a try/catch block
            // to prevent any exception from escaping the 'async void' method.
            try
            {
                if (CanExecute(parameter))
                {
                    _isExecuting = true;
                    OnCanExecuteChanged();

                    await _execute((T?)parameter);
                }
            }
            catch (Exception ex)
            {
                // Log the exception instead of crashing the application.
                System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled exception in AsyncRelayCommand: {ex}");
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
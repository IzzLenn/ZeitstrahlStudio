using System.Windows.Input;

namespace ZeitstrahlStudio.App;

/// <summary>Ein asynchroner MVVM-Befehl mit Wiederaufrufsperre.</summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> execute;
    private readonly Func<bool>? canExecute;
    private bool isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !isExecuting && (canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute().ConfigureAwait(true);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Ein asynchroner parametrisierter MVVM-Befehl.</summary>
public sealed class AsyncRelayCommand<T> : ICommand
    where T : class
{
    private readonly Func<T, Task> execute;
    private readonly Func<T, bool>? canExecute;
    private bool isExecuting;

    public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        !isExecuting && parameter is T typedParameter && (canExecute?.Invoke(typedParameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (parameter is not T typedParameter || !CanExecute(typedParameter))
        {
            return;
        }

        isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute(typedParameter).ConfigureAwait(true);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

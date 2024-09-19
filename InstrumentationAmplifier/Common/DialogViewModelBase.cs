using System;
using CommunityToolkit.Mvvm.Input;

namespace InstrumentationAmplifier.Common;

public abstract partial class DialogViewModelBase : ViewModelBase
{
    public event Action<bool>? Closed;
    
    public Exception? Exception { get; set; }
    
    protected void Close(bool success) => Closed?.Invoke(success);
    
    [RelayCommand] protected void CloseWithSuccess() => Close(true);
    [RelayCommand] protected void CloseWithFailure() => Close(false);
    
    [RelayCommand] protected void CloseWithException(Exception exception)
    {
        Exception = exception;
        Close(false);
    }
}
using System;
using CommunityToolkit.Mvvm.Input;

namespace InstrumentationAmplifier.Common;

public abstract partial class ToastViewModelBase : ViewModelBase
{
    public event Action? Closed;
    
    [RelayCommand] protected void Close() => Closed?.Invoke();
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.ViewModels;

// TODO: Refactor this

public partial class MainWindowViewModel
{
    [ObservableProperty] private Control? _toastContent;
    [ObservableProperty] private ToastViewModelBase? _toastViewModel;

    private readonly Queue<ToastElement> _toasts = new();

    // !Potential problems with concurrency.
    private void NextToast()
    {
        while (_toasts.TryPeek(out var message))
        {
            if (message.TaskCompletionSource.Task.IsCompleted)
            {
                _toasts.Dequeue();
                continue;
            }

            // Otherwise there will be an exception, because the ViewModel type will not be the same as x:DataType.
            ToastViewModel = null;
            
            ToastContent = message.Message.Control;
            ToastViewModel = message.Message.ToastViewModel;

            Task.Delay(message.Message.Duration).ContinueWith(_ => message.Message.ToastViewModel.CloseCommand.Execute(null));

            return;
        }

        ToastContent = null;
        ToastViewModel = null;
    }
    
    private Task ShowToast(ShowToastMessage message)
    {
        lock (this)
        {
            CancellationToken ct = message.CancellationToken;
            TaskCompletionSource newToastTaskCompletionSource = new();

            if (ct.IsCancellationRequested)
            {
                newToastTaskCompletionSource.SetCanceled(ct);
                return newToastTaskCompletionSource.Task;
            }

            Action onClosed = null!;
            CancellationTokenRegistration? ctr = null;
            
            onClosed = () =>
            {
                message.ToastViewModel.Closed -= onClosed;
                ctr!.Value.Dispose();
                
                newToastTaskCompletionSource.SetResult();

                NextToast();
            };

            ctr = ct.Register(() =>
            {
                bool isActive = message == _toasts.Peek().Message;
                if (isActive)
                {
                    message.ToastViewModel.Closed -= onClosed;
                }
                newToastTaskCompletionSource.SetCanceled(ct); 
                if (isActive) NextToast();
            });
            
            message.ToastViewModel.Closed += onClosed;

            bool firstMessage = _toasts.Count == 0;

            ToastElement element = new(message, newToastTaskCompletionSource);
            _toasts.Enqueue(element);
            
            if (firstMessage) NextToast();

            return newToastTaskCompletionSource.Task;
        }
    }
    
    private record ToastElement(ShowToastMessage Message, TaskCompletionSource TaskCompletionSource);
}
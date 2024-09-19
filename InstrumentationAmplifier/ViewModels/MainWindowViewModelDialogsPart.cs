using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private Control? _dialogContent;
    [ObservableProperty] private DialogViewModelBase? _dialogViewModel;

    private readonly Queue<DialogElement> _dialogs = new();

    // !Potential problems with concurrency.
    private void NextDialog()
    {
        while (_dialogs.TryPeek(out var message))
        {
            if (message.TaskCompletionSource.Task.IsCompleted)
            {
                _dialogs.Dequeue();
                continue;
            }

            // Otherwise there will be an exception, because the ViewModel type will not be the same as x:DataType.
            DialogViewModel = null;
            
            DialogContent = message.Message.Control;
            DialogViewModel = message.Message.DialogViewModel;

            return;
        }

        DialogContent = null;
        DialogViewModel = null;
    }
    
    private Task<bool> ShowDialog(ShowDialogMessage message)
    {
        lock (this)
        {
            message.DialogViewModel.Exception = null;
            
            CancellationToken ct = message.CancellationToken;
            TaskCompletionSource<bool> newDialogTaskCompletionSource = new();

            if (ct.IsCancellationRequested)
            {
                newDialogTaskCompletionSource.SetCanceled(ct);
                return newDialogTaskCompletionSource.Task;
            }

            Action<bool> onClosed = null!;
            CancellationTokenRegistration? ctr = null;
            
            onClosed = success =>
            {
                message.DialogViewModel.Closed -= onClosed;
                ctr!.Value.Dispose();
                
                if (success == false && message.DialogViewModel.Exception != null)
                {
                    newDialogTaskCompletionSource.SetException(message.DialogViewModel.Exception);
                }
                else
                {
                    newDialogTaskCompletionSource.SetResult(success);
                }

                NextDialog();
            };

            ctr = ct.Register(() =>
            {
                bool isActive = message == _dialogs.Peek().Message;
                if (isActive)
                {
                    message.DialogViewModel.Closed -= onClosed;
                }
                newDialogTaskCompletionSource.SetCanceled(ct); 
                if (isActive) NextDialog();
            });
            
            message.DialogViewModel.Closed += onClosed;

            bool firstMessage = _dialogs.Count == 0;

            DialogElement element = new(message, newDialogTaskCompletionSource);
            _dialogs.Enqueue(element);
            
            if (firstMessage) NextDialog();

            return newDialogTaskCompletionSource.Task;
        }
    }
    
    private record DialogElement(ShowDialogMessage Message, TaskCompletionSource<bool> TaskCompletionSource);
}
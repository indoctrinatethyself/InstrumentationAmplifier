using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Toasts;

namespace InstrumentationAmplifier.Services;

public class ToastService : IToastService
{
    private readonly IDataTemplate _viewLocator;

    public ToastService(IDataTemplate viewLocator)
    {
        _viewLocator = viewLocator;
    }

    public Task ShowToastAsync(ToastViewModelBase toastViewModel, TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            Control control = _viewLocator.Build(toastViewModel)!;
            ShowToastMessage message = new()
            {
                ToastViewModel = toastViewModel,
                Control = control,
                Duration = duration,
                CancellationToken = cancellationToken
            };

            WeakReferenceMessenger.Default.Send(message);

            return message.Response;
        });
    }
}

public sealed class ShowToastMessage : RequestMessage<Task>
{
    public required ToastViewModelBase ToastViewModel { get; set; }
    public required Control Control { get; set; }
    public required TimeSpan Duration { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public static class ToastServiceExtensions
{
    public static async Task ShowMessage(this IToastService toastService,
        string message, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        SimpleToastViewModel toast = new(message);
        await toastService.ShowToastAsync(toast, duration, cancellationToken);
    }
}
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Dialogs;

namespace InstrumentationAmplifier.Services;

public class DialogService : IDialogService
{
    private readonly IDataTemplate _viewLocator;

    public DialogService(IDataTemplate viewLocator)
    {
        _viewLocator = viewLocator;
    }

    public Task<bool> ShowDialogAsync(DialogViewModelBase dialogViewModel, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            Control control = _viewLocator.Build(dialogViewModel)!;
            ShowDialogMessage message = new()
            {
                DialogViewModel = dialogViewModel,
                Control = control,
                CancellationToken = cancellationToken
            };
            
            WeakReferenceMessenger.Default.Send(message);
            
            return message.Response;
        });
    }
}

public sealed class ShowDialogMessage : RequestMessage<Task<bool>>
{
    public required DialogViewModelBase DialogViewModel { get; set; }
    public required Control Control { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public static class DialogServiceExtensions
{
    public static async Task ShowMessage(this IDialogService dialogService,
        string message, string buttonText = "Закрыть", CancellationToken cancellationToken = default)
    {
        MessageDialogViewModel dialog = new(message, buttonText);
        await dialogService.ShowDialogAsync(dialog, cancellationToken);
    }
}
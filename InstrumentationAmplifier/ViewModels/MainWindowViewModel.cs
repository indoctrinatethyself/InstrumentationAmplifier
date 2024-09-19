using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.ViewModels;

public partial class MainWindowViewModel : DisposableViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly IExceptionsLogger _exceptionsLogger;
    private readonly MainViewModel _mainViewModel;
    private readonly ConfigurationViewModel _configurationViewModel;

    [ObservableProperty] private ViewModelBase? _content;
    
    public MainWindowViewModel(
        IDialogService dialogService, IToastService toastService,
        IExceptionsLogger exceptionsLogger,
        MainViewModel mainViewModel,
        ConfigurationViewModel configurationViewModel)
    {
        _dialogService = dialogService;
        _toastService = toastService;
        _exceptionsLogger = exceptionsLogger;
        _mainViewModel = mainViewModel;
        _configurationViewModel = configurationViewModel;

        _content = mainViewModel;

        void MessengerRegister<TMessage>(MessageHandler<MainWindowViewModel, TMessage> handler) where TMessage : class =>
            WeakReferenceMessenger.Default.Register<MainWindowViewModel, TMessage>(this, handler);
        
        MessengerRegister<ShowDialogMessage>((recipient, message) => message.Reply(recipient.ShowDialog(message)));
        MessengerRegister<ShowToastMessage>((recipient, message) => message.Reply(recipient.ShowToast(message)));

        MessengerRegister<MainViewChangeMessage>((vm, m) => vm.ChangeView(m));
    }


    private void ChangeView(MainViewChangeMessage message) =>
        Content = message.Value switch
        {
            MainViewChangeMessage.Views.Main => _mainViewModel,
            MainViewChangeMessage.Views.Configuration => _configurationViewModel,
            _ => Content
        };
}

public sealed class MainViewChangeMessage : ValueChangedMessage<MainViewChangeMessage.Views>
{
    public static readonly MainViewChangeMessage Main = new(Views.Main);
    public static readonly MainViewChangeMessage Configuration = new(Views.Configuration);

    public MainViewChangeMessage(Views value, object? argument = null) : base(value) => Argument = argument;

    public object? Argument { get; init; }

    public enum Views { Main, Configuration }
}

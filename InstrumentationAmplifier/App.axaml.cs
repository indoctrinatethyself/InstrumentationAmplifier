using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using InstrumentationAmplifier.Dialogs;
using InstrumentationAmplifier.Services;
using InstrumentationAmplifier.Services.CommandHandler;
using InstrumentationAmplifier.Toasts;
using InstrumentationAmplifier.Utils;
using InstrumentationAmplifier.ViewModels;
using InstrumentationAmplifier.Views;

namespace InstrumentationAmplifier;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private IExceptionsLogger? _exceptionsLogger;

    private readonly bool RunByAvaloniaRiderPlugin;
    
    public App()
    {
        var args = Environment.GetCommandLineArgs();
        RunByAvaloniaRiderPlugin = args[0].Contains("Avalonia.Designer.HostApp.dll");
        
        ServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupExceptionHandling();

        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownRequested += (sender, args) =>
            {
                Console.WriteLine("ShutdownRequested");
                _serviceProvider.Dispose();
            };
            desktop.Exit += (sender, args) => Console.WriteLine("Exit");
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        services.AddSingleton<ApplicationConfigurationService>();
        services.AddSingleton<IStorageProvider>(_ =>
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow!.StorageProvider);
        services.AddSingleton<IDialogService, DialogService>(s => new DialogService(new DialogsViewLocator()));
        services.AddSingleton<IToastService, ToastService>(s => new ToastService(new ToastsViewLocator()));
        
        services.AddSingleton<IExceptionsLogger, ExceptionsLogger>();
        //services.AddSingleton<IModuleTools, ModuleTools>();

        services.AddSingleton<SpiDeviceFactory>();
        services.AddSingleton<IParallelDeviceFactory, ParallelDeviceFactory>();
        
        services.AddSingleton<AdcToPowerConverter>();
        services.AddSingleton<AttenuatorGainPerFrequency>();
        
        services.AddSingleton<MqttCommandListener>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ICommandHandler, MainViewModel>(p => p.GetRequiredService<MainViewModel>());
        services.AddSingleton<ConfigurationViewModel>();
    }
    
    private void SetupExceptionHandling()
    {
        _exceptionsLogger ??= _serviceProvider.GetRequiredService<IExceptionsLogger>();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            _exceptionsLogger.Log((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _exceptionsLogger.Log(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }
}
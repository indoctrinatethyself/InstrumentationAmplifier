using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Services;
using InstrumentationAmplifier.Utils;
using MathNet.Numerics;
using static InstrumentationAmplifier.Utils.CommonUtils;

namespace InstrumentationAmplifier.ViewModels;

public partial class ConfigurationViewModel : DisposableViewModelBase
{
    private readonly TimeSpan _toastDuration = TimeSpan.FromSeconds(2);

    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly IExceptionsLogger _exceptionsLogger;
    private readonly ApplicationConfigurationService _configurationService;

    private decimal _port = 1883; // UInt32
    private decimal _ip1 = 127, _ip2 = 0, _ip3 = 0, _ip4 = 1; // byte

    public ConfigurationViewModel(
        IDialogService dialogService, IToastService toastService,
        IExceptionsLogger exceptionsLogger,
        ApplicationConfigurationService configurationService,
        MqttCommandListener mqttCommandListener)
    {
        MqttCommandListener = mqttCommandListener;
        _dialogService = dialogService;
        _toastService = toastService;
        _exceptionsLogger = exceptionsLogger;
        _configurationService = configurationService;

        var configuration = _configurationService.Configuration;
        if (configuration.MqttAddress.Split([ '.', ':' ]) is [ var ip1, var ip2, var ip3, var ip4, var port ])
        {
            if (byte.TryParse(ip1, out byte ip1Value)) _ip1 = ip1Value;
            if (byte.TryParse(ip2, out byte ip2Value)) _ip2 = ip2Value;
            if (byte.TryParse(ip3, out byte ip3Value)) _ip3 = ip3Value;
            if (byte.TryParse(ip4, out byte ip4Value)) _ip4 = ip4Value;
            if (UInt16.TryParse(port, out UInt16 portValue)) _port = portValue;
        }
    }

    public MqttCommandListener MqttCommandListener { get; }

    public decimal Ip1
    {
        get => _ip1;
        set
        {
            if (_ip1 == value) return;
            var ip1 = Range(value.Round(0), 0, byte.MaxValue);
            _ip1 = ip1;
            OnPropertyChanged();

            if (value != _ip1) _ = _toastService.ShowMessage($"0..{byte.MaxValue}", _toastDuration);
        }
    }

    public decimal Ip2
    {
        get => _ip2;
        set
        {
            if (_ip2 == value) return;
            var ip2 = Range(value.Round(0), 0, byte.MaxValue);
            _ip2 = ip2;
            OnPropertyChanged();

            if (value != _ip2) _ = _toastService.ShowMessage($"0..{byte.MaxValue}", _toastDuration);
        }
    }

    public decimal Ip3
    {
        get => _ip3;
        set
        {
            if (_ip3 == value) return;
            var ip3 = Range(value.Round(0), 0, byte.MaxValue);
            _ip3 = ip3;
            OnPropertyChanged();

            if (value != _ip3) _ = _toastService.ShowMessage($"0..{byte.MaxValue}", _toastDuration);
        }
    }

    public decimal Ip4
    {
        get => _ip4;
        set
        {
            if (_ip4 == value) return;
            var ip4 = Range(value.Round(0), 0, byte.MaxValue);
            _ip4 = ip4;
            OnPropertyChanged();

            if (value != _ip4) _ = _toastService.ShowMessage($"0..{byte.MaxValue}", _toastDuration);
        }
    }

    public decimal Port
    {
        get => _port;
        set
        {
            if (_port == value) return;
            var port = Range(value.Round(0), 0, UInt16.MaxValue);
            _port = port;
            OnPropertyChanged();

            if (value != _port)
                _ = _toastService.ShowMessage($"Диапазон портов - 0..{UInt16.MaxValue}", _toastDuration);
        }
    }

    public string Ip => $"{Ip1:##0}.{Ip2:##0}.{Ip3:##0}.{Ip4:##0}";
    public string IpWithPort => $"{Ip}:{Port}";

    [RelayCommand]
    private async Task Toggle()
    {
        var options = MqttCommandListener.GetOptionBuilderWithDefaults()
            .WithTcpServer(Ip, (int)Port)
            .Build();
        try
        {
            if (MqttCommandListener.Enabled)
                await MqttCommandListener.Stop();
            //await MqttCommandListener.ChangeOptions(options);
            else
                await MqttCommandListener.Start(options);
        }
        catch (Exception e)
        {
            _ = _dialogService.ShowMessage("Ошибка подключения к MQTT серверу.\n" + e.ToShortString());
            _exceptionsLogger.Log(e);
        }
    }

    [RelayCommand]
    private void Save()
    {
        _configurationService.Configuration.MqttAddress = IpWithPort;
        _configurationService.Save();
        _ = _dialogService.ShowMessage("Настройки и модули сохранены");
    }

    [RelayCommand]
    private void Back() => WeakReferenceMessenger.Default.Send<MainViewChangeMessage>(new(MainViewChangeMessage.Views.Main));
}
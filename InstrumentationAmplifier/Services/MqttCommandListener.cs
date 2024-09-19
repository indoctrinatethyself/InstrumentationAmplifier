using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using InstrumentationAmplifier.Services.CommandHandler;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace InstrumentationAmplifier.Services;

public interface IMqttCommandListener
{
    bool Enabled { get; }
    IMqttClient? Client { get; }

    Task Start(CancellationToken ct = default);
    Task Stop();
    Task ChangeOptions(MqttClientOptions options);
}

public class MqttCommandListener : IMqttCommandListener, INotifyPropertyChanged, IDisposable
{
    public const string OnConnectedCommand = "_connected";
    public const string OnDisconnectedCommand = "_disconnected";
    
    private readonly MqttFactory _mqttFactory = new();

    private readonly MqttClientSubscribeOptions _subscribeOptions = new()
    {
        TopicFilters =
        [
            new() { Topic = "IA", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce }
        ]
    };

    private readonly Lazy<ICommandHandler> _commandHandler; // Lazy to avoid recursion
    private readonly IToastService _toastService;

    private MqttClientOptions? _options;
    private IMqttClient? _client;
    private bool _enabled;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MqttCommandListener(Lazy<ICommandHandler> commandHandler, IToastService toastService)
    {
        _commandHandler = commandHandler;
        _toastService = toastService;
    }

    public IMqttClient? Client => _client;

    public bool Enabled
    {
        get => _enabled;
        private set => SetField(ref _enabled, value);
    }

    public async Task Start(CancellationToken ct = default)
    {
        _client?.Dispose();
        _client = _mqttFactory.CreateMqttClient();

        _client.DisconnectedAsync += OnDisconnectedAsync;
        _client.ConnectedAsync += OnConnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

        await _client.ConnectAsync(_options, ct);
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        await _client!.SubscribeAsync(_subscribeOptions);
        Enabled = true;
        
        _commandHandler.Value.Handle(OnConnectedCommand);
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        if (arg.ClientWasConnected == false) return;

        _ = _toastService.ShowMessage("MQTT disconnected", TimeSpan.FromSeconds(2));

        await Task.Delay(TimeSpan.FromSeconds(2));
        try
        {
            using CancellationTokenSource cts = new();
            _ = _toastService.ShowMessage("MQTT reconnecting...", TimeSpan.FromSeconds(5), cancellationToken: cts.Token);
            try { await _client!.ConnectAsync(_client.Options, CancellationToken.None); }
            finally { cts.Cancel(); }
            
            _ = _toastService.ShowMessage("MQTT reconnected", TimeSpan.FromSeconds(1.5));
        }
        catch
        {
            _client!.Dispose();
            _client = null;
            Enabled = false;

            _commandHandler.Value.Handle(OnDisconnectedCommand);
            _ = _toastService.ShowMessage("MQTT disconnected", TimeSpan.FromSeconds(2));
        }
    }

    public Task Start(MqttClientOptions options, CancellationToken ct = default)
    {
        _options = options;
        return Start(ct);
    }

    public async Task Stop()
    {
        _client!.DisconnectedAsync -= OnDisconnectedAsync;
        await _client.DisconnectAsync();
        _client!.Dispose();
        _client = null;

        Enabled = false;
        
        _commandHandler.Value.Handle(OnDisconnectedCommand);
    }

    public async Task ChangeOptions(MqttClientOptions options)
    {
        bool enabled = Enabled;
        if (enabled) await Stop();
        _options = options;
        if (enabled) await Start();
    }

    public static MqttClientOptionsBuilder GetOptionBuilderWithDefaults() =>
        new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId("InstrumentationAmplifier");


    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var message = arg.ApplicationMessage;
        var response = _commandHandler.Value.Handle(message.ConvertPayloadToString());
        if (response != null)
        {
            var responseMessageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(message.ResponseTopic)
                .WithCorrelationData(message.CorrelationData)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithPayload(response.BytesValue);
            var responseMessage = responseMessageBuilder.Build();

            await _client!.PublishAsync(responseMessage);
        }
    }


    public void Dispose()
    {
        _client?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] String? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] String? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace InstrumentationAmplifier.Ad71248;

public class DummyAd71248Device : IAd71248Device
{
    private readonly ChannelRegister[] _channels = Enumerable.Range(0, 16).Select(n => ChannelRegister.Default).ToArray();

    public StatusRegister LastStatus { get; } = new();
    public ErrorRegister LastError { get; } = new();
    public AdcControlRegister AdcControl { get; } = AdcControlRegister.Default;
    public ErrorEnRegister ErrorEn { get; } = ErrorEnRegister.Default;

    public IReadOnlyList<ChannelRegister> Channels => _channels;

    public IReadOnlyList<ConfigurationRegister> Configs { get; } =
        Enumerable.Range(0, 16).Select(n => ConfigurationRegister.Default).ToArray();

    public IReadOnlyList<FilterRegister> Filters { get; } =
        Enumerable.Range(0, 16).Select(n => FilterRegister.Default).ToArray();

    public IReadOnlyList<OffsetRegister> Offsets { get; } =
        Enumerable.Range(0, 16).Select(n => OffsetRegister.Default).ToArray();

    public IReadOnlyList<GainRegister> Gains { get; } =
        Enumerable.Range(0, 16).Select(n => GainRegister.Undefined).ToArray();

    public Int32 DeviceReadyCheckCount { get; set; } = 1000;
    public Int32 DeviceConversationReadyCheckCount { get; set; } = 10000;

    public void Reset() { }

    public OperationResults TryReset() => OperationResults.Success;

    public void Initialize(PowerModes powerMode = PowerModes.LowPower) { }

    public void SetChannel(Byte channelNumber, ChannelRegister channel) => _channels[channelNumber] = channel;

    public void SetConfiguration(Byte configurationNumber, ConfigurationRegister configuration) { }

    public void SetFilter(Byte filterNumber, FilterRegister filter) { }

    public void SetOffset(Byte offsetNumber, OffsetRegister offset) { }

    public void SetGain(Byte gainNumber, GainRegister gain) { }

    public Byte GetId() => 123;

    public IEnumerable<(UInt32 data, Double voltage, StatusRegister status)> GetContinuousData(CancellationToken ct)
    {
        List<(UInt32 data, double voltage, StatusRegister status)> results = new();

        while (!ct.IsCancellationRequested)
        {
            yield return (0, Random.Shared.Next(0, 250000) / 100000.0, new StatusRegister() { ActiveChannel = 0 });
            Thread.Sleep(50);
        }
    }
}
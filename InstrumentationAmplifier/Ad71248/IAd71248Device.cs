using System;
using System.Collections.Generic;

namespace InstrumentationAmplifier.Ad71248;

public interface IAd71248Device
{
    StatusRegister LastStatus { get; }
    ErrorRegister LastError { get; }
    AdcControlRegister AdcControl { get; }
    ErrorEnRegister ErrorEn { get; }
    IReadOnlyList<ChannelRegister> Channels { get; }
    IReadOnlyList<ConfigurationRegister> Configs { get; }
    IReadOnlyList<FilterRegister> Filters { get; }
    IReadOnlyList<OffsetRegister> Offsets { get; }
    IReadOnlyList<GainRegister> Gains { get; }
    int DeviceReadyCheckCount { get; set; }
    int DeviceConversationReadyCheckCount { get; set; }
    void Reset();
    OperationResults TryReset();
    void Initialize(PowerModes powerMode = PowerModes.LowPower);
    void SetChannel(byte channelNumber, ChannelRegister channel);
    void SetConfiguration(byte configurationNumber, ConfigurationRegister configuration);
    void SetFilter(byte filterNumber, FilterRegister filter);
    void SetOffset(byte offsetNumber, OffsetRegister offset);
    void SetGain(byte gainNumber, GainRegister gain);
    byte GetId();
    //ContinuousData GetContinuousData(CancellationToken ct);
    
    public record struct ContinuousMeasurement(UInt32 Data, double Voltage, StatusRegister Status);
}
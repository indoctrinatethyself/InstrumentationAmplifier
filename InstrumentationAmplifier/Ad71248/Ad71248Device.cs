using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using InstrumentationAmplifier.Services;
using ContinuousMeasurement = InstrumentationAmplifier.Ad71248.IAd71248Device.ContinuousMeasurement;
using static InstrumentationAmplifier.Ad71248.Ad71248Constants;

namespace InstrumentationAmplifier.Ad71248;

public class Ad71248Device : IAd71248Device
{
    public const double RefVoltage = 2.5;
    public const int BitsCount = 24;
    
    private StatusRegister _lastStatus = default;
    private ErrorRegister _lastError = default;

    private AdcControlRegister _adcControl = AdcControlRegister.Default;
    private ErrorEnRegister _errorEn = ErrorEnRegister.Default;

    private readonly ChannelRegister[] _channels = new ChannelRegister[16]
    {
        ChannelRegister.Default0, ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default,
        ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default,
        ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default,
        ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default, ChannelRegister.Default,
    };

    private readonly ConfigurationRegister[] _configs = new ConfigurationRegister[8]
    {
        ConfigurationRegister.Default, ConfigurationRegister.Default,
        ConfigurationRegister.Default, ConfigurationRegister.Default,
        ConfigurationRegister.Default, ConfigurationRegister.Default,
        ConfigurationRegister.Default, ConfigurationRegister.Default,
    };

    private readonly FilterRegister[] _filters = new FilterRegister[8]
    {
        FilterRegister.Default, FilterRegister.Default, FilterRegister.Default, FilterRegister.Default,
        FilterRegister.Default, FilterRegister.Default, FilterRegister.Default, FilterRegister.Default,
    };

    private readonly OffsetRegister[] _offsets = new OffsetRegister[8]
    {
        OffsetRegister.Default, OffsetRegister.Default, OffsetRegister.Default, OffsetRegister.Default,
        OffsetRegister.Default, OffsetRegister.Default, OffsetRegister.Default, OffsetRegister.Default,
    };

    public readonly GainRegister[] _gains = new GainRegister[8]
    {
        GainRegister.Undefined, GainRegister.Undefined, GainRegister.Undefined, GainRegister.Undefined,
        GainRegister.Undefined, GainRegister.Undefined, GainRegister.Undefined, GainRegister.Undefined,
    };

    private readonly ISpiDevice _spi;

    public Ad71248Device(ISpiDevice spi)
    {
        _spi = spi;
    }

    public StatusRegister LastStatus => _lastStatus;
    public ErrorRegister LastError => _lastError;

    public AdcControlRegister AdcControl => _adcControl;
    public ErrorEnRegister ErrorEn => _errorEn;
    public IReadOnlyList<ChannelRegister> Channels => _channels;
    public IReadOnlyList<ConfigurationRegister> Configs => _configs;
    public IReadOnlyList<FilterRegister> Filters => _filters;
    public IReadOnlyList<OffsetRegister> Offsets => _offsets;
    public IReadOnlyList<GainRegister> Gains => _gains;

    public int DeviceReadyCheckCount { get; set; } = 1000;
    public int DeviceConversationReadyCheckCount { get; set; } = 10000;

    private OperationResults ReadRegister(byte reg, int bytesCount, out UInt64 value)
    {
        if (bytesCount is < 1 or > sizeof(UInt64)) throw new ArgumentException(nameof(bytesCount));

        Span<byte> buf = stackalloc byte[bytesCount];
        var result = ReadRegister(reg, buf);

        value = 0;
        for (int i = 0; i < bytesCount; i++)
            value |= (UInt64)buf[bytesCount - i - 1] << (i * 8);

        return result;
    }

    private OperationResults ReadRegisterWithoutCheck(byte reg, Span<byte> readBuffer)
    {
        bool crcEnable = _errorEn.SpiCrcErrEn;
        int bufferSize = 1 + readBuffer.Length + (crcEnable ? 1 : 0);
        Span<byte> buf = stackalloc byte[bufferSize];
        byte command = (byte)(CommRegRead | (reg & 0b00111111u));
        buf[0] = command;

        _spi.TransferFullDuplex(buf, buf);
        buf[1..(readBuffer.Length + 1)].CopyTo(readBuffer);
        byte receivedCrc = buf[^1];

        if (crcEnable)
        {
            buf[0] = command;
            byte crc = ComputeCrc8(buf);

            if (crc != 0) return OperationResults.CrcFail; // TODO: several attempts
            // If the last byte is equal to the crc of the previous bytes, then it becomes 0.
        }

        return OperationResults.Success;
    }

    private OperationResults ReadRegister(byte reg, Span<byte> readBuffer)
    {
        if (reg != RegError && _errorEn.SpiIgnoreErrEn)
        {
            var result = WaitForSpiReady(DeviceReadyCheckCount);
            if (result < 0) return result;
        }

        return ReadRegisterWithoutCheck(reg, readBuffer);
    }

    private OperationResults WaitForSpiReady(int timeout)
    {
        bool ready = false;

        Span<byte> buf = stackalloc byte[4];
        while (!ready && timeout-- > 0)
        {
            buf.Fill(0);
            var errBuf = buf[1..4];
            var result = ReadRegister(RegError, errBuf);
            if (result < 0) return result;

            ErrorRegister reg = new ErrorRegister(BinaryPrimitives.ReadUInt32BigEndian(buf));
            ready = reg.SpiIgnoreErr == false;
        }

        return ready ? OperationResults.Success : OperationResults.Timeout;
    }

    private OperationResults WriteRegisterWithoutCheck(byte reg, Span<byte> writeBuffer)
    {
        bool crcEnable = _errorEn.SpiCrcErrEn;
        int bufferSize = 1 + writeBuffer.Length + (crcEnable ? 1 : 0);
        Span<byte> buf = stackalloc byte[bufferSize];
        byte command = (byte)(reg & 0b00111111u);
        buf[0] = command;
        writeBuffer.CopyTo(buf[1..(writeBuffer.Length + 1)]);

        if (crcEnable) buf[^1] = ComputeCrc8(buf[..^1]);

        _spi.Write(buf);

        return OperationResults.Success;
    }

    private OperationResults WriteRegister(byte reg, Span<byte> writeBuffer)
    {
        if (_errorEn.SpiIgnoreErrEn)
        {
            var result = WaitForSpiReady(DeviceReadyCheckCount);
            if (result < 0) return result;
        }

        return WriteRegisterWithoutCheck(reg, writeBuffer);
    }

    private OperationResults WaitToPowerOn(int timeout)
    {
        bool powerOn = false;

        while (!powerOn && timeout-- > 0)
        {
            var result = ReadRegister(RegStatus, 1, out var reg);
            if (result < 0) return result;
            StatusRegister status = new((byte)reg);
            powerOn = status.PowerOnReset == false;
            Thread.Sleep(1);
        }

        return powerOn ? OperationResults.Success : OperationResults.Timeout;
    }

    private OperationResults WaitForConversationReady(int timeout, CancellationToken ct = default)
    {
        bool ready = false;

        while (!ready && timeout-- > 0)
        {
            ct.ThrowIfCancellationRequested();
            var result = ReadRegister(RegStatus, 1, out var reg);
            if (result < 0) return result;
            StatusRegister status = new((byte)reg);
            ready = status.Rdy == false;
            Thread.Sleep(1);
        }

        return ready ? OperationResults.Success : OperationResults.Timeout;
    }

    public void Reset() => TryReset().ThrowIfFail();

    public OperationResults TryReset()
    {
        _spi.Write(ResetCommand);

        _lastStatus = default;
        _lastError = default;
        _errorEn = ErrorEnRegister.Default;
        _channels[0] = ChannelRegister.Default0;
        Array.Fill(_channels, ChannelRegister.Default);
        Array.Fill(_configs, ConfigurationRegister.Default);
        Array.Fill(_filters, FilterRegister.Default);
        Array.Fill(_offsets, OffsetRegister.Default);
        Array.Fill(_gains, GainRegister.Undefined);

        var result = WaitToPowerOn(DeviceReadyCheckCount);
        Thread.Sleep(4);
        if (result.IsFail()) return result;

        for (byte i = 0; i < 8; i++)
        {
            result = ReadRegister(RegGain(i), 3, out var value);
            if (result.IsFail()) return result;
            _gains[i] = new GainRegister((UInt32)value);
        }

        return result;
    }

    public void Initialize(PowerModes powerMode = PowerModes.LowPower)
    {
        Reset();

        ErrorEnRegister errorEn = new() { SpiCrcErrEn = true, SpiIgnoreErrEn = true };
        WriteRegister(RegErrorEn, errorEn.GetBytes()).ThrowIfFail();
        _errorEn = errorEn;

        AdcControlRegister adcControl = new()
        {
            Mode = OperatingModes.StandbyMode,
            PowerMode = powerMode,
            RefEn = true, CsEn = true, DataStatus = true
        };
        WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
        _adcControl = adcControl;

        _channels[0] = ChannelRegister.Default;
        WriteRegister(RegChannel0, _channels[0].GetBytes()).ThrowIfFail();
    }

    public void SetChannel(byte channelNumber, ChannelRegister channel)
    {
        WriteRegister(RegChannel(channelNumber), channel.GetBytes()).ThrowIfFail();
        _channels[channelNumber] = channel;
    }

    public void SetConfiguration(byte configurationNumber, ConfigurationRegister configuration)
    {
        WriteRegister(RegConfig(configurationNumber), configuration.GetBytes()).ThrowIfFail();
        _configs[configurationNumber] = configuration;
    }

    public void SetFilter(byte filterNumber, FilterRegister filter)
    {
        WriteRegister(RegFilter(filterNumber), filter.GetBytes()).ThrowIfFail();
        _filters[filterNumber] = filter;
    }

    public void SetOffset(byte offsetNumber, OffsetRegister offset)
    {
        WriteRegister(RegOffset(offsetNumber), offset.GetBytes()).ThrowIfFail();
        _offsets[offsetNumber] = offset;
    }

    public void SetGain(byte gainNumber, GainRegister gain)
    {
        WriteRegister(RegGain(gainNumber), gain.GetBytes()).ThrowIfFail();
        _gains[gainNumber] = gain;
    }

    public byte GetId()
    {
        var result = ReadRegister(RegId, 1, out var value);
        result.ThrowIfFail();
        return (byte)value;
    }
    
    public Dictionary<int, (UInt32 data, double voltage)> GetSingleData()
    {
        AdcControlRegister adcControl = new()
        {
            Mode = OperatingModes.SingleConversionMode,
            RefEn = true, CsEn = true, DataStatus = true
        };
        WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
        _adcControl = adcControl;

        Thread.Sleep(2);

        Dictionary<int, (UInt32 data, double voltage)> results = new();

        Span<byte> buf2 = stackalloc byte[2];
        do
        {
            WaitForConversationReady(DeviceConversationReadyCheckCount).ThrowIfFail();

            ReadRegister(RegData, 4, out var value).ThrowIfFail();

            UInt32 data = (UInt32)(value >> 8);
            StatusRegister status = new((byte)value);
            _lastStatus = status;
            double voltage = ConvertSampleToVoltage(status.ActiveChannel, data);
            results[status.ActiveChannel] = (data, voltage);

            ReadRegister(RegAdcControl, buf2).ThrowIfFail();
            AdcControlRegister adcControlRegister = new(BinaryPrimitives.ReadUInt16BigEndian(buf2));
            _adcControl = adcControlRegister;
        }
        while (_adcControl.Mode == OperatingModes.SingleConversionMode);

        return results;
    }
    
    /*public IEnumerable<ContinuousMeasurement> GetContinuousData(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        AdcControlRegister adcControl = new()
        {
            Mode = OperatingModes.ContinuousConversionMode,
            RefEn = true, CsEn = true, DataStatus = true
        };
        WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
        _adcControl = adcControl;

        Thread.Sleep(5);

        try
        {
            while (true)
            {
                WaitForConversationReady(DeviceConversationReadyCheckCount, ct).ThrowIfFail();

                ReadRegister(RegData, 4, out var value).ThrowIfFail();

                UInt32 data = (UInt32)(value >> 8);
                StatusRegister status = new((byte)value);
                _lastStatus = status;
                double voltage = ConvertSampleToVoltage(status.ActiveChannel, data);
                yield return new(data, voltage, status);
                Thread.Sleep(1);
            }
        }
        finally
        {
            Thread.Sleep(5);

            adcControl = new()
            {
                Mode = OperatingModes.StandbyMode,
                RefEn = true, CsEn = true, DataStatus = true
            };
            WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
            _adcControl = adcControl;
        }
    }*/

    public ContinuousData GetContinuousData(CancellationToken ct) => new(this, ct);

    public sealed class ContinuousData : IEnumerable<ContinuousMeasurement>, IDisposable
    {
        private readonly Ad71248Device _ad71248Device;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _ct;
        
        public ContinuousData(Ad71248Device ad71248Device, CancellationToken ct)
        {
            _ad71248Device = ad71248Device;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ct = _cts.Token;
        }
        
        public IEnumerator<ContinuousMeasurement> GetEnumerator()
        {
            _ct.ThrowIfCancellationRequested();
            AdcControlRegister adcControl = new()
            {
                Mode = OperatingModes.ContinuousConversionMode,
                RefEn = true, CsEn = true, DataStatus = true
            };
            _ad71248Device.WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
            _ad71248Device._adcControl = adcControl;

            Thread.Sleep(5);
            
            while (true)
            {
                _ad71248Device.WaitForConversationReady(_ad71248Device.DeviceConversationReadyCheckCount, _ct).ThrowIfFail();

                _ad71248Device.ReadRegister(RegData, 4, out var value).ThrowIfFail();

                UInt32 data = (UInt32)(value >> 8);
                StatusRegister status = new((byte)value);
                _ad71248Device._lastStatus = status;
                double voltage = _ad71248Device.ConvertSampleToVoltage(status.ActiveChannel, data);
                yield return new(data, voltage, status);
                Thread.Sleep(1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            Thread.Sleep(5);

            AdcControlRegister adcControl = new()
            {
                Mode = OperatingModes.StandbyMode,
                RefEn = true, CsEn = true, DataStatus = true
            };
            _ad71248Device.WriteRegister(RegAdcControl, adcControl.GetBytes()).ThrowIfFail();
            _ad71248Device._adcControl = adcControl;
        }
    }
    

    private double ConvertSampleToVoltage(byte channel, UInt32 sample)
    {
        byte channelConfigNumber = _channels[channel].Setup;
        var channelConfig = _configs[channelConfigNumber];
        bool isBipolar = channelConfig.Bipolar;
        Pga channelPga = channelConfig.Pga;

        double convertedValue;
        byte gain = (byte)(1 << (byte)channelPga);

        if (isBipolar)
        {
            const ulong max = 1 << (BitsCount - 1);
            convertedValue = ((double)sample / max - 1) * (RefVoltage / gain);
        }
        else
        {
            const ulong max = 1 << BitsCount;
            convertedValue = sample * RefVoltage / (gain * max);
        }

        return convertedValue;
    }

    private static byte ComputeCrc8(Span<byte> buf)
    {
        byte crc = 0;

        for (int bufIndex = 0; bufIndex < buf.Length; bufIndex++)
        {
            byte b = buf[bufIndex];
            for (byte i = 0x80; i != 0; i >>= 1)
            {
                bool cmp1 = (crc & 0x80) != 0; // 7 bit = 1
                bool cmp2 = (b & i) != 0; // i bit = 1
                if (cmp1 != cmp2)
                {
                    /* MSB of CRC register XOR input Bit from Data */
                    crc <<= 1;
                    crc ^= 0b111;
                }
                else
                    crc <<= 1;

                /*Console.WriteLine($"b: {Convert.ToString(b, 2).PadLeft(8, '0')}" +
                                  $" i: {Convert.ToString(i, 2).PadLeft(8, '0')}" +
                                  $" crc: {Convert.ToString(crc, 2).PadLeft(8, '0')}");*/
            }
        }

        return crc;
    }
}

public enum OperationResults
{
    Success = 0,
    Timeout = -1,
    CrcFail = -2,
}

public static class OperationResultsExtensions
{
    public static void ThrowIfFail(this OperationResults result)
    {
        if (result == OperationResults.Success) return;
        if (result == OperationResults.Timeout) throw new IOException(result.ToString("G"));
    }

    public static bool IsFail(this OperationResults result) => result != OperationResults.Success;
}
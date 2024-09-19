using System;
using System.Device.Spi;
using InstrumentationAmplifier.Devices;

namespace InstrumentationAmplifier.Services;

public interface ISpiDevice : IDisposable
{
    byte ReadByte();
    void Read(Span<byte> buffer);
    void WriteByte(byte value);
    void Write(ReadOnlySpan<byte> buffer);
    void TransferFullDuplex(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer);
}

public interface ISpiDeviceWrapper : ISpiDevice
{
    SpiDeviceOptions Options { get; }
    SpiDevice Spi { get; }
}

public class SpiDeviceFactory
{
    public ISpiDeviceWrapper CreateInstance(SpiDeviceOptions options) => CreateInstance(options, _ => { });

    public ISpiDeviceWrapper CreateInstance(SpiDeviceOptions options, Action<SpiConnectionSettings> configure)
    {
        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            return new DummySpiDevice(options);
        return new DefaultSpiDevice(options, configure);
    }
}
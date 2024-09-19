using System;
using System.Device.Gpio;
using System.Device.Spi;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.Devices;

public class DefaultSpiDevice : ISpiDeviceWrapper
{
    public SpiDeviceOptions Options { get; }
    public SpiDevice Spi { get; }

    public DefaultSpiDevice(SpiDeviceOptions options) : this(options, _ => { }) { }

    public DefaultSpiDevice(SpiDeviceOptions options, Action<SpiConnectionSettings> configure)
    {
        Options = options;
        SpiConnectionSettings spiConnectionSettings = new((int)Options.SpiBus.Number, (int)Options.LePin.Number)
        {
            ChipSelectLineActiveState = PinValue.High
        };
        configure(spiConnectionSettings);
        Spi = SpiDevice.Create(spiConnectionSettings);
    }

    public Byte ReadByte() => Spi.ReadByte();

    public void Read(Span<Byte> buffer) => Spi.Read(buffer);

    public void WriteByte(Byte value) => Spi.WriteByte(value);

    public void Write(ReadOnlySpan<Byte> buffer) => Spi.Write(buffer);

    public void TransferFullDuplex(ReadOnlySpan<Byte> writeBuffer, Span<Byte> readBuffer) =>
        Spi.TransferFullDuplex(writeBuffer, readBuffer);

    public void WriteThenRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
    {
        int len = writeBuffer.Length + readBuffer.Length;
        Span<byte> buf = stackalloc byte[len];
        writeBuffer.CopyTo(buf);

        Spi.TransferFullDuplex(buf, buf);
        buf[writeBuffer.Length..].CopyTo(readBuffer);
    }

    public void Dispose() => Spi.Dispose();
}

public class DummySpiDevice : ISpiDeviceWrapper
{
    public DummySpiDevice(SpiDeviceOptions options) => Options = options;

    public SpiDeviceOptions Options { get; }
    public SpiDevice Spi => null!;

    public Byte ReadByte() => 0;
    public void Read(Span<Byte> buffer) { }
    public void WriteByte(Byte value) { }
    public void Write(ReadOnlySpan<Byte> buffer) { }
    public void TransferFullDuplex(ReadOnlySpan<Byte> writeBuffer, Span<Byte> readBuffer) { }
    public void Dispose() { }
}
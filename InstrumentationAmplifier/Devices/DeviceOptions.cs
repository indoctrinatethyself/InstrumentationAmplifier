using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace InstrumentationAmplifier.Devices;

public abstract record DeviceOptions
{
    public abstract DeviceOptionsJsonModel ToJsonModel();
}

public record SpiDeviceOptions(SpiBus SpiBus, SpiBusLe LePin) : DeviceOptions
{
    public override SpiDeviceOptionsJsonModel ToJsonModel() => new(SpiBus.Number, LePin.Number);
}

public record I2cDeviceOptions(I2cBus I2cBus, uint I2cAddress) : DeviceOptions
{
    public override I2cDeviceOptionsJsonModel ToJsonModel() => new(I2cBus.Number, I2cAddress);
}

public record ParallelDeviceOptions(IReadOnlyList<ParallelPin> Pins) : DeviceOptions
{
    public override ParallelDeviceOptionsJsonModel ToJsonModel() => new(Pins.Select(p => p.Pin.Gpio).ToImmutableArray());
}


[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(SpiDeviceOptionsJsonModel), typeDiscriminator: "SPI")]
[JsonDerivedType(typeof(I2cDeviceOptionsJsonModel), typeDiscriminator: "I2C")]
[JsonDerivedType(typeof(ParallelDeviceOptionsJsonModel), typeDiscriminator: "Parallel")]
public abstract record DeviceOptionsJsonModel
{
    public abstract DeviceOptions ConvertBack();
}

public record SpiDeviceOptionsJsonModel(uint SpiBus, uint LePin) : DeviceOptionsJsonModel
{
    public override SpiDeviceOptions ConvertBack()
    {
        var spiBus = RaspberryPiAvailableDevices.Spi.First(b => b.Number == SpiBus);
        var lePin = spiBus.Le.First(l => l.Number == LePin);
        return new(spiBus, lePin);
    }
}

public record I2cDeviceOptionsJsonModel(uint I2cBus, uint I2cAddress) : DeviceOptionsJsonModel
{
    public override I2cDeviceOptions ConvertBack()
    {
        var i2cBus = RaspberryPiAvailableDevices.I2c.First(b => b.Number == I2cBus);
        return new(i2cBus, I2cAddress);
    }
}

public record ParallelDeviceOptionsJsonModel(IReadOnlyList<uint> Pins) : DeviceOptionsJsonModel
{
    public override ParallelDeviceOptions ConvertBack() => new(
        Pins.Select(pin => RaspberryPiAvailableDevices.Gpio.First(avPin => avPin.Pin.Gpio == pin)).ToImmutableArray());
}
using System;
using System.Collections.Generic;
using System.Reflection;

namespace InstrumentationAmplifier.Devices;

public record SpiBus(uint Number, IReadOnlyList<SpiBusLe> Le, Pin Mosi, Pin Miso, Pin Clk);

public record SpiBusLe(uint Number, Pin Pin)
{
    public List<Module> UsedBy { get; set; } = new();
    public bool IsUsed => UsedBy.Count > 0;
};

public record I2cBus(uint Number, Pin Sda, Pin Scl)
{
    public Dictionary<uint, List<Module>> UsedAddresses { get; set; } = new();
}

public record ParallelPin(Pin Pin)
{
    public List<Module> UsedBy { get; set; } = new();
    public bool IsUsed => UsedBy.Count > 0;
}

public readonly struct Pin
{
    public Pin(UInt32 gpio, UInt32 number)
    {
        Gpio = gpio;
        Number = number;
    }

    public UInt32 Gpio { get; }

    public UInt32 Number { get; }

    public static implicit operator Pin((uint gpio, uint number) t) => new(t.gpio, t.number);
    //public static implicit operator uint(Pin p) => p.Gpio;
}
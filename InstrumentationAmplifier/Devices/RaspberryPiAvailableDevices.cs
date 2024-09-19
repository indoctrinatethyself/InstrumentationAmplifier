using System.Collections.Immutable;

namespace InstrumentationAmplifier.Devices;

public static class RaspberryPiAvailableDevices
{
    public static readonly SpiBusLe SpiBus0Le0 = new(0, (8, 24));
    public static readonly SpiBusLe SpiBus0Le1 = new(1, (7, 26));

    public static readonly SpiBus SpiBus0 = new(0,
        new[] { SpiBus0Le0, SpiBus0Le1 }.ToImmutableArray(),
        (10, 19), (9, 21), (11, 23));
    
    /*public static readonly SpiBusLe SpiBus1Le0 = new(0, (18, 12));
    public static readonly SpiBusLe SpiBus1Le2 = new(1, (17, 11));
    public static readonly SpiBusLe SpiBus1Le3 = new(2, (16, 36));

    public static readonly SpiBus SpiBus1 = new(1,
        new[] { SpiBus1Le0, SpiBus1Le2, SpiBus1Le3 }.ToImmutableArray(),
        (20, 38), (19, 35), (21, 40));*/

    public static ImmutableArray<SpiBus> Spi { get; } = new[] { SpiBus0/*, SpiBus1*/ } .ToImmutableArray();


    public static readonly I2cBus I2cBus1 = new(1, (2, 3), (3, 5));
    public static ImmutableArray<I2cBus> I2c { get; } = new[] { I2cBus1 } .ToImmutableArray();

    
    public static readonly ParallelPin Gpio5 = new((5, 29));
    public static readonly ParallelPin Gpio6 = new((6, 31));
    public static readonly ParallelPin Gpio12 = new((12, 32));
    public static readonly ParallelPin Gpio13 = new((13, 33));
    public static readonly ParallelPin Gpio14 = new((14, 8));
    public static readonly ParallelPin Gpio15 = new((15, 10));
    public static readonly ParallelPin Gpio16 = new((16, 36));
    public static readonly ParallelPin Gpio17 = new((17, 11));
    public static readonly ParallelPin Gpio22 = new((22, 15));
    public static readonly ParallelPin Gpio23 = new((23, 16));
    public static readonly ParallelPin Gpio24 = new((24, 18));
    public static readonly ParallelPin Gpio25 = new((25, 22));
    public static readonly ParallelPin Gpio26 = new((26, 37));
    public static readonly ParallelPin Gpio27 = new((27, 13));

    public static ImmutableArray<ParallelPin> Gpio { get; } = new[]
        {
            Gpio5, Gpio6, Gpio12, Gpio13, Gpio14, Gpio15,
            Gpio22, Gpio23, Gpio24, Gpio25, Gpio26, Gpio27
        }
        .ToImmutableArray();
}
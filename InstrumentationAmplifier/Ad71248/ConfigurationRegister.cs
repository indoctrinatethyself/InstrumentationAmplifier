using System;
using System.ComponentModel;
using InstrumentationAmplifier.Utils;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct ConfigurationRegister
{
    private UInt16 _register;

    public bool Bipolar { get => GetBit(_register, 11); set => SetBit(ref _register, 11, value); }

    public BurnoutCurrent Burnout
    {
        get => (BurnoutCurrent)GetBits(_register, 9, 2);
        set => SetBits(ref _register, 9, 2, (byte)value);
    }

    public bool RefBufp { get => GetBit(_register, 8); set => SetBit(ref _register, 8, value); }
    public bool RefBufm { get => GetBit(_register, 7); set => SetBit(ref _register, 7, value); }
    public bool AinBufp { get => GetBit(_register, 6); set => SetBit(ref _register, 6, value); }
    public bool AinBufm { get => GetBit(_register, 5); set => SetBit(ref _register, 5, value); }

    public RefSel RefSel
    {
        get => (RefSel)GetBits(_register, 3, 2);
        set => SetBits(ref _register, 3, 2, (byte)value);
    }

    public Pga Pga
    {
        get => (Pga)GetBits(_register, 0, 3);
        set => SetBits(ref _register, 0, 3, (byte)value);
    }

    public UInt16 Register => _register;
    public byte[] GetBytes() => BinaryUtils.GetBytes(_register);

    public static readonly ConfigurationRegister Default = new() { AinBufm = true, AinBufp = true, Bipolar = true };
}

public enum BurnoutCurrent : byte
{
    [Description("Off")] BurnoutOff = 0b00,
    [Description("0.5 μA")] Burnout500Na = 0b01,
    [Description("2 μA")] Burnout2Ua = 0b10,
    [Description("4 μA")] Burnout4Ua = 0b11
}

public enum RefSel : byte
{
    [Description("REFIN1(+)/REFIN1(−)")] RefIn1 = 0b00,
    [Description("REFIN2(+)/REFIN2(−)")] RefIn2 = 0b01,
    [Description("Internal reference")] InternalReference = 0b10,
    [Description("AVdd")] AVdd = 0b11
}

public enum Pga : byte
{
    [Description("1, ±2.5 V")] Pga1 = 0b000, /* Gain 1, Input Range When VREF = 2.5 V: ±2.5 V */
    [Description("2, ±1.25 V")] Pga2 = 0b001, /* Gain 2, Input Range When VREF = 2.5 V: ±1.25 V */
    [Description("4, ±625 mV")] Pga4 = 0b010, /* Gain 4, Input Range When VREF = 2.5 V: ± 625 mV */
    [Description("8, ±312.5 mV")] Pga8 = 0b011, /* Gain 8, Input Range When VREF = 2.5 V: ±312.5 mV */
    [Description("16, ±156.25 mV")] Pga16 = 0b100, /* Gain 16, Input Range When VREF = 2.5 V: ±156.25 mV */
    [Description("32, ±78.125 mV")] Pga32 = 0b101, /* Gain 32, Input Range When VREF = 2.5 V: ±78.125 mV */
    [Description("64, ±39.06 mV")] Pga64 = 0b110, /* Gain 64, Input Range When VREF = 2.5 V: ±39.06 mV */
    [Description("128, ±19.53 mV")] Pga128 = 0b111 /* Gain 128, Input Range When VREF = 2.5 V: ±19.53 mV */
}

public static class PgaExtensions
{
    public static Pga Next(this Pga pga) => pga >= Pga.Pga128 ? pga : pga + 1;
    public static Pga Previous(this Pga pga) => pga <= Pga.Pga1 ? pga : pga - 1;
    public static double GetMaxVoltage(this Pga pga) => Ad71248Device.RefVoltage / (1 << (int)pga);
    public static byte Gain(this Pga pga) => (byte)(1 << (byte)pga);
}
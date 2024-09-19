using System;
using System.ComponentModel;
using InstrumentationAmplifier.Utils;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct AdcControlRegister
{
    private UInt16 _register;

    public AdcControlRegister() { }
    public AdcControlRegister(UInt16 register) => _register = (UInt16)(register & 0b000_1_1_1_1_1_11_1111_11u);

    public bool DoutRdyDel { get => GetBit(_register, 12); set => SetBit(ref _register, 12, value); }
    public bool ContRead { get => GetBit(_register, 11); set => SetBit(ref _register, 11, value); }
    public bool DataStatus { get => GetBit(_register, 10); set => SetBit(ref _register, 10, value); }
    public bool CsEn { get => GetBit(_register, 9); set => SetBit(ref _register, 9, value); }
    public bool RefEn { get => GetBit(_register, 8); set => SetBit(ref _register, 8, value); }

    public PowerModes PowerMode
    {
        get => (PowerModes)GetBits(_register, 6, 2);
        set => SetBits(ref _register, 6, 2, (byte)value);
    }
    
    public OperatingModes Mode
    {
        get => (OperatingModes)GetBits(_register, 2, 4);
        set => SetBits(ref _register, 2, 4, (byte)value);
    }
    
    public AdcClockSources ClkSel
    {
        get => (AdcClockSources)GetBits(_register, 0, 2);
        set => SetBits(ref _register, 0, 2, (byte)value);
    }
    
    public UInt16 Register => _register;
    public byte[] GetBytes() => BinaryUtils.GetBytes(_register);

    public static readonly AdcControlRegister Default = new() { };

    public override string ToString() => $"{nameof(DoutRdyDel)}: {DoutRdyDel}, {nameof(ContRead)}: {ContRead}, {nameof(DataStatus)}: {DataStatus}, {nameof(CsEn)}: {CsEn}, {nameof(RefEn)}: {RefEn}, {nameof(PowerMode)}: {PowerMode}, {nameof(Mode)}: {Mode}, {nameof(ClkSel)}: {ClkSel}, {nameof(Register)}: {Register}";
}

public enum OperatingModes : byte
{
    ContinuousConversionMode = 0b0000,
    SingleConversionMode = 0b0001,
    StandbyMode = 0b0010,
    PowerDownMode = 0b0011,
    IdleMode = 0b0100,
    InternalZeroScaleCalibration = 0b0101, // offset
    InternalFullScaleCalibration = 0b0110, // gain
    SystemZeroScaleCalibration = 0b0111, // offset
    SystemFullScaleCalibration = 0b1000, // gain
}

public enum PowerModes : byte
{
    [Description("Низкое")] LowPower = 0b00,
    [Description("Среднее")] MidPower = 0b01,
    [Description("Высокое")] FullPower = 0b10,
}

public enum AdcClockSources : byte
{
    InternalClk = 0b00,
    InternalWithOutputClk = 0b01,
    ExternalClk = 0b10,
    ExternalDiv4Clk = 0b11,
}
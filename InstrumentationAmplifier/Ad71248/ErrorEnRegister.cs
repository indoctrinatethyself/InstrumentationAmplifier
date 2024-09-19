using System;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct ErrorEnRegister
{
    private UInt32 _register;

    public bool MclkCntEn { get => GetBit(_register, 22); set => SetBit(ref _register, 22, value); }
    public bool LdoCapChkTestEn { get => GetBit(_register, 21); set => SetBit(ref _register, 21, value); }

    public CapacitorCheckModes LdoCapChk
    {
        get => (CapacitorCheckModes)GetBits(_register, 19, 2);
        set => SetBits(ref _register, 19, 2, (byte)value);
    }

    public bool AdcCalErrEn { get => GetBit(_register, 18); set => SetBit(ref _register, 18, value); }
    public bool AdcConvErrEn { get => GetBit(_register, 17); set => SetBit(ref _register, 17, value); }
    public bool AdcSatErrEn { get => GetBit(_register, 16); set => SetBit(ref _register, 16, value); }
    public bool AinpOvErrEn { get => GetBit(_register, 15); set => SetBit(ref _register, 15, value); }
    public bool AinpUvErrEn { get => GetBit(_register, 14); set => SetBit(ref _register, 14, value); }
    public bool AinmOvErrEn { get => GetBit(_register, 13); set => SetBit(ref _register, 13, value); }
    public bool AinmUvErrEn { get => GetBit(_register, 12); set => SetBit(ref _register, 12, value); }
    public bool RefDetErrEn { get => GetBit(_register, 11); set => SetBit(ref _register, 11, value); }
    public bool DldoPsmTripTestEn { get => GetBit(_register, 10); set => SetBit(ref _register, 10, value); }
    public bool DldoPsmErrErrEn { get => GetBit(_register, 9); set => SetBit(ref _register, 9, value); }
    public bool AldoPsmTripTestEn { get => GetBit(_register, 8); set => SetBit(ref _register, 8, value); }
    public bool AldoPsmErrEn { get => GetBit(_register, 7); set => SetBit(ref _register, 7, value); }
    public bool SpiIgnoreErrEn { get => GetBit(_register, 6); set => SetBit(ref _register, 6, value); }
    public bool SpiSclkCntErrEn { get => GetBit(_register, 5); set => SetBit(ref _register, 5, value); }
    public bool SpiReadErrEn { get => GetBit(_register, 4); set => SetBit(ref _register, 4, value); }
    public bool SpiWriteErrEn { get => GetBit(_register, 3); set => SetBit(ref _register, 3, value); }
    public bool SpiCrcErrEn { get => GetBit(_register, 2); set => SetBit(ref _register, 2, value); }
    public bool MmCrcErrEn { get => GetBit(_register, 1); set => SetBit(ref _register, 1, value); }
    public bool RomCrcErrEn { get => GetBit(_register, 0); set => SetBit(ref _register, 0, value); }

    public UInt32 Register => _register;

    public byte[] GetBytes() => GetBytes24(_register);

    public static readonly ErrorEnRegister Default = new() { SpiIgnoreErrEn = true };
}

public enum CapacitorCheckModes : byte
{
    Disable = 0b00,
    CheckTheAnalogLdoCapacitor = 0b01,
    CheckTheDigitalLdoCapacitor = 0b10
}
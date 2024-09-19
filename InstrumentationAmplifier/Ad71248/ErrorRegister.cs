using System;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct ErrorRegister
{
    private UInt32 _register;

    /// <summary> new ErrorRegister() { Register = register } </summary>
    public ErrorRegister(UInt32 register) => Register = register;

    public bool LdoCapErr { get => GetBit(_register, 19); set => SetBit(ref _register, 19, value); }
    public bool AdcCalErr { get => GetBit(_register, 18); set => SetBit(ref _register, 18, value); }
    public bool AdcConvErr { get => GetBit(_register, 17); set => SetBit(ref _register, 17, value); }
    public bool AdcSatErr { get => GetBit(_register, 16); set => SetBit(ref _register, 16, value); }
    public bool AinpOvErr { get => GetBit(_register, 15); set => SetBit(ref _register, 15, value); }
    public bool AinpUvErr { get => GetBit(_register, 14); set => SetBit(ref _register, 14, value); }
    public bool AinmOvErr { get => GetBit(_register, 13); set => SetBit(ref _register, 13, value); }
    public bool AinmUvErr { get => GetBit(_register, 12); set => SetBit(ref _register, 12, value); }
    public bool RefDetErr { get => GetBit(_register, 11); set => SetBit(ref _register, 11, value); }
    public bool DldoPsmErr { get => GetBit(_register, 9); set => SetBit(ref _register, 9, value); }
    public bool AldoPsmErr { get => GetBit(_register, 7); set => SetBit(ref _register, 7, value); }
    public bool SpiIgnoreErr { get => GetBit(_register, 6); set => SetBit(ref _register, 6, value); }
    public bool SpiSclkCntErr { get => GetBit(_register, 5); set => SetBit(ref _register, 5, value); }
    public bool SpiReadErr { get => GetBit(_register, 4); set => SetBit(ref _register, 4, value); }
    public bool SpiWriteErr { get => GetBit(_register, 3); set => SetBit(ref _register, 3, value); }
    public bool SpiCrcErr { get => GetBit(_register, 2); set => SetBit(ref _register, 2, value); }
    public bool MmCrcErr { get => GetBit(_register, 1); set => SetBit(ref _register, 1, value); }
    public bool RomCrcErr { get => GetBit(_register, 0); set => SetBit(ref _register, 0, value); }

    /// <summary> 24bit value. <br/> Mask 00001111_11111010_11111111. </summary>
    public UInt32 Register
    {
        get => _register;
        set => _register = value & 0b_00000000_00001111_11111010_11111111u;
    }

    public byte[] GetBytes() => GetBytes24(_register);
}
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct StatusRegister
{
    private byte _register;

    public bool Rdy { get => GetBit(_register, 7); set => SetBit(ref _register, 7, value); }
    public bool ErrorFlag { get => GetBit(_register, 6); set => SetBit(ref _register, 6, value); }
    public bool PowerOnReset { get => GetBit(_register, 4); set => SetBit(ref _register, 4, value); }
    
    /// <summary> 0-15 </summary>
    public byte ActiveChannel
    {
        get => GetBits(_register, 0, 4);
        set => SetBits(ref _register, 0, 4, value);
    }
    
    public byte Register => _register;

    public StatusRegister(byte register)
    {
        _register = (byte)(register & 0b11011111u);
    }
}
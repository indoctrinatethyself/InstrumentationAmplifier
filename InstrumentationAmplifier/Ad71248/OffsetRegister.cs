using System;
using InstrumentationAmplifier.Utils;

namespace InstrumentationAmplifier.Ad71248;

/// <summary>
/// The AD7124-8 has eight offset registers, OFFSET_0 to OFFSET_7.
/// Each offset register is associated with a setup; OFFSET_x is associated with Setup x.
/// The offset registers are 24-bit registers and hold the offset calibration coefficient for the ADC
/// and its power-on reset value is 0x800000. Each of these registers is a read/write register.
/// These registers are used in conjunction with the associated gain register to form a register pair.
/// The power-on reset value is automatically overwritten if an internal or system zero-scale calibration is initiated by the user.
/// The ADC must be placed in standby mode or idle mode when writing to the offset registers.
/// </summary>
public readonly struct OffsetRegister
{
    /// <summary> Power-On/Reset = 0x800000. </summary>
    public static readonly OffsetRegister Default = new(0x800000);
    
    /// <summary> 24bit value. </summary>
    public readonly UInt32 Register;
    
    /// <summary> new OffsetRegister() { Register = register } </summary>
    public OffsetRegister(UInt32 register) => Register = register & 0x00FFFFFFu;

    /// <summary> Returns an array of 3 bytes. </summary>
    public byte[] GetBytes() => BinaryUtils.GetBytes24(Register);
}
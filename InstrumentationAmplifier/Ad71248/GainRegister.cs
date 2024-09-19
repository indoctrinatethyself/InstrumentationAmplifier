using System;
using InstrumentationAmplifier.Utils;

namespace InstrumentationAmplifier.Ad71248;

/// <summary>
/// The AD7124-8 has eight gain registers, GAIN_0 to GAIN_7.
/// Each gain register is associated with a setup; GAIN_x is associated with Setup x.
/// The gain registers are 24-bit registers and hold the full-scale calibration coefficient for the ADC.
/// The AD7124-8 is factory calibrated to a gain of 1.
/// The gain register contains this factory generated value on power-on and after a reset.
/// The gain registers are read/write registers. However, when writing to the registers,
/// the ADC must be placed in standby mode or idle mode.
/// The default value is automatically overwritten if an internal or system full-scale calibration
/// is initiated by the user or the full-scale registers are written to.
/// Power-On/Reset = 0x5XXXXX
/// </summary>
public readonly struct GainRegister
{
    /// <summary> An undefined value that will not be written to the ADC. </summary>
    public static readonly GainRegister Undefined = new(unchecked((Int32)0x80000000u));
    
    /// <summary> 24bit value. </summary>
    public readonly UInt32 Register = 0x80000000u;
    
    /// <summary> Creates an instance with mask 0x00FFFFFF. </summary>
    public GainRegister(UInt32 register) => Register = register & 0x00FFFFFFu;
    
    /// <summary> Creates an instance with without using a mask. </summary>
    private GainRegister(Int32 register) => Register = (UInt32)register;
    
    /// <summary> Register == Undefined </summary>
    public bool IsUndefined => Register == Undefined.Register;

    /// <summary> Returns an array of 3 bytes. </summary>
    public byte[] GetBytes() => BinaryUtils.GetBytes24(Register);
}
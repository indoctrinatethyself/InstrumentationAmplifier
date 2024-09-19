using System;
using System.ComponentModel;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

/// <summary>
/// The AD7124-8 has eight filter registers, FILTER_0 to FILTER_7.
/// Each filter register is associated with a setup; FILTER_x is associated with Setup x.
/// In the filter register, the filter type and output word rate are set.
/// </summary>
public struct FilterRegister
{
    /// <summary> Power-On/Reset = 0x060180. </summary>
    public static readonly FilterRegister Default = new() { Fs = 0x180, PostFilter = PostFilterTypes.Db62PostFilter };

    private UInt32 _register;
    
    /// <summary> new FilterRegister() { Register = register } </summary>
    public FilterRegister(UInt32 register) => Register = register;

    /// <summary> Filter type select bits. These bits select the filter type </summary>
    public FilterTypes Filter
    {
        get => (FilterTypes)GetBits(_register, 21, 3);
        set => SetBits(ref _register, 21, 3, (UInt32)value);
    }

    /// <summary>
    /// When this bit is set, a first order notch is placed at 60 Hz when the first notch of the sinc filter is at 50 Hz.
    /// This allows simultaneous 50 Hz and 60 Hz rejection.
    /// </summary>
    public bool Rej60 { get => GetBit(_register, 20); set => SetBit(ref _register, 20, value); }

    /// <summary>
    /// Post filter type select bits.
    /// When the filter bits are set to 1, the sinc3 filter is followed by a post filter
    /// that offers good 50 Hz and 60 Hz rejection at output data rates that have zero latency approximately.
    /// </summary>
    public PostFilterTypes PostFilter
    {
        get => (PostFilterTypes)GetBits(_register, 17, 3);
        set => SetBits(ref _register, 17, 3, (UInt32)value);
    }

    /// <summary>
    /// Single cycle conversion enable bit.
    /// When this bit is set, the AD7124-8 settles in one conversion cycle so that it functions as a zero latency ADC.
    /// This bit has no effect when multiple analog input channels are enabled or when the single conversion mode is selected.
    /// When the fast filters are used, this bit has no effect.
    /// </summary>
    public bool SingleCycle { get => GetBit(_register, 16); set => SetBit(ref _register, 16, value); }

    /// <summary>
    /// Filter output data rate select bits.
    /// These bits set the output data rate of the sinc3 filter, sinc4 filter, and fast settling filters.
    /// In addition, they affect the position of the first notch of the sinc filter and the cutoff frequency.
    /// In association with the gain selection, they also determine the output noise and,
    /// therefore, the effective resolution of the device (see noise tables).
    /// FS can have a value from 1 to 2047.
    /// </summary>
    public UInt16 Fs
    {
        get => (UInt16)GetBits(_register, 0, 10);
        set => SetBits(ref _register, 0, 10, value);
    }

    /// <summary> 24bit value. <br/> Mask 111_1_111_1_00000_11111111111. </summary>
    public UInt32 Register
    {
        get => _register;
        set => _register = value & 0b_111_1_111_1_00000_11111111111u;
    }

    /// <summary> Returns an array of 3 bytes. </summary>
    public byte[] GetBytes() => GetBytes24(_register);
}

/// <summary> Filter type select bits. These bits select the filter type. </summary>
public enum FilterTypes : byte
{
    /// <summary> Sinc4 filter. </summary>
    [Description("Sinc4 filter")] Sinc4Filter = 0b000,

    /// <summary> Sinc3 filter. </summary>
    [Description("Sinc3 filter")] Sinc3Filter = 0b010,

    /// <summary>
    /// Fast settling filter using the sinc4 filter.
    /// The sinc4 filter is followed by an averaging block, which results in a settling time equal to the conversion time.
    /// In full power and mid power modes, averaging by 16 occurs whereas averaging by 8 occurs in low power mode.
    /// </summary>
    [Description("Sinc4 fast filter")] Sinc4FastFilter = 0b100,

    /// <summary>
    /// Fast settling filter using the sinc3 filter.
    /// The sinc3 filter is followed by an averaging block, which results in a settling time equal to the conversion time.
    /// In full power and mid power modes, averaging by 16 occurs whereas averaging by 8 occurs in low power mode.
    /// </summary>
    [Description("Sinc3 fast filter")] Sinc3FastFilter = 0b101,

    /// <summary>
    /// Post filter enabled.
    /// The AD7124-8 includes several post filters, selectable using the POST_FILTER bits.
    /// The post filters have single cycle settling, the settling time being considerably better than a simple sinc3 /sinc4 filter.
    /// These filters offer excellent 50 Hz and 60 Hz rejection.
    /// </summary>
    [Description("Post filter")] PostFilter = 0b111,
}

/// <summary>
/// Post filter type select bits.
/// When the filter bits are set to 1, the sinc3 filter is followed by a post filter
/// that offers good 50 Hz and 60 Hz rejection at output data rates that have zero latency approximately.
/// </summary>
public enum PostFilterTypes : byte
{
    /// <summary> No Post Filter. </summary>
    [Description("No Post Filter")] NoPostFilter = 0b000,

    /// <summary> Rejection at 50 Hz and 60 Hz ± 1 Hz: 47 dB, Output Data Rate (SPS): 27.27 Hz. </summary>
    [Description("47 dB | SPS: 27.27 Hz")] Db47PostFilter = 0b010,

    /// <summary> Rejection at 50 Hz and 60 Hz ± 1 Hz: 62 dB, Output Data Rate (SPS): 25 Hz. </summary>
    [Description("62 dB | SPS: 25 Hz")] Db62PostFilter = 0b011,

    /// <summary> Rejection at 50 Hz and 60 Hz ± 1 Hz: 86 dB, Output Data Rate (SPS): 20 Hz. </summary>
    [Description("86 dB | SPS: 20 Hz")] Db86PostFilter = 0b101,

    /// <summary> Rejection at 50 Hz and 60 Hz ± 1 Hz: 92 dB, Output Data Rate (SPS): 16.7 Hz. </summary>
    [Description("92 dB | SPS: 16.7 Hz")] Db92PostFilter = 0b110,
}
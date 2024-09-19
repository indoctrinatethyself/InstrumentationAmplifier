using System;
using System.ComponentModel;
using InstrumentationAmplifier.Utils;
using static InstrumentationAmplifier.Utils.BinaryUtils;

namespace InstrumentationAmplifier.Ad71248;

public struct ChannelRegister
{
    private UInt16 _register;

    public bool Enable { get => GetBit(_register, 15); set => SetBit(ref _register, 15, value); }

    /// <summary> 0-7 </summary>
    public byte Setup
    {
        get => (byte)GetBits(_register, 12, 3); 
        set => SetBits(ref _register, 12, 3, value);
    }

    public Ain AinP
    {
        get => (Ain)GetBits(_register, 5, 5);
        set => SetBits(ref _register, 5, 5, (byte)value);
    }

    public Ain AinM
    {
        get => (Ain)GetBits(_register, 0, 5);
        set => SetBits(ref _register, 0, 5, (byte)value);
    }

    public UInt16 Register => _register;
    public byte[] GetBytes() => BinaryUtils.GetBytes(_register);
    
    
    public static readonly ChannelRegister Default0 = new() { Enable = true, Setup = 0, AinP = Ain.Ain0, AinM = Ain.Ain1 };
    public static readonly ChannelRegister Default = new() { Enable = false, Setup = 0, AinP = Ain.Ain0, AinM = Ain.Ain1 };
}

public enum Ain : byte
{
    [Description("AIN0")] Ain0 = 0b00000,
    [Description("AIN1")] Ain1 = 0b00001,
    [Description("AIN2")] Ain2 = 0b00010,
    [Description("AIN3")] Ain3 = 0b00011,
    [Description("AIN4")] Ain4 = 0b00100,
    [Description("AIN5")] Ain5 = 0b00101,
    [Description("AIN6")] Ain6 = 0b00110,
    [Description("AIN7")] Ain7 = 0b00111,
    [Description("AIN8")] Ain8 = 0b01000,
    [Description("AIN9")] Ain9 = 0b01001,
    [Description("AIN10")] Ain10 = 0b01010,
    [Description("AIN11")] Ain11 = 0b01011,
    [Description("AIN12")] Ain12 = 0b01100,
    [Description("AIN13")] Ain13 = 0b01101,
    [Description("AIN14")] Ain14 = 0b01110,
    [Description("AIN15")] Ain15 = 0b01111,
    [Description("Temperature sensor")] TemperatureSensor = 0b10000,
    [Description("AVss")] Avss = 0b10001,
    [Description("Internal reference")] InternalReference = 0b10010,
    [Description("DGND")] Dgnd = 0b10011,
    [Description("(AVdd − AVss)/6+")] Avdd6PInput = 0b10100,
    [Description("(AVdd − AVss)/6−")] Avdd6MInput = 0b10101,
    [Description("(IOVdd − DGND)/6+")] Iovdd6PInput = 0b10110,
    [Description("(IOVdd − DGND)/6−")] Iovdd6MInput = 0b10111,
    [Description("(ALDO − AVss)/6+")] Aldo6PInput = 0b11000,
    [Description("(ALDO − AVss)/6−")] Aldo6MInput = 0b11001,
    [Description("(DLDO − DGND)/6+")] Dldo6PInput = 0b11010,
    [Description("(DLDO − DGND)/6−")] Dldo6MInput = 0b11011,
    [Description("V_20MV_P")] V20MVpInput = 0b11100,
    [Description("V_20MV_M")] V20MvmInput = 0b11101,
}
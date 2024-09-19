namespace InstrumentationAmplifier.Ad71248;

public static class Ad71248Constants
{
    public const byte CommRegRead = 1 << 6;

    public const byte RegStatus = 0x00;
    public const byte RegAdcControl = 0x01;
    public const byte RegData = 0x02;
    public const byte RegIoControl1 = 0x03; // TODO: 
    public const byte RegIoControl2 = 0x04; // TODO: 
    public const byte RegId = 0x05;
    public const byte RegError = 0x06;
    public const byte RegErrorEn = 0x07;
    public const byte RegMclkCount = 0x08;
    public const byte RegChannel0 = 0x09;
    public const byte RegConfig0 = 0x19;
    public const byte RegFilter0 = 0x21;
    public const byte RegOffset0 = 0x29;
    public const byte RegGain0 = 0x31;

    /// <param name="number">0 - 15</param>
    public static byte RegChannel(byte number) => (byte)(RegChannel0 + number);

    /// <param name="number">0 - 7</param>
    public static byte RegConfig(byte number) => (byte)(RegConfig0 + number);

    /// <param name="number">0 - 7</param>
    public static byte RegFilter(byte number) => (byte)(RegFilter0 + number);

    /// <param name="number">0 - 7</param>
    public static byte RegOffset(byte number) => (byte)(RegOffset0 + number);

    /// <param name="number">0 - 7</param>
    public static byte RegGain(byte number) => (byte)(RegGain0 + number);

    public static readonly byte[] ResetCommand = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
}
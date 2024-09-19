using System;
using System.Device.Gpio;
using InstrumentationAmplifier.Devices;

namespace InstrumentationAmplifier.Services;

public interface IParallelDevice : IDisposable
{
    PinValue Read(int pinNumber);
    void Write(int pinNumber, PinValue value);
    void ReadAll(Span<PinValue> buffer);
    void WriteAll(ReadOnlySpan<PinValue> buffer);
    void WriteAll(PinValue value);
}

public interface IParallelDeviceWrapper : IParallelDevice
{
    ParallelDeviceOptions Options { get; }
    GpioPin[] Pins { get; }
    GpioController GetControllerAndPin(Int32 pinNumber, out int controllerPinNumber);

    delegate GpioController GetControllerAndPinDelegate(Int32 pinNumber, out int controllerPinNumber);
}

public interface IParallelDeviceFactory
{
    IParallelDeviceWrapper CreateInstance(ParallelDeviceOptions options, PinMode defaultPinMode);

    IParallelDeviceWrapper CreateInstance(ParallelDeviceOptions options,
        Func<IParallelDeviceWrapper.GetControllerAndPinDelegate, GpioPin[]> initialize);
}

public class ParallelDeviceFactory : IParallelDeviceFactory
{
    public IParallelDeviceWrapper CreateInstance(ParallelDeviceOptions options, PinMode defaultPinMode)
    {
        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            return new DummyParallelDevice(options);

        return new DefaultParallelDevice(options, defaultPinMode);
    }

    public IParallelDeviceWrapper CreateInstance(ParallelDeviceOptions options,
        Func<IParallelDeviceWrapper.GetControllerAndPinDelegate, GpioPin[]> initialize)
    {
        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            return new DummyParallelDevice(options);

        return new DefaultParallelDevice(options, initialize);
    }
}
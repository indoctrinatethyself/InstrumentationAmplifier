using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Linq;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.Devices;

public class DefaultParallelDevice : IParallelDeviceWrapper
{
    public DefaultParallelDevice(ParallelDeviceOptions options, PinMode defaultPinMode)
        : this(options, func =>
        {
            GpioPin[] pins = new GpioPin[options.Pins.Count];
            for (int i = 0; i < pins.Length; i++)
            {
                pins[i] = func((int)options.Pins[i].Pin.Gpio, out int pin)
                    .OpenPin(pin, defaultPinMode);
            }

            return pins;
        }) { }

    public DefaultParallelDevice(ParallelDeviceOptions options,
        Func<IParallelDeviceWrapper.GetControllerAndPinDelegate, GpioPin[]> initialize)
    {
        Controller = new(PinNumberingScheme.Logical, new LibGpiodDriver());
        Options = options;
        try
        {
            Pins = initialize(GetControllerAndPin);
        }
        catch (Exception)
        {
            Controller.Dispose();
            throw;
        }
    }

    public ParallelDeviceOptions Options { get; }
    public GpioPin[] Pins { get; }

    public GpioController Controller { get; }

    public bool AutomaticallyChangePinMode { get; set; } = false;
    public PinMode DefaultInputPinMode { get; set; } = PinMode.Input;

    public void Dispose()
    {
        Controller.Dispose();
    }

    public GpioController GetControllerAndPin(Int32 pinNumber, out int controllerPinNumber)
    {
        controllerPinNumber = pinNumber;
        return Controller;
    }

    public PinValue Read(Int32 pinNumber)
    {
        var pin = Pins.First(p => p.PinNumber == pinNumber);
        if (AutomaticallyChangePinMode && pin.GetPinMode() == PinMode.Output)
            pin.SetPinMode(DefaultInputPinMode);
        return pin.Read();
    }

    public void Write(Int32 pinNumber, PinValue value)
    {
        var pin = Pins.First(p => p.PinNumber == pinNumber);
        if (AutomaticallyChangePinMode && pin.GetPinMode() != PinMode.Output)
            pin.SetPinMode(PinMode.Output);
        pin.Write(value);
    }

    public void ReadAll(Span<PinValue> buffer)
    {
        if (buffer.Length != Pins.Length)
            throw new ArgumentException("Buffer length must be equal to pins count.", nameof(buffer));

        for (int i = 0; i < Pins.Length; i++)
        {
            var pin = Pins[i];
            if (AutomaticallyChangePinMode && pin.GetPinMode() == PinMode.Output)
                pin.SetPinMode(DefaultInputPinMode);
            buffer[i] = pin.Read();
        }
    }

    public void WriteAll(ReadOnlySpan<PinValue> buffer)
    {
        if (buffer.Length != Pins.Length)
            throw new ArgumentException("Buffer length must be equal to pins count.", nameof(buffer));

        for (int i = 0; i < Pins.Length; i++)
        {
            var pin = Pins[i];
            if (AutomaticallyChangePinMode && pin.GetPinMode() != PinMode.Output)
                pin.SetPinMode(PinMode.Output);
            pin.Write(buffer[i]);
        }
    }
    
    public void WriteAll(PinValue value)
    {
        for (int i = 0; i < Pins.Length; i++)
        {
            var pin = Pins[i];
            if (AutomaticallyChangePinMode && pin.GetPinMode() != PinMode.Output)
                pin.SetPinMode(PinMode.Output);
            pin.Write(value);
        }
    }
}

public class DummyParallelDevice : IParallelDeviceWrapper
{
    public DummyParallelDevice(ParallelDeviceOptions options)
    {
        Options = options;
        Pins = Enumerable.Repeat<GpioPin>(null!, options.Pins.Count).ToArray();
    }

    public ParallelDeviceOptions Options { get; }
    public GpioPin[] Pins { get; }

    public GpioController GetControllerAndPin(Int32 pinNumber, out int controllerPinNumber)
    {
        controllerPinNumber = pinNumber;
        return null!;
    }

    public PinValue Read(Int32 pinNumber) => 0;
    public void Write(Int32 pinNumber, PinValue value) { }
    public void ReadAll(Span<PinValue> buffer) { }
    public void WriteAll(ReadOnlySpan<PinValue> buffer) { }
    public void WriteAll(PinValue value) { }
    public void Dispose() { }
}
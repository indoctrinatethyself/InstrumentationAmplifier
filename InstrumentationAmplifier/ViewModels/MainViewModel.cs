using System;
using System.Collections.Immutable;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Pwm;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InstrumentationAmplifier.Ad71248;
using InstrumentationAmplifier.Common;
using InstrumentationAmplifier.Controls;
using InstrumentationAmplifier.Devices;
using InstrumentationAmplifier.Services;
using InstrumentationAmplifier.Utils;
using InstrumentationAmplifier.ViewModels.Units;
using MQTTnet.Client;
using MQTTnet.Formatter;
using static InstrumentationAmplifier.Utils.CommonUtils;

namespace InstrumentationAmplifier.ViewModels;

public partial class MainViewModel : DisposableViewModelBase
{
    private static readonly uint FanPinNumber = RaspberryPiAvailableDevices.Gpio16.Pin.Gpio;
    private static readonly uint PowerPinNumber = RaspberryPiAvailableDevices.Gpio5.Pin.Gpio;
    private static readonly byte PowerSensorChannel = 0;
    private static readonly byte ThermalSensorChannel = 1;
    private static readonly byte MaxAttenuation = 0b11111;

    /// <summary> Power sensor </summary>
    private static readonly ChannelRegister AdcChannel0 = new() { Enable = true, AinP = Ain.Ain0, AinM = Ain.Avss, Setup = 0 };

    /// <summary> Thermal sensor </summary>
    private static readonly ChannelRegister AdcChannel1 = new() { Enable = true, AinP = Ain.Ain1, AinM = Ain.Avss, Setup = 1 };

    private static readonly ConfigurationRegister AdcConfiguration0 = new()
        { Bipolar = false, RefSel = RefSel.RefIn1, AinBufm = false, AinBufp = true };

    private static readonly ConfigurationRegister AdcConfiguration1 = new()
        { Bipolar = false, RefSel = RefSel.RefIn1, AinBufm = false, AinBufp = true };

    private static readonly FilterRegister AdcFilter0 = new()
        { Filter = FilterTypes.Sinc4Filter, Fs = 30, PostFilter = PostFilterTypes.Db62PostFilter };

    private static readonly FilterRegister AdcFilter1 = new()
        { Filter = FilterTypes.Sinc4Filter, Fs = 30, PostFilter = PostFilterTypes.Db62PostFilter };

    private static readonly ImmutableArray<ParallelPin> AttenuatorPins =
    [
        RaspberryPiAvailableDevices.Gpio24,
        RaspberryPiAvailableDevices.Gpio23,
        RaspberryPiAvailableDevices.Gpio22,
        RaspberryPiAvailableDevices.Gpio27,
        RaspberryPiAvailableDevices.Gpio17,
    ];

    private static readonly ImmutableArray<decimal> AttenuatorControlPinValues = [ 14.4m, 7.2m, 3.6m, 1.8m, 0.9m ]; // 27.9

    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2);

    private readonly IParallelDeviceFactory _parallelDeviceFactory;
    private readonly AdcToPowerConverter _adcToPowerConverter;
    private readonly AttenuatorGainPerFrequency _attenuatorGainPerFrequency;
    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly IExceptionsLogger _exceptionsLogger;
    private readonly MqttCommandListener _mqttCommandListener;
    private readonly GpioController _controller = null!;

    private FrequencyValue _frequency = new(10, FrequencyUnit.Ghz);
    private PowerValue _outputPower = new(-10, PowerUnit.Dbm);
    private GainValue _gain = new(20, GainUnit.Db);
    private bool _outputPowerOrGain = true;
    private bool _modulation = true;
    private TimeValue _pulseDuration = new(1000, TimeUnit.Us);
    private decimal _dutyCycle = 10;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Enabled), nameof(RemoteLock))]
    private DeviceStatus _status = DeviceStatus.Initializing;

    private string _txt = "";

    private CancellationTokenSource _workCancellationTokenSource = null!;

    private Ad71248Device _ad71248 = null!;
    private GpioPin _powerPin = null!;
    private GpioPin _fanPin = null!;
    private PwmChannel _pwm = null!;
    private IParallelDeviceWrapper _attenuatorDevice = null!;
    private byte _currentAttenuatorPinsState;

    public MainViewModel(IParallelDeviceFactory parallelDeviceFactory,
        AdcToPowerConverter adcToPowerConverter,
        AttenuatorGainPerFrequency attenuatorGainPerFrequency,
        IDialogService dialogService, IToastService toastService,
        IExceptionsLogger exceptionsLogger,
        MqttCommandListener mqttCommandListener)
    {
        _parallelDeviceFactory = parallelDeviceFactory;
        _adcToPowerConverter = adcToPowerConverter;
        _attenuatorGainPerFrequency = attenuatorGainPerFrequency;
        _dialogService = dialogService;
        _toastService = toastService;
        _exceptionsLogger = exceptionsLogger;
        _mqttCommandListener = mqttCommandListener;

        _commandHandler = new(this);

        if (!OperatingSystem.IsWindows())
        {
            _controller = new(PinNumberingScheme.Logical, new LibGpiodDriver());
            _ = Initialize();
        }

        void MessengerRegister<TMessage>(MessageHandler<MainViewModel, TMessage> handler) where TMessage : class =>
            WeakReferenceMessenger.Default.Register<MainViewModel, TMessage>(this, handler);
    }

    public string Txt
    {
        get => _txt;
        set
        {
            if (_txt == value) return;
            _txt = value;
            OnPropertyChanged();
        }
    }

    public void Log(string text)
    {
        string Truncate(string s, int length) => s.Length > length ? s[..length] : s;
        Txt = Truncate(text + "\n" + Txt, 2000);
        Console.WriteLine(text);
    }

    public FrequencyValue Frequency
    {
        get => _frequency;
        set
        {
            if (_frequency == value) return;
            var valueInGhz = Range(value.InGhz, 6, 18);
            _frequency = new FrequencyValue(valueInGhz, value.Unit, FrequencyUnit.Ghz);
            OnPropertyChanged();

            if (value != _frequency)
                _ = _toastService.ShowMessage("Частота может быть >= 6 ГГц и <= 18 ГГц", ToastDuration);
        }
    }

    public PowerValue OutputPower
    {
        get => _outputPower;
        set
        {
            if (_outputPower == value) return;
            try
            {
                decimal valueInWatt = 0;
                if (!((value.Unit == PowerUnit.Watt || value.Unit == PowerUnit.MWatt) && value.Value < 0))
                    valueInWatt = Range(value.InWatt, 0m, 6.5m);
                _outputPower = new PowerValue(valueInWatt, value.Unit, PowerUnit.Watt);

                if (value != _outputPower)
                    _ = _toastService.ShowMessage("Мощность может быть >= 0 Вт и <= 6.5 Вт", ToastDuration);
            }
            catch (OverflowException) { }

            _outputPower = _outputPower.With(Math.Round(_outputPower.Value, 12, MidpointRounding.ToEven));
            OnPropertyChanged();
        }
    }

    public GainValue Gain
    {
        get => _gain;
        set
        {
            if (_gain == value) return;
            /*// TODO: for test
            _gain = value.With(Range(value.Value, 0, MaxAttenuation));*/
            _gain = value.With(Range(value.Value, 12, 45));
            OnPropertyChanged();

            if (value != _gain)
                _ = _toastService.ShowMessage("Усиление может быть >= 12 дБ и <= 45 дБ", ToastDuration);
            else if (OutputPowerOrGain == false && Status == DeviceStatus.On) // gain
            {
                var pins = CalcAttenuatorControlPins(Gain.InDb);
                byte pinsValue = pins.pins;
                SetAttenuatorPins(pinsValue);
                Log($"Pins: {Convert.ToString(pinsValue, 2)} ({pinsValue}) | Gain: {pins.gain}");
            }
        }
    }

    /// <summary> True - output power, false - gain </summary>
    public bool OutputPowerOrGain
    {
        get => _outputPowerOrGain;
        set
        {
            if (_outputPowerOrGain == value) return;
            _outputPowerOrGain = value;

            if (Status == DeviceStatus.On)
            {
                if (OutputPowerOrGain == true) // output power
                {
                    SetAttenuatorPins(MaxAttenuation);
                }
                else // gain
                {
                    byte pinsValue = CalcAttenuatorControlPins(Gain.InDb).pins;
                    SetAttenuatorPins(pinsValue);
                }
            }

            OnPropertyChanged();
        }
    }

    public bool Modulation
    {
        get => _modulation;
        set
        {
            if (_modulation == value) return;
            _modulation = value;

            if (Status == DeviceStatus.On)
            {
                _pwm.DutyCycle = Modulation == false ? 1 : (double)(1 / DutyCycle);
            }

            OnPropertyChanged();
        }
    }

    public TimeValue PulseDuration
    {
        get => _pulseDuration;
        set
        {
            if (_pulseDuration == value) return;

            bool error = false;
            try
            {
                var valueInUs = Range(value.InUs, 1m, 1e6m);
                if (valueInUs / DutyCycle < 1) valueInUs = DutyCycle;
                _pulseDuration = new TimeValue(valueInUs, value.Unit, TimeUnit.Us);

                if (Status == DeviceStatus.On)
                {
                    _pwm.Frequency = (Int32)(1 / _pulseDuration.InSeconds);
                }

                if (value != _pulseDuration) error = true;
            }
            catch (OverflowException) { error = true; }

            OnPropertyChanged();

            if (error)
                _ = _toastService.ShowMessage(
                    "Тимп может быть >= 1 нс и <= 1 с.\nТимп(нс) / Скважность должен быть >= 1.", ToastDuration);
        }
    }

    public decimal DutyCycle
    {
        get => _dutyCycle;
        set
        {
            if (_dutyCycle == value) return;
            _dutyCycle = Math.Max(1, value);
            var pulseDurationInUs = PulseDuration.InUs;
            if (pulseDurationInUs / _dutyCycle < 1)
                _dutyCycle = pulseDurationInUs;

            if (Status == DeviceStatus.On)
            {
                _pwm.DutyCycle = Modulation == false ? 1 : (double)(1 / DutyCycle);
            }

            OnPropertyChanged();

            if (value != _dutyCycle)
                _ = _toastService.ShowMessage(
                    "Тимп(нс) / Скважность должен быть >= 1.", ToastDuration);
        }
    }

    public bool Enabled => Status == DeviceStatus.On;

    private decimal CurrentAttenuatorGain => _attenuatorGainPerFrequency.FindClosest(Frequency.InGhz)
                                             - _currentAttenuatorPinsState * AttenuatorControlPinValues[^1];

    private bool CanToggle => Status is DeviceStatus.Off or DeviceStatus.On; // IsWorkStopping == false && IsInitialized == true;

    public static bool IsTempOk(double temp) => temp is <= 60 and >= -55;
    public static bool IsTempNotOk(double temp) => temp is > 60 or < -55;

    private async Task Initialize()
    {
        _powerPin = _controller.OpenPin((int)PowerPinNumber, PinMode.Output, PinValue.High);
        _fanPin = _controller.OpenPin((int)FanPinNumber, PinMode.Output, PinValue.Low);
        _pwm = PwmChannel.Create(0, 0);
        Encoder encoder = new(_controller, 19, 13, 6);
        _attenuatorDevice = _parallelDeviceFactory.CreateInstance(new(AttenuatorPins), PinMode.Output);
        _ad71248 = new(new DefaultSpiDevice(
            new(RaspberryPiAvailableDevices.SpiBus0, RaspberryPiAvailableDevices.SpiBus0Le0)));

        encoder.Step += (_, args) => WeakReferenceMessenger.Default.Send<EncoderEventMessage>(new(args));
        encoder.KeyStateChanged += (_, args) => WeakReferenceMessenger.Default.Send<EncoderEventMessage>(new(args));
        encoder.Click += (_, args) => WeakReferenceMessenger.Default.Send<EncoderEventMessage>(new(args));

        _fanPin.Write(PinValue.High);
        _powerPin.Write(PinValue.Low);
        _pwm.Stop();
        SetAttenuatorPins(MaxAttenuation);

        await EnsureAd71248Initialized();

        _powerPin.Write(PinValue.High);

        Thread.Sleep(20);

        double temp = ReadTempSensor();
        if (IsTempNotOk(temp))
        {
            _ = _dialogService.ShowMessage($"Температура за пределами допустымих значений ({temp:0.###}).");
        }

        _powerPin.Write(PinValue.Low);

        System.Reactive.Disposables.Disposable.Create(this, vm =>
        {
            vm.SetAttenuatorPins(MaxAttenuation);
            vm._fanPin.Write(PinValue.Low);
            vm._powerPin.Write(PinValue.Low);
        }).DisposeWith(Disposable);
        encoder.DisposeWith(Disposable);
        _pwm.DisposeWith(Disposable);
        _controller.DisposeWith(Disposable);

        Status = DeviceStatus.Off;
    }

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private void Toggle()
    {
        if (Status == DeviceStatus.Off) _ = Start();
        else if (Status == DeviceStatus.On) Stop();
    }

    private void Stop()
    {
        Status = DeviceStatus.Stopping;
        _workCancellationTokenSource.Cancel();
    }

    private async Task Start()
    {
        // TODO: try finally
        // TODO: Validate?

        Status = DeviceStatus.Starting;
        _workCancellationTokenSource = new();

        _powerPin.Write(PinValue.High);

        Thread.Sleep(100);

        await EnsureAd71248Initialized();

        double temp = ReadTempSensor();

        if (IsTempNotOk(temp))
        {
            await _dialogService.ShowMessage($"Температура за пределами допустымих значений ({temp:0.###}). Запуск предотвращён.");
            _powerPin.Write(PinValue.Low);
            Status = DeviceStatus.Off;
            return;
        }

        if (OutputPowerOrGain == true) // output power
        {
            SetAttenuatorPins(MaxAttenuation);
        }
        else // gain
        {
            var pins = CalcAttenuatorControlPins(Gain.InDb);
            byte pinsValue = pins.pins;
            SetAttenuatorPins(pinsValue);
            Log($"Pins: {Convert.ToString(pinsValue, 2)} ({pinsValue}) | Gain: {pins.gain}");

            /*byte pinsValue = 0b11111;
            SetAttenuatorPins(pinsValue);
            Log($"Pins: {Convert.ToString(pinsValue, 2)} ({pinsValue})");

            _pwm.DutyCycle = Modulation == false ? 1 : (double)(1 / DutyCycle);
            _pwm.Frequency = (Int32)(1 / PulseDuration.InSeconds);
            _pwm.Start();

            for (; pinsValue > 0; pinsValue--)
            {
                await Task.Delay(1000);
                SetAttenuatorPins(pinsValue);
                Log($"Pins: {Convert.ToString(pinsValue, 2)} ({pinsValue})");
            }

            var pins = CalcAttenuatorControlPins(Gain);
            pinsValue = pins.pins;
            SetAttenuatorPins(pinsValue);
            Log($"Pins: {Convert.ToString(pinsValue, 2)} ({pinsValue}) | Gain: {pins.gain}");
            await Task.Delay(2000);

            _pwm.Stop();
            _powerPin.Write(PinValue.Low);
            return;*/
        }

        _pwm.DutyCycle = Modulation == false ? 1 : (double)(1 / DutyCycle);
        _pwm.Frequency = (Int32)(1 / PulseDuration.InSeconds);
        _pwm.Start();


        Task continuousConversionTask = Task.Factory.StartNew(DoWork);

        _ = continuousConversionTask.ContinueWith(
            HandleWorkFunctionError,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.NotOnCanceled,
            TaskScheduler.FromCurrentSynchronizationContext());

        _ = continuousConversionTask.ContinueWith(
            FinishWorkFunction,
            TaskScheduler.FromCurrentSynchronizationContext());

        Status = DeviceStatus.On;
    }

    private void DoWork()
    {
        var ct = _workCancellationTokenSource.Token;

        for (;;)
        {
            ct.ThrowIfCancellationRequested();

            Double actualPowerInDbm = 0, temp = 0;
            try
            {
                (actualPowerInDbm, temp) = ReadSensors(ct);
            }
            catch (ZeroPowerOnSensorException e)
            {
                // TODO: 
                /*_ = _dialogService.ShowMessage($"Ошибка чтения датчиков. Работа приостановлена.\n{e}");
                Stop();
                continue;*/
            }

            Log($"power: {actualPowerInDbm:0.000000}dBm, temp: {temp:0.000000}*C");

            // TODO: for test
            if (IsTempNotOk(temp))
            {
                _ = _dialogService.ShowMessage($"Температура за пределами допустымих значений ({temp:0.###}). Работа приостановлена.");
                Stop();
                continue;
            }

            if (OutputPowerOrGain == true)
            {
                WorkOutputPowerPart((Decimal)actualPowerInDbm);
            }
        }
    }

    private void WorkOutputPowerPart(decimal actualPowerInDbm)
    {
        if (actualPowerInDbm > OutputPower.InDbm)
        {
            _ = _dialogService.ShowMessage($"Выходная мощность ({actualPowerInDbm:0.####}dBm) превысила установленное значение"
                                           + $" ({OutputPower.InDbm:0.####}dBm). Работа приостановлена.");
            Stop();
            return;
        }

        decimal deltaPower = OutputPower.InDbm - actualPowerInDbm;

        if (deltaPower >= 0.9m)
        {
            var reduceAttenuation = Math.Max(1, (int)(deltaPower / 0.9m / 2m));

            var newPinsState = _currentAttenuatorPinsState - reduceAttenuation;
            if (newPinsState < 0) newPinsState = 0;

            Log($"/\\ = {deltaPower} | -{reduceAttenuation}p, gain = {CurrentAttenuatorGain}");

            SetAttenuatorPins((byte)newPinsState);

            Gain = Gain.With(CurrentAttenuatorGain);
        }
    }

    private void HandleWorkFunctionError(Task t)
    {
        var e = t.Exception?.InnerException!;
        if (e is OperationCanceledException) return;

        _exceptionsLogger.Log(e);
        _ = _dialogService.ShowMessage($"Ошибка\n{e.Message}");
    }

    private void FinishWorkFunction(Task _)
    {
        _pwm.Stop();
        SetAttenuatorPins(MaxAttenuation);
        _powerPin.Write(PinValue.Low);

        Status = DeviceStatus.Off;
    }

    private void SetAttenuatorPins(byte pinsValue)
    {
        Span<PinValue> pins = stackalloc PinValue[5];
        for (int i = 0; i < 5; i++)
            pins[5 - 1 - i] = BinaryUtils.GetBit(pinsValue, i);
        //pins[i] = BinaryUtils.GetBit(pinsValue, i);
        _attenuatorDevice.WriteAll(pins);
        _currentAttenuatorPinsState = pinsValue;
    }

    private (double powerInDbm, double temp) ReadSensors(CancellationToken ct = default)
    {
        // TODO: test it
        //var values = ReadAdc(6, ct);
        var values = ReadAdcWithGain(4, ct);

        var tempSensorVoltage = values[ThermalSensorChannel].voltage;
        var temp = GetTempFromVoltage(tempSensorVoltage);

        var powerSensor = values[PowerSensorChannel];

        Log($"raw power: {powerSensor.voltage:0.000000} ({powerSensor.data}), raw temp: {tempSensorVoltage:0.000000}");

        var powerFunc = _adcToPowerConverter.FindClosest(Frequency.InGhz);
        var powerInWatt = Math.Max(0, powerFunc(powerSensor.data));
        if (powerInWatt <= 0) throw new ZeroPowerOnSensorException();
        var powerInDbm = PowerUnit.WattsToDbm((decimal)powerInWatt);

        return ((double)powerInDbm, temp);
    }

    private static readonly int[] ListOfThermalSensorChannel = [ ThermalSensorChannel ];

    private double ReadTempSensor(CancellationToken ct = default)
    {
        // TODO: test it
        //var values = ReadAdc(6, ListOfThermalSensorChannel, ct);
        var values = ReadAdcWithGain(4, ListOfThermalSensorChannel, ct);

        var tempSensorVoltage = values[ThermalSensorChannel].voltage;
        var temp = GetTempFromVoltage(tempSensorVoltage);

        Log($"raw temp: {tempSensorVoltage:0.000000}");

        return temp;
    }

    private (byte pins, decimal gain) CalcAttenuatorControlPins(decimal gain)
    {
        decimal @base = _attenuatorGainPerFrequency.FindClosest(Frequency.InGhz);
        decimal step = AttenuatorControlPinValues[^1]; // 0.9

        /*// TODO: for test
        byte p = (byte)Range(gain, 0, MaxAttenuation);
        return (p, @base - p * step);*/

        // TODO: add restriction to gain property
        decimal attenuation = Math.Max(0, @base - gain);
        byte pinsValue = (byte)(attenuation / step);

        return (pinsValue, @base - pinsValue * step);
    }

    private static double GetTempFromVoltage(double voltage) =>
        -1481.96 + Math.Sqrt(2.1962e6 + (1.8639 - voltage) / 3.88E-6);

    [RelayCommand]
    private void OpenConfiguration()
    {
        WeakReferenceMessenger.Default.Send<MainViewChangeMessage>(new(MainViewChangeMessage.Views.Configuration));
    }
}

public enum DeviceStatus
{
    Initializing,
    Off,
    Starting,
    On,
    Stopping,
    Lock
}

public class ZeroPowerOnSensorException(string message = "Датчик мощности вернул недопустимо низкое значение") : Exception(message);

/*
let a = [5,4,5,6,7,6,1,4];
let sa = a.toSorted((a, b) => a - b);
let half = Math.floor(sa.length / 2);
let median = sa.length % 2 ? sa[half] : (sa[half - 1] + sa[half]) / 2;
let deviation = sa.map(m => Math.abs(median - m));
const average = arr => arr.reduce((p, c) => p + c, 0) / arr.length;
let averageDeviation = average(deviation);
console.log(sa);
console.log(median);
console.log(deviation);
console.log(averageDeviation);
*/
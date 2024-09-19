using System;
using System.Device.Gpio;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using InstrumentationAmplifier.Services;
using InstrumentationAmplifier.Services.CommandHandler;
using InstrumentationAmplifier.ViewModels.Units;

// ReSharper disable UnusedMember.Local

namespace InstrumentationAmplifier.ViewModels;

public partial class MainViewModel : ICommandHandler
{
    private readonly CommandHandler _commandHandler;

    public bool RemoteLock => Status == DeviceStatus.Lock;

    public CommandResponse? Handle(string message) => _commandHandler.Handle(message);

    private bool EnterLockState([NotNullWhen(false)] out string? error)
    {
        Status = DeviceStatus.Lock;
        SetAttenuatorPins(MaxAttenuation);
        _powerPin.Write(PinValue.High);

        Thread.Sleep(50);

        if (!InitializeAd71248(out error))
        {
            _powerPin.Write(PinValue.Low);
            Status = DeviceStatus.Off;
            return false;
        }

        return true;
    }

    private void ExitLockState()
    {
        SetAttenuatorPins(MaxAttenuation);
        _pwm.Stop();
        _powerPin.Write(PinValue.Low);

        Status = DeviceStatus.Off;
    }

    private void EnablePwm(double dutyCycle, double pulseDurationInSeconds)
    {
        _pwm.DutyCycle = 1 / dutyCycle;
        _pwm.Frequency = (Int32)(1 / pulseDurationInSeconds);
        _pwm.Start();
    }

    private void DisablePwm()
    {
        _pwm.Stop();
    }

    record SensorsValue(uint PowerInAdc, byte PowerAdcGain, double PowerInVoltage, uint TempInAdc, double TempInVoltage, double TempInC);

    private SensorsValue ReadSensorsRaw(CancellationToken ct = default)
    {
        var values = ReadAdcWithGain(5, ct);

        var tempSensor = values[ThermalSensorChannel];
        var powerSensor = values[PowerSensorChannel];
        var temp = GetTempFromVoltage(tempSensor.voltage);

        return new(powerSensor.data, powerSensor.gain, powerSensor.voltage,
            tempSensor.data, tempSensor.voltage, temp);
    }


    public class CommandHandler : CommandHandlerBase<CommandResponse>
    {
        private readonly MainViewModel _vm;

        public CommandHandler(MainViewModel vm) => _vm = vm;

        public CommandResponse? Handle(string message)
        {
            try
            {
                if (TryHandle(message, out var response)) return response;
                return CommandResult.UnknownCommand();
            }
            catch (Exception e)
            {
                return CommandResult.ExecutionError(e.GetType() + ": " + e.Message);
            }
        }

        [Command("test")]
        private CommandResponse TestCommand() => CommandResult.Ok($"test");

        [Command("echo")]
        private CommandResponse EchoCommand(string s) => CommandResult.Ok($"> {s}");

        [Command("full_echo", TrimStart = false)]
        private CommandResponse FullEchoCommand(string s) => CommandResult.Ok($"> {s}");


        [Command(MqttCommandListener.OnDisconnectedCommand)]
        private CommandResponse? OnDisconnected()
        {
            if (_vm.Status == DeviceStatus.Lock)
            {
                _vm.ExitLockState();
            }

            return null;
        }

        [Command("status")]
        private CommandResponse StatusCommand()
        {
            return CommandResult.Ok(_vm.Status.ToString("G").ToLowerInvariant()); // TODO: to snake_case
        }


        #region Extended

        [Command("lock")]
        private CommandResponse LockCommand()
        {
            if (_vm.Status != DeviceStatus.Off)
                return CommandResult.InvalidArguments($"Lock is only available in the off state. Current status:"
                                                      + $" {_vm.Status.ToString("G").ToLowerInvariant()}.");

            if (!_vm.EnterLockState(out var error))
            {
                return CommandResult.ExecutionError(error);
            }

            return CommandResult.Ok();
        }

        [Command("unlock")]
        private CommandResponse RemoteCommand()
        {
            if (_vm.Status != DeviceStatus.Lock)
                return CommandResult.InvalidArguments($"Unlock is only available in the lock state. Current status:"
                                                      + $" {_vm.Status.ToString("G").ToLowerInvariant()}.");

            _vm.ExitLockState();
            return CommandResult.Ok();
        }

        private CommandResponse? OnlyInLockState()
        {
            if (_vm.Status == DeviceStatus.Lock) return null;
            return CommandResult.InvalidArguments($"This command is only available in lock mode. Current status:"
                                                  + $" {_vm.Status.ToString("G").ToLowerInvariant()}.");
        }

        [Command("set_attenuator ")]
        private CommandResponse SetAttenuatorCommand(string s)
        {
            if (OnlyInLockState() is { } result) return result;
            if (!byte.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid pins state (byte).");

            _vm.SetAttenuatorPins(value);
            return CommandResult.Ok();
        }

        [Command("read_sensors")]
        private CommandResponse ReadSensorsCommand()
        {
            if (OnlyInLockState() is { } result) return result;

            var value = _vm.ReadSensorsRaw();
            return CommandResult.Ok(data: value);
        }

        /// <summary> enable_pwm dutyCycle{double, >0, скважность} pulseDuration{double, >0, seconds} </summary>
        [Command("enable_pwm ")]
        private CommandResponse EnablePwmCommand(string s)
        {
            if (OnlyInLockState() is { } result) return result;

            var args = s.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (args is not [ var dutyCycleRaw, var pulseDurationRaw ]
                || !double.TryParse(dutyCycleRaw, out var dutyCycle) || dutyCycle <= 0
                || !double.TryParse(pulseDurationRaw, out var pulseDuration) || pulseDuration <= 0
               )
                return CommandResult.InvalidArguments(
                    "enable_pwm dutyCycle{decimal, >0, скважность} pulseDuration{decimal, >0, seconds}");

            _vm.EnablePwm(dutyCycle, pulseDuration);

            return CommandResult.Ok();
        }

        [Command("disable_pwm")]
        private CommandResponse DisablePwmCommand()
        {
            if (OnlyInLockState() is { } result) return result;
            _vm.DisablePwm();

            return CommandResult.Ok();
        }

        #endregion


        #region UI

        [Command("start")]
        private CommandResponse StartCommand()
        {
            if (_vm.Status != DeviceStatus.Off)
                return CommandResult.InvalidArguments($"Start is only available in the off state. Current status:"
                                                      + $" {_vm.Status.ToString("G").ToLowerInvariant()}.");
            _ = _vm.Start();
            return CommandResult.Ok();
        }

        [Command("stop")]
        private CommandResponse StopCommand()
        {
            if (_vm.Status != DeviceStatus.On)
                return CommandResult.InvalidArguments($"Stop is only available in the on state. Current status:"
                                                      + $" {_vm.Status.ToString("G").ToLowerInvariant()}.");
            _vm.Stop();
            return CommandResult.Ok();
        }

        [Command("set_frequency ")]
        private CommandResponse SetFrequencyCommand(string s)
        {
            if (!decimal.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid gain (Mhz).");

            _vm.Frequency = new(value, FrequencyUnit.Mhz);
            return CommandResult.Ok();
        }

        [Command("set_output_power ")]
        private CommandResponse SetOutputPowerCommand(string s)
        {
            if (!decimal.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid output power (dBm).");

            _vm.OutputPower = new(value, PowerUnit.Dbm);
            return CommandResult.Ok();
        }

        [Command("set_gain ")]
        private CommandResponse SetGainCommand(string s)
        {
            if (!decimal.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid gain (dB).");

            _vm.Gain = new(value, GainUnit.Db);
            return CommandResult.Ok();
        }

        [Command("set_mode ")]
        private CommandResponse SetModeCommand(string s)
        {
            if (s == "output_power") _vm.OutputPowerOrGain = true;
            else if (s == "gain") _vm.OutputPowerOrGain = false;
            else return CommandResult.InvalidArguments("Invalid mode (output_power | gain).");

            return CommandResult.Ok();
        }

        [Command("set_modulation ")]
        private CommandResponse SetModulationCommand(string s)
        {
            if (!bool.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid modulation (true/false).");

            _vm.Modulation = value;
            return CommandResult.Ok();
        }

        [Command("set_pulse_duration ")]
        private CommandResponse SetPulseDurationCommand(string s)
        {
            if (!decimal.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid pulse duration (us).");

            _vm.PulseDuration = new(value, TimeUnit.Us);
            return CommandResult.Ok();
        }

        [Command("set_duty_cycle ")]
        private CommandResponse SetDutyCycleCommand(string s)
        {
            if (!decimal.TryParse(s, out var value))
                return CommandResult.InvalidArguments("Invalid duty cycle.");

            _vm.DutyCycle = value;
            return CommandResult.Ok();
        }

        #endregion
    }
}
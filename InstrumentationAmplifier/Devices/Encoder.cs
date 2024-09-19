using System;
using System.Collections;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace InstrumentationAmplifier.Devices;

public class Encoder : IDisposable
{
    private readonly GpioController _controller;
    private readonly Int32 _s1PinNumber;
    private readonly Int32 _s2PinNumber;
    private readonly Int32 _keyPinNumber;

    private readonly SlidingBuffer<PinValueChangedEventArgs> _lastPinEvents = new(4);

    private bool _lastPin1State = false;
    private bool _lastPin2State = false;
    private bool _isButtonPressed = false;

    /*private Subject<(Object sender, PinValueChangedEventArgs args)> _pin1Observable = new();
    private Subject<(Object sender, PinValueChangedEventArgs args)> _pin2Observable = new();*/
    private readonly Subject<(Object sender, PinValueChangedEventArgs args)> _keyPinObservable = new();

    public event EventHandler<PinValueChangedEventArgs>? PinChanged;
    public event EventHandler<EncoderStepEventArgs>? Step;
    public event EventHandler<EncoderKeyStateChangeEventArgs>? KeyStateChanged;
    public event EventHandler<EncoderClickEventArgs>? Click;

    public Encoder(GpioController controller, int s1PinNumber, int s2PinNumber, int keyPinNumber = 0)
    {
        _controller = controller;
        _s1PinNumber = s1PinNumber;
        _s2PinNumber = s2PinNumber;
        _keyPinNumber = keyPinNumber;

        var s1Pin = controller.OpenPin(s1PinNumber);
        var s2Pin = controller.OpenPin(s2PinNumber);
        var keyPin = controller.OpenPin(keyPinNumber);

        /*_pin1Observable.Throttle(TimeSpan.FromMilliseconds(1)).Subscribe(a => OnSPinChanged(a.sender, a.args));
        _pin2Observable.Throttle(TimeSpan.FromMilliseconds(1)).Subscribe(a => OnSPinChanged(a.sender, a.args));

        _controller.RegisterCallbackForPinValueChangedEvent(s1PinNumber, PinEventTypes.Rising | PinEventTypes.Falling,
            (sender, args) => _pin1Observable.OnNext((sender, args)));
        _controller.RegisterCallbackForPinValueChangedEvent(s2PinNumber, PinEventTypes.Rising | PinEventTypes.Falling,
            (sender, args) => _pin1Observable.OnNext((sender, args)));*/
        _keyPinObservable.Throttle(TimeSpan.FromMilliseconds(5)).Subscribe(a => OnKeyPinChanged(a.sender, a.args));

        _controller.RegisterCallbackForPinValueChangedEvent(s1PinNumber, PinEventTypes.Rising | PinEventTypes.Falling, OnSPinChanged);
        _controller.RegisterCallbackForPinValueChangedEvent(s2PinNumber, PinEventTypes.Rising | PinEventTypes.Falling, OnSPinChanged);
        _controller.RegisterCallbackForPinValueChangedEvent(keyPinNumber, PinEventTypes.Rising | PinEventTypes.Falling,
            OnKeyPinEventRaised);

        _lastPin1State = s1Pin.Read() == PinValue.High;
        _lastPin2State = s2Pin.Read() == PinValue.High;
        _isButtonPressed = keyPin.Read() == PinValue.Low;
    }

    private void OnKeyPinEventRaised(Object sender, PinValueChangedEventArgs args) => _keyPinObservable.OnNext((sender, args));

    private void OnSPinChanged(Object sender, PinValueChangedEventArgs args)
    {
        lock (this)
        {
            bool newPinState = args.ChangeType == PinEventTypes.Rising;
            if (args.PinNumber == _s1PinNumber)
                if (newPinState == _lastPin1State) return;
                else _lastPin1State = newPinState;

            if (args.PinNumber == _s2PinNumber)
                if (newPinState == _lastPin2State) return;
                else _lastPin2State = newPinState;

            _lastPinEvents.Add(args);
            PinChanged?.Invoke(sender, args);

            if (_lastPinEvents.Count != 4) return;

            using var en = _lastPinEvents.GetEnumerator();
            en.MoveNext();
            var e1 = en.Current;
            en.MoveNext();
            var e2 = en.Current;
            en.MoveNext();
            var e3 = en.Current;
            en.MoveNext();
            var e4 = en.Current;

            if (e1.ChangeType != PinEventTypes.Falling ||
                e2.ChangeType != PinEventTypes.Falling ||
                e3.ChangeType != PinEventTypes.Rising ||
                e4.ChangeType != PinEventTypes.Rising ||
                e1.PinNumber != e3.PinNumber ||
                e2.PinNumber != e4.PinNumber) return;

            bool isLeft = e1.PinNumber == _s1PinNumber;

            /*if (_lastPinEvents.Count != 2) return;

            using var en = _lastPinEvents.GetEnumerator();
            en.MoveNext();
            var e1 = en.Current;
            en.MoveNext();
            var e2 = en.Current;

            if (e1.ChangeType != PinEventTypes.Rising ||
                e2.ChangeType != PinEventTypes.Rising ||
                e1.PinNumber == e2.PinNumber) return;

            bool isLeft = e1.PinNumber == _s1PinNumber;*/

            _rotateDuringClick = true;
            Step?.Invoke(this, new(isLeft ? StepDirection.Left : StepDirection.Right, _isButtonPressed));
        }
    }

    private bool _rotateDuringClick = false;

    private void OnKeyPinChanged(Object sender, PinValueChangedEventArgs args)
    {
        lock (this)
        {
            PinChanged?.Invoke(sender, args);
            _isButtonPressed = args.ChangeType == PinEventTypes.Falling;
            if (_isButtonPressed) _rotateDuringClick = false;
            KeyStateChanged?.Invoke(this, new(_isButtonPressed));

            if (args.ChangeType == PinEventTypes.Rising)
            {
                Click?.Invoke(this, new(_rotateDuringClick));
            }
        }
    }

    public void Dispose()
    {
        /*_pin1Observable.Dispose();
        _pin2Observable.Dispose();*/
        _keyPinObservable.Dispose();

        _controller.UnregisterCallbackForPinValueChangedEvent(_s1PinNumber, OnSPinChanged);
        _controller.UnregisterCallbackForPinValueChangedEvent(_s2PinNumber, OnSPinChanged);
        _controller.UnregisterCallbackForPinValueChangedEvent(_keyPinNumber, OnKeyPinEventRaised);

        _controller.ClosePin(_s1PinNumber);
        _controller.ClosePin(_s2PinNumber);
        _controller.ClosePin(_keyPinNumber);
    }
}

public enum StepDirection { Left, Right }

public class EncoderEventArgs : EventArgs;

public class EncoderStepEventArgs(StepDirection direction, bool isButtonPressed = false) : EncoderEventArgs
{
    public StepDirection Direction { get; } = direction;
    public bool IsButtonPressed { get; } = isButtonPressed;
}

public class EncoderKeyStateChangeEventArgs(bool isButtonPressed) : EncoderEventArgs
{
    public bool IsButtonPressed { get; } = isButtonPressed;
}

public class EncoderClickEventArgs(bool rotatingDuringClick) : EncoderEventArgs
{
    public bool RotatingDuringClick { get; } = rotatingDuringClick;
}

class SlidingBuffer<T> : IEnumerable<T>
{
    private readonly Queue<T> _queue;
    private readonly int _maxCount;

    public SlidingBuffer(int maxCount)
    {
        _maxCount = maxCount;
        _queue = new Queue<T>(maxCount);
    }

    public void Add(T item)
    {
        if (_queue.Count == _maxCount)
            _queue.Dequeue();
        _queue.Enqueue(item);
    }

    public int Count => _queue.Count;

    public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
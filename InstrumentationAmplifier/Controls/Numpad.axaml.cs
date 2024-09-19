using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using InstrumentationAmplifier.Devices;
using InstrumentationAmplifier.Utils;

namespace InstrumentationAmplifier.Controls;

public partial class Numpad : UserControl
{
    #region Attached properties

    public static readonly AttachedProperty<bool> CommaAllowedProperty = AvaloniaProperty.RegisterAttached<Numpad, TextBox, bool>(
        "CommaAllowed", true, false, BindingMode.OneTime);

    public static readonly AttachedProperty<bool> NegativeAllowedProperty = AvaloniaProperty.RegisterAttached<Numpad, TextBox, bool>(
        "NegativeAllowed", true, false, BindingMode.OneTime);

    // TODO: allow double value
    public static readonly AttachedProperty<IUnitValue> ValueProperty =
        AvaloniaProperty.RegisterAttached<Numpad, TextBox, IUnitValue>(
            "Value", default!, false, BindingMode.TwoWay);


    public static void SetCommaAllowed(AvaloniaObject element, bool value) => element.SetValue(CommaAllowedProperty, value);

    public static bool GetCommaAllowed(AvaloniaObject? element) => element?.GetValue(CommaAllowedProperty) ?? false;

    public static void SetNegativeAllowed(AvaloniaObject element, bool value) => element.SetValue(NegativeAllowedProperty, value);

    public static bool GetNegativeAllowed(AvaloniaObject? element) => element?.GetValue(NegativeAllowedProperty) ?? false;

    public static void SetValue(AvaloniaObject element, IUnitValue value) => element.SetValue(ValueProperty, value);

    public static IUnitValue GetValue(AvaloniaObject element) => element.GetValue(ValueProperty);

    #endregion
    
    private const bool ConvertValueWithUnits = false;

    public string NumpadSelectedClass { get; init; } = "numpad-selected";

    public static readonly DirectProperty<Numpad, Control> ParentControlProperty =
        AvaloniaProperty.RegisterDirect<Numpad, Control>(
            nameof(ParentControl),
            o => o.ParentControl,
            (o, v) => o.ParentControl = v);

    private const string DecimalFormat = "0.##############################";

    private Control? _parentControl;

    private TextBox? _focusedTb;
    private bool _isCommaAllowed;
    private bool _isNegativeAllowed;
    private IUnitValue? _value;
    private IReadOnlyList<UnitDefinition> _units = [ ];
    private int _selectedUnit;

    public Numpad()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<Numpad, EncoderEventMessage>(this, (recipient, message) =>
        {
            Dispatcher.UIThread.Invoke(() => recipient.HandleEncoderMessage(message));
        });
    }

    public string Text
    {
        get => TempValueTb.Text ??= "";
        set => TempValueTb.Text = value;
    }

    public required Control ParentControl
    {
        get => _parentControl!;
        set
        {
            var old = _parentControl;
            if (_parentControl != null) _parentControl.GotFocus -= OnElementGotFocus;
            _parentControl = value;
            if (_parentControl != null) _parentControl.GotFocus += OnElementGotFocus;
            RaisePropertyChanged(ParentControlProperty, old!, _parentControl!);
        }
    }

    private TextBox? FocusedTb
    {
        get => _focusedTb;
        set
        {
            _focusedTb?.Classes.Remove(NumpadSelectedClass);
            _focusedTb = value;
            EditPanel.IsEnabled = _focusedTb != null;
            IsCommaAllowed = GetCommaAllowed(_focusedTb);
            IsNegativeAllowed = GetNegativeAllowed(_focusedTb);
            SetTextFromFocusedTb();
            _focusedTb?.Classes.Add(NumpadSelectedClass);
        }
    }

    private bool IsCommaAllowed
    {
        get => _isCommaAllowed;
        set
        {
            _isCommaAllowed = value;
            CommaBtn.IsEnabled = value;
        }
    }

    private bool IsNegativeAllowed
    {
        get => _isNegativeAllowed;
        set
        {
            _isNegativeAllowed = value;
            MinusBtn.IsEnabled = value;
        }
    }

    private IUnitValue? Value
    {
        get => _value;
        set
        {
            _value = value;
            Units = value?.Units ?? ImmutableArray<UnitDefinition>.Empty;
            SelectedUnit = Units.Count == 0
                ? -1
                : value!.Units.IndexOf(value.Unit) is var v && v == -1 ? 0 : v;
        }
    }

    private IReadOnlyList<UnitDefinition> Units
    {
        get => _units;
        set
        {
            _units = value ?? ImmutableArray<UnitDefinition>.Empty;

            Unit1Btn.IsEnabled = _units.Count > 0;
            Unit1Btn.Content = _units.Count > 0 ? _units[0].Name : "";

            Unit2Btn.IsEnabled = _units.Count > 1;
            Unit2Btn.Content = _units.Count > 1 ? _units[1].Name : "";

            Unit3Btn.IsEnabled = _units.Count > 2;
            Unit3Btn.Content = _units.Count > 2 ? _units[2].Name : "";
        }
    }

    private readonly IBrush _selectedUnitBrush = new SolidColorBrush(Color.Parse("#5000"));
    private readonly IBrush _unselectedUnitBrush = new SolidColorBrush(Color.Parse("#3000"));

    private int SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            _selectedUnit = value;
            Unit1Btn.Background = value == 0 ? _selectedUnitBrush : _unselectedUnitBrush;
            Unit2Btn.Background = value == 1 ? _selectedUnitBrush : _unselectedUnitBrush;
            Unit3Btn.Background = value == 2 ? _selectedUnitBrush : _unselectedUnitBrush;
        }
    }

    private UnitDefinition? GetUnit(int index) => index >= 0 && index <= Units?.Count - 1 ? Units?[index] : null;

    private void OnElementGotFocus(object? sender, GotFocusEventArgs e)
    {
        var element = (StyledElement)e.Source!;
        var newFocusedTb = element.GetParent<TextBox>();
        // if (tb == FocusedTb) return;
        bool editPanelPart = element.IsChildOf(EditPanel);

        if (editPanelPart) return;

        if (newFocusedTb == null && FocusedTb != null)
        {
            FocusedTb = null;
        }
        else if (newFocusedTb != null)
        {
            if (FocusedTb != null && FocusedTb != newFocusedTb) Apply();

            FocusedTb = newFocusedTb;
        }
    }

    private void SetTextFromFocusedTb()
    {
        if (FocusedTb == null)
        {
            Text = "";
        }
        else
        {
            Value = GetValue(FocusedTb);
            if (Value != null)
            {
                Text = Value.Value.ToString(DecimalFormat);
            }
            else
            {
                Text = FocusedTb.Text ?? "";
            }

            TempValueTb.CaretIndex = Text.Length;
        }
    }

    private void EscBtnClick(object? sender, RoutedEventArgs e) => SetTextFromFocusedTb();

    private void ApplyBtnClick(object? sender, RoutedEventArgs e) => Apply();

    private void Apply()
    {
        string s = Text.Length > 0 ? Text : "0";
        if (decimal.TryParse(s, out var val))
        {
            if (!IsCommaAllowed) val = Math.Floor(val);
            if (!IsNegativeAllowed) val = Math.Max(0, val);

            if (Value != null)
            {
                var v = GetValue(FocusedTb!);
                SetValue(FocusedTb!, Value.With(val, GetUnit(SelectedUnit)!));
            }
            else
            {
                FocusedTb!.Text = val.ToString(DecimalFormat, CultureInfo.CurrentCulture);
            }
        }
    }

    private void BackspaceBtnClick(object? sender, RoutedEventArgs e)
    {
        string val = Text;
        int ss = TempValueTb.SelectionStart;
        int se = TempValueTb.SelectionEnd;
        if (se < ss) (ss, se) = (se, ss);

        if (ss != se)
        {
            Text = val.Remove(ss, se - ss);
            TempValueTb.CaretIndex = ss;
        }
        else if (ss != 0)
        {
            Text = val.Remove(ss - 1, 1);
            TempValueTb.CaretIndex = ss - 1;
        }
    }

    private void InputBtnClick(object? sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender!;
        string val = Text;
        int ss = TempValueTb.SelectionStart;
        int se = TempValueTb.SelectionEnd;
        if (se < ss) (ss, se) = (se, ss);

        switch (btn.Tag)
        {
            case int digit:
            {
                if (ss != se)
                {
                    val = val.Remove(ss, se - ss);
                }

                val = val.Insert(ss, digit.ToString());


                Text = val;
                TempValueTb.CaretIndex = ss + 1;
                break;
            }

            case "-":
            {
                int caretOffset = 1;

                if (val == "") val = "-";
                else if (val[0] == '-')
                {
                    val = val.Remove(0, 1);
                    caretOffset = ss == 0 ? 0 : -1;
                }
                else
                {
                    val = "-" + val;
                }

                if (ss != se)
                {
                    Text = val;
                    TempValueTb.SelectionStart += caretOffset;
                    TempValueTb.SelectionEnd += caretOffset;
                }
                else
                {
                    Text = val;
                    TempValueTb.CaretIndex = ss + caretOffset;
                }

                break;
            }

            case ".":
            {
                if (!val.Contains('.'))
                {
                    if (ss != se)
                    {
                        val = val.Remove(ss, se - ss);
                    }

                    val = val.Insert(ss, ".");

                    Text = val;
                    TempValueTb.CaretIndex = ss + 1;
                }

                break;
            }
        }
    }

    private void SelectUnitBtnClick(object? sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender!;
        int unit = (Int32)btn.Tag!;
        if (unit == SelectedUnit)
        {
            Apply();
            return;
        }

        if (/*ConvertValueWithUnits == false*/  ConvertToggleBtn.IsChecked != true)
        {
            SelectedUnit = unit;

            Apply();
        }
        else
        {
            var currentUnit = GetUnit(SelectedUnit)!;
            var selectedUnit = GetUnit(unit)!;


            if (decimal.TryParse(Text, out var val))
            {
                try
                {
                    val = selectedUnit.FromBase(currentUnit.ToBase(val));
                    Text = val.ToString(DecimalFormat, CultureInfo.CurrentCulture);
                    TempValueTb.CaretIndex = Text.Length;

                    SelectedUnit = unit;
                }
                catch (OverflowException) { }
            }
        }
    }

    public (int start, int end) GetSelection()
    {
        if (TempValueTb.SelectionEnd < TempValueTb.SelectionStart)
            (TempValueTb.SelectionStart, TempValueTb.SelectionEnd) = (TempValueTb.SelectionEnd, TempValueTb.SelectionStart);
        return (TempValueTb.SelectionStart, TempValueTb.SelectionEnd);
    }

    private void HandleEncoderMessage(EncoderEventMessage message)
    {
        if (FocusedTb == null) return;

        var (ss, se) = GetSelection();

        if (message.Value is EncoderStepEventArgs args)
        {
            if (args.IsButtonPressed)
            {
                int len = Text.Length;
                if (Text is "" or "-" or "." or "-.") return;

                int direction = args.Direction == StepDirection.Left ? -1 : 1;
                int selectedIndex = ss;
                for (;;)
                {
                    selectedIndex += direction;
                    if (selectedIndex == -1) selectedIndex = len - 1;
                    if (selectedIndex >= len) selectedIndex = 0;
                    if (Text[selectedIndex] is not ('-' or '.')) break;
                }

                TempValueTb.SelectionStart = selectedIndex;
                TempValueTb.SelectionEnd = selectedIndex + 1;
            }
            else
            {
                if (Text.Length == 0) Text = "0";

                if (ss == se)
                {
                    if (decimal.TryParse(Text, out var val))
                    {
                        val += args.Direction == StepDirection.Left ? -1 : 1;
                        if (!(IsNegativeAllowed == false && val < 0))
                        {
                            Text = val.ToString(DecimalFormat, CultureInfo.CurrentCulture);
                            TempValueTb.CaretIndex = Text.Length;
                        }
                    }
                }
                else
                {
                    if (ss == 0 && Text[0] == '-')
                    {
                        if (se == 1)
                        {
                            TempValueTb.CaretIndex = 1;
                            return;
                        }

                        TempValueTb.SelectionStart = ss = 1;
                    }

                    string selectedText = Text.Substring(ss, se - ss);
                    int digitsAfterComma = selectedText.IndexOf('.') is var i and >= 0 ? selectedText.Length - 1 - i : 0;
                    decimal step = (decimal)Math.Pow(10, -digitsAfterComma);
                    decimal maxSelectedTextValue = digitsAfterComma == 0
                        ? (decimal)Math.Pow(10, selectedText.Length) - 1
                        : (decimal)Math.Pow(10, selectedText.Length - 1) * step - step;

                    if (selectedText == ".")
                    {
                        TempValueTb.ClearSelection();
                        return;
                    }

                    if (decimal.TryParse(selectedText, out var val))
                    {
                        val += args.Direction == StepDirection.Left ? -step : step;
                        if (val > maxSelectedTextValue) val = 0;
                        if (val < 0) val = maxSelectedTextValue;

                        string format = new(selectedText.Select(c => Char.IsDigit(c) ? '0' : c).ToArray());
                        Text = Text[..ss] + val.ToString(format, CultureInfo.CurrentCulture) + Text[se..];
                    }
                }
            }
        }
        else if (message.Value is EncoderClickEventArgs { RotatingDuringClick: false })
        {
            if (se != ss)
                TempValueTb.SelectionStart = TempValueTb.SelectionEnd = se;
            else
                TempValueTb.SelectAll();
        }
    }
}

public class EncoderEventMessage(EncoderEventArgs value) : ValueChangedMessage<EncoderEventArgs>(value);
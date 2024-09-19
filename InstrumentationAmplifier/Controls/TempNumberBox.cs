using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using DynamicData.Binding;

namespace InstrumentationAmplifier.Controls;

public class TempNumberBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_presenter")]
    static extern ref TextPresenter? GetPresenter(TextBox @this);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdateCommandStates")]
    static extern void UpdateCommandStates(TextBox @this);

    private static readonly FieldInfo ImClientField;
    private static readonly MethodInfo SetPresenterMethod;
    private static readonly MethodInfo TemplatedControlOnLostFocus;

    static TempNumberBox()
    {
        var type = typeof(Avalonia.Application).Assembly.GetType("Avalonia.Controls.TextBoxTextInputMethodClient")!;
        SetPresenterMethod = type.GetMethod("SetPresenter")!;
        ImClientField = typeof(TextBox).GetField("_imClient", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TemplatedControlOnLostFocus = typeof(TemplatedControl)
            .GetMethod("OnLostFocus", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    public TempNumberBox()
    {
        this.WhenValueChanged(o => o.IsEffectivelyEnabled).Subscribe(v =>
        {
            if (v) GetPresenter(this)?.ShowCaret();
            else GetPresenter(this)?.HideCaret();
        });
    }

    /*protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        /*if (!IsFocused)
            GetPresenter(this)?.ShowCaret();#1#
    }*/

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        IntPtr ptr = TemplatedControlOnLostFocus.MethodHandle.GetFunctionPointer();
        var controlOnLostFocus = (Action<RoutedEventArgs>)Activator.CreateInstance(typeof(Action<RoutedEventArgs>), this, ptr)!;
        controlOnLostFocus(e);

        if ((ContextFlyout == null || !ContextFlyout.IsOpen) &&
            (ContextMenu == null || !ContextMenu.IsOpen))
        {
            // ClearSelection();
            SetCurrentValue(RevealPasswordProperty, false);
        }

        UpdateCommandStates(this);

        //GetPresenter(this)?.HideCaret();

        var t = ImClientField.GetValue(this);
        SetPresenterMethod.Invoke(t, new object?[] { null, null });

        //GetImClient(this).SetPresenter(null, null);
        //_imClient.SetPresenter(null, null);
    }
}
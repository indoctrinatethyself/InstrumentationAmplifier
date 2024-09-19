using System;
using InstrumentationAmplifier.Common;

namespace InstrumentationAmplifier.Dialogs;

public class MessageDialogViewModel(string text, String buttonText = "Закрыть") : DialogViewModelBase
{
    public string Text { get; } = text;
    public string ButtonText { get; } = buttonText;
}
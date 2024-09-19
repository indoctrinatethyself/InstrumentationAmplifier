using InstrumentationAmplifier.Common;

namespace InstrumentationAmplifier.Toasts;

public class SimpleToastViewModel(string text) : ToastViewModelBase
{
    public string Text { get; } = text;
}
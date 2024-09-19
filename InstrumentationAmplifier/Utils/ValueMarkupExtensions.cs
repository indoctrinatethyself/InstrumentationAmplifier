using System;
using Avalonia.Markup.Xaml;

namespace InstrumentationAmplifier.Utils;

public abstract class ValueMarkupExtension<T> : MarkupExtension
{
    public ValueMarkupExtension(T value) => Value = value;
    public T Value { get; set; }
    public override object ProvideValue(IServiceProvider sp) => Value!;
}

public sealed class Int32Extension : ValueMarkupExtension<int>
{
    public Int32Extension(int value) : base(value) { }
}

public sealed class BoolExtension : ValueMarkupExtension<bool>
{
    public BoolExtension(bool value) : base(value) { }
}
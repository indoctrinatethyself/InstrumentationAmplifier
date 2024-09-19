using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace InstrumentationAmplifier.Utils;

public class CompiledTernaryExtension : MarkupExtension
{
    public CompiledTernaryExtension() { }

    public required CompiledBindingExtension Binding { get; init; }

    public required object True { get; init; }

    public required object False { get; init; }

    public Type? Type { get; init; }

    public Func<object?, object>? ConvertFunc { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var cultureInfo = new CultureInfo("en-US");

        object? ConvertToType(object? value) => Convert.ChangeType(value, Type!, cultureInfo);

        Func<object?, object?> convertFunc;
        if (ConvertFunc != null)
        {
            convertFunc = ConvertFunc;
        }
        else if (Type != null)
        {
            if (Type.IsEnum)
            {
                convertFunc = o => Enum.Parse(Type, (string)o!);
            }
            else
            {
                convertFunc = TernaryExtensionConverters.Types.TryGetValue(Type, out var func)
                    ? func
                    : ConvertToType;
            }
        }
        else convertFunc = o => o;

        Binding.Converter = Binding.Converter is { } converter
            ? new FuncValueConverter<object?, object?>(e =>
                convertFunc(converter.Convert(e, typeof(bool), Binding.ConverterParameter, cultureInfo) is true ? True : False))
            : new FuncValueConverter<bool, object?>(e => convertFunc(e ? True : False));

        Binding.Mode = BindingMode.OneWay;

        return Binding.ProvideValue(serviceProvider);
    }
}

[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public static class TernaryExtensionConverters
{
    private static bool IsDouble(object? o, [NotNullWhen(true)] out double? val)
    {
        val = null;
        val = o switch
        {
            Double v => v,
            Single v => v,
            Byte v => v,
            Int16 v => v,
            Int32 v => v,
            Int64 v => v,
            Decimal v => (double)v,
            _ => null
        };
        return val != null;
    }

    public static object? ParseThickness(object? v) =>
        v is string s ? Avalonia.Thickness.Parse(s)
        : IsDouble(v, out double? d) ? new Avalonia.Thickness(d.Value) : null;
    
    public static readonly Func<object?, object> ParseBrush = o => Brush.Parse((string)o!);

    public static readonly ImmutableDictionary<Type, Func<object?, object?>> Types =
        new Dictionary<Type, Func<object?, object?>>
            {
                [typeof(Avalonia.Thickness)] = ParseThickness,
                [typeof(IBrush)] = ParseBrush
            }
            .ToImmutableDictionary();
}
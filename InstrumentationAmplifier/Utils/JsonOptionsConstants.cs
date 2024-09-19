using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DynamicData;

namespace InstrumentationAmplifier.Utils;

public static class JsonOptionsConstants
{
    public static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    //JavaScriptEncoder.Create(UnicodeRanges.All);

    public static readonly Action<JsonTypeInfo> AllowIgnoreRequiredModifier = info =>
    {
        if (info.Kind != JsonTypeInfoKind.Object) return;

        foreach (var prop in info.Properties)
        {
            if (!prop.IsRequired) continue;

            prop.IsRequired = prop.AttributeProvider?.IsDefined(
                typeof(JsonRequiredAttribute), inherit: false
            ) ?? false;
        }
    };

    public static readonly ImmutableArray<Action<JsonTypeInfo>> Modifiers =
        new List<Action<JsonTypeInfo>>
            {
                AllowIgnoreRequiredModifier
            }
            .ToImmutableArray();

    public static readonly JsonSerializerOptions Options;

    public static JsonSerializerOptions WithModifiers(params Action<JsonTypeInfo>[] modifiers)
    {
        JsonSerializerOptions options = new(Options);
        var defaultJsonTypeInfoResolver = new DefaultJsonTypeInfoResolver();
        defaultJsonTypeInfoResolver.Modifiers.AddRange(Modifiers);
        defaultJsonTypeInfoResolver.Modifiers.AddRange(modifiers);
        options.TypeInfoResolver = defaultJsonTypeInfoResolver;
        return options;
    }

    static JsonOptionsConstants()
    {
        var defaultJsonTypeInfoResolver = new DefaultJsonTypeInfoResolver();
        defaultJsonTypeInfoResolver.Modifiers.AddRange(Modifiers);

        Options = new()
        {
            Encoder = Encoder,
            TypeInfoResolver = defaultJsonTypeInfoResolver
        };
    }
}
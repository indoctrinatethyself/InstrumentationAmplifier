using System;
using System.Collections.Immutable;
using InstrumentationAmplifier.Controls;

namespace InstrumentationAmplifier.ViewModels.Units;

public class FrequencyUnit : UnitDefinition
{
    public static readonly FrequencyUnit Ghz = new("ГГц", 9);
    public static readonly FrequencyUnit Mhz = new("МГц", 6);
    
    public static readonly ImmutableArray<UnitDefinition> Units = [ Ghz, Mhz ];
    
    private FrequencyUnit(String name, Int32 degree) : base(name, degree) { }
}

public readonly struct FrequencyValue(Decimal value, FrequencyUnit unit) : IUnitValue, IEquatable<FrequencyValue>
{
    public Decimal Value { get; } = value;
    public FrequencyUnit Unit { get; } = unit;
    UnitDefinition IUnitValue.Unit => Unit;
    
    public FrequencyValue(Decimal value, FrequencyUnit unit, FrequencyUnit convertFrom) :
        this(unit != convertFrom ? unit.FromBase(convertFrom.ToBase(value)) : value, unit) { }

    public ImmutableArray<UnitDefinition> Units => FrequencyUnit.Units;

    public decimal InGhz => Convert(FrequencyUnit.Ghz); //FrequencyUnit.Ghz.FromBase(Unit.ToBase(Value));
    public decimal InMhz => Convert(FrequencyUnit.Mhz); //FrequencyUnit.Mhz.FromBase(Unit.ToBase(Value));
    
    public IUnitValue With(Decimal newValue, UnitDefinition newUnit) => new FrequencyValue(newValue, (FrequencyUnit)newUnit);
    
    public decimal Convert(FrequencyUnit to) => Convert(Value, Unit, to);

    public static decimal Convert(decimal value, FrequencyUnit from, FrequencyUnit to)
    {
        if (from == to) return value;
        return to.FromBase(from.ToBase(value));
    }
    
    public bool Equals(FrequencyValue other) => Value == other.Value && Unit.Equals(other.Unit);
    public override bool Equals(object? obj) => obj is FrequencyValue other && Equals(other);
    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public static bool operator ==(FrequencyValue left, FrequencyValue right) => left.Equals(right);
    public static bool operator !=(FrequencyValue left, FrequencyValue right) => !left.Equals(right);
}
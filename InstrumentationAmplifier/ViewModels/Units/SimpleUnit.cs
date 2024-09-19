using System;
using System.Collections.Immutable;
using InstrumentationAmplifier.Controls;

namespace InstrumentationAmplifier.ViewModels.Units;

public class SimpleUnit(String name) : UnitDefinition(name, d => d, d => d)
{
    public static implicit operator SimpleUnit(string name) => new(name);
};

public readonly struct SimpleValue(Decimal value, SimpleUnit unit) : IUnitValue, IEquatable<SimpleValue>
{
    public Decimal Value { get; } = value;
    public SimpleUnit Unit { get; } = unit;
    UnitDefinition IUnitValue.Unit => Unit;

    public ImmutableArray<UnitDefinition> Units { get; } = [ unit ];
    
    public IUnitValue With(Decimal newValue, UnitDefinition unit) => new SimpleValue(newValue, (SimpleUnit)unit);
    public SimpleValue With(Decimal newValue) => new(newValue, Unit);
    
    public static implicit operator decimal(SimpleValue value) => value.Value;
    
    public bool Equals(SimpleValue other) => Value == other.Value && Unit.Equals(other.Unit);
    public override bool Equals(object? obj) => obj is SimpleValue other && Equals(other);
    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public static bool operator ==(SimpleValue left, SimpleValue right) => left.Equals(right);
    public static bool operator !=(SimpleValue left, SimpleValue right) => !left.Equals(right);
}
using System;
using System.Collections.Immutable;
using InstrumentationAmplifier.Controls;

namespace InstrumentationAmplifier.ViewModels.Units;

public class GainUnit : UnitDefinition
{
    public static readonly GainUnit Db = new("дБ", d => d, d => d);

    public static readonly ImmutableArray<UnitDefinition> Units = [ Db ];

    private GainUnit(String name, Func<Decimal, Decimal> fromBase, Func<Decimal, Decimal> toBase)
        : base(name, fromBase, toBase) { }
}

public readonly struct GainValue(Decimal value, GainUnit unit) : IUnitValue, IEquatable<GainValue>
{
    public Decimal Value { get; } = value;
    public GainUnit Unit { get; } = unit;
    UnitDefinition IUnitValue.Unit => Unit;

    public GainValue(Decimal value, GainUnit unit, GainUnit convertFrom) :
        this(Convert(value, convertFrom, unit), unit) { }

    public ImmutableArray<UnitDefinition> Units => GainUnit.Units;

    public decimal InDb => Convert(GainUnit.Db);

    public IUnitValue With(Decimal newValue, UnitDefinition newUnit) => new GainValue(newValue, (GainUnit)newUnit);
    public GainValue With(Decimal newValue) => new(newValue, Unit);

    public decimal Convert(GainUnit to) => Convert(Value, Unit, to);

    public static decimal Convert(decimal value, GainUnit from, GainUnit to) => 
        from == to ? value : to.FromBase(from.ToBase(value));

    public bool Equals(GainValue other) => Math.Abs(Value - other.Value) <= 1e-11m && Unit == other.Unit;
    public override bool Equals(object? obj) => obj is GainValue other && Equals(other);
    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public static bool operator ==(GainValue left, GainValue right) => left.Equals(right);
    public static bool operator !=(GainValue left, GainValue right) => !left.Equals(right);
}
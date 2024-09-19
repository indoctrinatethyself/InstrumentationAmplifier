using System;
using System.Collections.Immutable;
using InstrumentationAmplifier.Controls;

namespace InstrumentationAmplifier.ViewModels.Units;

public class TimeUnit : UnitDefinition
{
    public static readonly TimeUnit Second = new("с", 0);
    public static readonly TimeUnit Ms = new("мс", -3);
    public static readonly TimeUnit Us = new("мкс", -6);
    
    public static readonly ImmutableArray<UnitDefinition> Units = [ Second, Ms, Us ];
    
    private TimeUnit(String name, Int32 degree) : base(name, degree) { }
}

public readonly struct TimeValue(Decimal value, TimeUnit unit) : IUnitValue, IEquatable<TimeValue>
{
    public Decimal Value { get; } = value;
    public TimeUnit Unit { get; } = unit;
    UnitDefinition IUnitValue.Unit => Unit;
    
    public TimeValue(Decimal value, TimeUnit unit, TimeUnit convertFrom) :
        this(unit != convertFrom ? unit.FromBase(convertFrom.ToBase(value)) : value, unit) { }

    public ImmutableArray<UnitDefinition> Units => TimeUnit.Units;

    public decimal InSeconds => TimeUnit.Second.FromBase(Unit.ToBase(Value));
    public decimal InMs => TimeUnit.Ms.FromBase(Unit.ToBase(Value));
    public decimal InUs => TimeUnit.Us.FromBase(Unit.ToBase(Value));
    
    public IUnitValue With(Decimal newValue, UnitDefinition newUnit) => new TimeValue(newValue, (TimeUnit)newUnit);
    
    public bool Equals(TimeValue other) => Value == other.Value && Unit.Equals(other.Unit);
    public override bool Equals(object? obj) => obj is TimeValue other && Equals(other);
    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public static bool operator ==(TimeValue left, TimeValue right) => left.Equals(right);
    public static bool operator !=(TimeValue left, TimeValue right) => !left.Equals(right);
}
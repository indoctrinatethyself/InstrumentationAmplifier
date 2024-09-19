using System;
using System.Collections.Immutable;
using System.Diagnostics;
using InstrumentationAmplifier.Controls;

namespace InstrumentationAmplifier.ViewModels.Units;

public class PowerUnit : UnitDefinition
{
    public static decimal DbmToWatts(decimal dBm) => (decimal)Math.Pow(10, (double)(dBm / 10)) / 1000;
    public static decimal DbmToMWatts(decimal dBm) => (decimal)Math.Pow(10, (double)(dBm / 10));
    public static decimal WattsToDbm(decimal watts) => (decimal)(10.0 * Math.Log10((double)watts * 1000));
    public static decimal MWattsToDbm(decimal mWatts) => (decimal)(10.0 * Math.Log10((double)mWatts));

    public static readonly PowerUnit Dbm = new("дБм", d => d, d => d);
    public static readonly PowerUnit Watt = new("Вт", DbmToWatts, WattsToDbm);
    public static readonly PowerUnit MWatt = new("мВт", DbmToMWatts, MWattsToDbm);

    public static readonly ImmutableArray<UnitDefinition> Units = [ Dbm, Watt, MWatt ];

    private PowerUnit(String name, Func<Decimal, Decimal> fromBase, Func<Decimal, Decimal> toBase)
        : base(name, fromBase, toBase) { }
}

public readonly struct PowerValue(Decimal value, PowerUnit unit) : IUnitValue, IEquatable<PowerValue>
{
    public Decimal Value { get; } = value;
    public PowerUnit Unit { get; } = unit;
    UnitDefinition IUnitValue.Unit => Unit;

    public PowerValue(Decimal value, PowerUnit unit, PowerUnit convertFrom) :
        this(Convert(value, convertFrom, unit), unit) { }

    public ImmutableArray<UnitDefinition> Units => PowerUnit.Units;

    public decimal InDbm => Convert(PowerUnit.Dbm);
    public decimal InWatt => Convert(PowerUnit.Watt);
    public decimal InMWatt => Convert(PowerUnit.MWatt);

    public IUnitValue With(Decimal newValue, UnitDefinition newUnit) => new PowerValue(newValue, (PowerUnit)newUnit);
    public PowerValue With(Decimal newValue) => new(newValue, Unit);

    public decimal Convert(PowerUnit to) => Convert(Value, Unit, to);

    public static decimal Convert(decimal value, PowerUnit from, PowerUnit to)
    {
        if (from == to) return value;
        if (from == PowerUnit.Dbm || to == PowerUnit.Dbm)
            return to.FromBase(from.ToBase(value));
            //return Math.Round(to.FromBase(from.ToBase(value)), 12, MidpointRounding.ToEven);
        if (from == PowerUnit.Watt && to == PowerUnit.MWatt) return value * 1000m;
        if (from == PowerUnit.MWatt && to == PowerUnit.Watt) return value / 1000m;
        throw new UnreachableException();
    }

    public bool Equals(PowerValue other) => Math.Abs(Value - other.Value) <= 1e-11m && Unit == other.Unit;
    public override bool Equals(object? obj) => obj is PowerValue other && Equals(other);
    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public static bool operator ==(PowerValue left, PowerValue right) => left.Equals(right);
    public static bool operator !=(PowerValue left, PowerValue right) => !left.Equals(right);
}
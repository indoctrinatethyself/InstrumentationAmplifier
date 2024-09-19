using System;
using System.Collections.Immutable;

namespace InstrumentationAmplifier.Controls;

public interface IUnitValue
{
    public decimal Value { get; }
    public UnitDefinition Unit { get; }
    public ImmutableArray<UnitDefinition> Units { get; }

    public IUnitValue With(Decimal value, UnitDefinition unit);
}
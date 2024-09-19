using System;

namespace InstrumentationAmplifier.Controls;

public class UnitDefinition
{
    public UnitDefinition(string name, int degree)
    {
        Name = name;
        FromBase = d => d * (decimal)Math.Pow(10, -degree);
        ToBase = d => d * (decimal)Math.Pow(10, degree);
    }


    public UnitDefinition(String name, Func<Decimal, Decimal> fromBase, Func<Decimal, Decimal> toBase)
    {
        Name = name;
        FromBase = fromBase;
        ToBase = toBase;
    }

    public string Name { get; }
    public Func<decimal, decimal> FromBase { get; }
    public Func<decimal, decimal> ToBase { get; }

    public override String ToString() => Name;
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Symbolics;

namespace InstrumentationAmplifier.Services;

public class AdcToPowerConverter
{
    public AdcToPowerConverter()
    {
        var fileStream = File.OpenRead("Data/adc_power.txt");
        var stream = new StreamReader(fileStream);

        while (stream.ReadLine() is { } line)
        {
            ReadOnlySpan<char> s = line.AsSpan().Trim();
            if (s.IsEmpty) continue;
            int spaceIndex = s.IndexOf(' ');
            if (spaceIndex is -1 or 0 || spaceIndex == s.Length - 1) continue;

            var ghzRaw = s[..spaceIndex].ToString().Replace(',', '.');
            if (!decimal.TryParse(ghzRaw, out var ghz)) continue;

            var rawExpression = s[(spaceIndex + 1)..];

            //var variable = SymbolicExpression.Variable("x");
            try
            {
                var expression = SymbolicExpression.Parse(rawExpression.ToString());
                var func = expression.Compile("x");
                FrequencyDependentConversionFunctions[ghz] = func;
            }
            catch (Exception e) { continue; }
            // TODO: show errors
        }
    }

    public SortedDictionary<decimal, Func<double, double>> FrequencyDependentConversionFunctions { get; } = new();

    public Func<double, double> FindClosest(decimal ghz) => 
        FrequencyDependentConversionFunctions.MinBy(e => Math.Abs(e.Key - ghz)).Value;
}
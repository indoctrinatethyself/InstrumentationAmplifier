using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InstrumentationAmplifier.Services;

public class AttenuatorGainPerFrequency
{
    public AttenuatorGainPerFrequency()
    {
        var fileStream = File.OpenRead("Data/Attenuator gain -12dbm.txt");
        var stream = new StreamReader(fileStream);

        while (stream.ReadLine() is { } line)
        {
            ReadOnlySpan<char> s = line.AsSpan().Trim();
            if (s.IsEmpty) continue;
            int spaceIndex = s.IndexOfAny(" \t");
            if (spaceIndex is -1 or 0 || spaceIndex == s.Length - 1) continue;

            var frequencyRaw = s[..spaceIndex].ToString().Replace(',', '.');
            if (!decimal.TryParse(frequencyRaw, out var frequency)) continue;
            
            var gainRaw = s[(spaceIndex + 1)..].ToString().Replace(',', '.');
            if (!decimal.TryParse(gainRaw, out var gain)) continue;

            Gains[frequency] = gain;
            // TODO: show errors
        }
    }

    public SortedDictionary<decimal, decimal> Gains { get; } = new();

    public decimal FindClosest(decimal ghz) => Gains.MinBy(e => Math.Abs(e.Key - ghz)).Value;
}
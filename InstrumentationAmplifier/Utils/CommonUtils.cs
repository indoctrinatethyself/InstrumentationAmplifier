using System;
using System.Collections.Generic;
using System.Numerics;

namespace InstrumentationAmplifier.Utils;

public static class CommonUtils
{
    public static T Range<T>(T num, T min, T max) where T : IComparisonOperators<T, T, bool>
    {
        if (num < min) num = min;
        if (num > max) num = max;
        return num;
    }
    
    public static double GetMedianInSortedArray<T>(IReadOnlyList<T> source)
        where T : INumber<T>, IDivisionOperators<T, double, double>
    {
        if (source == null || source.Count == 0)
            throw new Exception("Median of empty array not defined.");

        int mid = source.Count / 2;
        return source.Count % 2 != 0
            ? source[mid] / 1
            : (source[mid] + source[mid - 1]) / 2;
    }

    public static double GetMedianInSortedArray<TSource, TKey>
        (IReadOnlyList<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : INumber<TKey>, IDivisionOperators<TKey, double, double>
    {
        if (source == null || source.Count == 0)
            throw new Exception("Median of empty array not defined.");

        int mid = source.Count / 2;
        return source.Count % 2 != 0
            ? keySelector(source[mid]) / 1
            : (keySelector(source[mid]) + keySelector(source[mid - 1])) / 2;
    }
}
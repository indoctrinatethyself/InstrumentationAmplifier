using System;

namespace InstrumentationAmplifier.Utils;

public static class ExceptionExtensions
{
    public static string ToShortString(this Exception e) => e.GetType() + ": " + e.Message;
}
using System;

namespace InstrumentationAmplifier.Services;

public interface IExceptionsLogger
{
    void Log(string text);
    void Log(Exception exception, string? source = null);
}
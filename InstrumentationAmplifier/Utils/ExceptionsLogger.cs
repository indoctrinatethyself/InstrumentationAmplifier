using System;
using System.IO;
using InstrumentationAmplifier.Services;

namespace InstrumentationAmplifier.Utils;

public class ExceptionsLogger : IExceptionsLogger
{
    private const string LogFileName = "exceptions.log";

    private static readonly object _locker = new();

    public void Log(String text)
    {
        string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}]\n{text}\n";
        
        try
        {
            lock (_locker)
            {
                Console.Error.WriteLine(log);

                using var stream = File.AppendText(LogFileName);
                stream.WriteLine(log);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error writing logs to file ./{LogFileName}\n{e}\n");
        }
    }

    public void Log(Exception exception, string? source = null)
    {
        string exceptionText = exception?.ToString() ?? "Неизвестная ошибка";
        string log = source == null
            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}]\n{exceptionText}\n"
            : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] {source}\n{exceptionText}\n";
        try
        {
            Console.Error.WriteLine(log);

            lock (_locker)
            {
                using FileStream fileStream = new(LogFileName, FileMode.Append, FileAccess.Write);
                using StreamWriter writer = new(fileStream);
                writer.WriteLine(log);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error writing logs to file ./{LogFileName}\n{e}\n");
        }
    }
}
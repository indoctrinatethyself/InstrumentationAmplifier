using System;
using System.IO;
using System.Text.Json;
using InstrumentationAmplifier.Configuration;
using InstrumentationAmplifier.Utils;

namespace InstrumentationAmplifier.Services;

public class ApplicationConfigurationService
{
    private const string FileName = "configuration.json";

    private ApplicationConfiguration _configuration;

    public ApplicationConfigurationService()
    {
        try
        {
            if (File.Exists(FileName))
            {
                using var fileStream = File.Open(FileName, FileMode.Open, FileAccess.Read);
                _configuration = JsonSerializer.Deserialize<ApplicationConfiguration>(fileStream)!;
            }
        }
        catch (Exception e)
        {
            ConfigurationLoadingException = e;
        }

        _configuration ??= new();
    }


    public ApplicationConfiguration Configuration => _configuration;

    public Exception? ConfigurationLoadingException { get; }

    public void Save()
    {
        using var fileStream = File.Open(FileName, FileMode.Create);
        JsonSerializer.Serialize(fileStream, _configuration,
            new JsonSerializerOptions(JsonOptionsConstants.Options) { WriteIndented = true });
    }

    public void Save(ApplicationConfiguration configuration)
    {
        using var fileStream = File.Open(FileName, FileMode.Create);
        JsonSerializer.Serialize(fileStream, configuration,
            new JsonSerializerOptions(JsonOptionsConstants.Options) { WriteIndented = true });
        _configuration = configuration;
    }
}
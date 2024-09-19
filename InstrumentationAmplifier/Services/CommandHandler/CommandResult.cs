using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstrumentationAmplifier.Utils;

namespace InstrumentationAmplifier.Services.CommandHandler;

public class CommandResult
{
    public CommandResult(CommandResultCode code, String message, Object? data = null)
    {
        Code = code;
        Message = message;
        Data = data;
    }

    public CommandResultCode Code { get; init; }
    public string Message { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
    
    
    public static CommandResult Ok(string message = "Ok", object? data = null) =>
        new(CommandResultCode.Ok, message, data);

    public static CommandResult UnknownCommand(string message = "Unknown command", object? data = null) =>
        new(CommandResultCode.UnknownCommand, message, data);

    public static CommandResult InvalidArguments(string message = "Invalid arguments", object? data = null) =>
        new(CommandResultCode.InvalidArguments, message, data);
    
    public static CommandResult ExecutionError(string message = "Execution error", object? data = null) =>
        new(CommandResultCode.ExecutionError, message, data);

    public static implicit operator CommandResponse(CommandResult result) =>
        JsonSerializer.Serialize(result, JsonOptionsConstants.Options);
}

public enum CommandResultCode
{
    Ok = 0,
    UnknownCommand = 1,
    InvalidArguments = 2,
    ExecutionError = 3
}
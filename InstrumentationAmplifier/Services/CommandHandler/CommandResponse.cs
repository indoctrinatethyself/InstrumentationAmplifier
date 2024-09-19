using System.Text;

namespace InstrumentationAmplifier.Services.CommandHandler;

public abstract class CommandResponse
{
    private byte[]? _bytesValue;
    private string? _stringValue;

    public byte[] BytesValue => _bytesValue ??= AsBytes();
    public string StringValue => _stringValue ??= AsString();
    public bool IsStringValue => this is StringCommandResponse;

    protected abstract byte[] AsBytes();
    protected abstract string AsString();

    public static implicit operator byte[](CommandResponse response) => response.BytesValue;
    public static implicit operator string(CommandResponse response) => response.StringValue;

    public static implicit operator CommandResponse(byte[] value) => new BytesCommandResponse(value);
    public static implicit operator CommandResponse(string value) => new StringCommandResponse(value);
}

public class StringCommandResponse : CommandResponse
{
    private readonly string _value;
    public StringCommandResponse(string value) => _value = value;

    protected override byte[] AsBytes() => Encoding.UTF8.GetBytes(_value);
    protected override string AsString() => _value;
}

public class BytesCommandResponse : CommandResponse
{
    private readonly byte[] _value;
    public BytesCommandResponse(byte[] value) => _value = value;

    protected override byte[] AsBytes() => _value;
    protected override string AsString() => Encoding.UTF8.GetString(_value);
}
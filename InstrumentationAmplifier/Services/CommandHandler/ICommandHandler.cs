namespace InstrumentationAmplifier.Services.CommandHandler;

public interface ICommandHandler
{
    public CommandResponse? Handle(string message);
}
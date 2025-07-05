using System.CommandLine;

namespace TimesheetsLogger.Commands;

public static class CommandExtensions
{
    public static Command AddCommand<TCommand>(this Command command) where TCommand : Command, new()
    {
        var subCommand = new TCommand();
        command.Subcommands.Add(subCommand);

        return command;
    }
}

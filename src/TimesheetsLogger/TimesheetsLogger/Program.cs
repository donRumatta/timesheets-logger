using Spectre.Console;
using System.CommandLine;
using System.Globalization;
using TimesheetsLogger.Commands;

namespace TimesheetsLogger
{
    internal class Program
    {
        private static Option<string>  _cultureOption = new Option<string>("--culture", "-c")
        {
            Description = "Culture to deal with dates",
            DefaultValueFactory = _ => "ru-RU",
            Recursive = true
        };

        private static async Task<int> Main(string[] args)
        {
            try
            {
                var rootCommand = CreateRootCommand();

                var config = new CommandLineConfiguration(rootCommand)
                {
                    EnableDefaultExceptionHandler = false
                };
                var parseResult = rootCommand.Parse(args, config);

                SetCulture(parseResult);

                return await parseResult.InvokeAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteException(ex);
                AnsiConsole.WriteLine();

                return 1;
            }
        }

        private static void SetCulture(ParseResult parseResult)
        {
            var culture = parseResult.GetValue(_cultureOption);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            AnsiConsole.MarkupLine($"Using culture: {culture}");
        }

        private static RootCommand CreateRootCommand()
        {
            RootCommand rootCommand = new("Utility for logging your work to Jira.");
            rootCommand.Options.Add(_cultureOption);
            rootCommand.AddCommand<LogWorkCommand>();

            return rootCommand;
        }
    }
}
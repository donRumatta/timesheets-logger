using Spectre.Console;
using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TimesheetsParser;

namespace TimesheetsLogger.Commands
{
    internal class LogWorkCommand : Command
    {
        record JiraOptions(string Url, string Login, string Password);

        private Option<string> _filePathOption = new Option<string>("--file-path", "-fp")
        {
            Description = "Absolute path to file which contains work logs",
            Required = true
        };

        private Option<string> _jiraUrlOption = new Option<string>("--jira-url", "-ju")
        {
            Description = "URL of your Jira API (v2)",
            Required = true
        };

        private Option<string> _jiraLoginOption = new Option<string>("--jira-login", "-jl")
        {
            Description = "Your Jira login",
            Required = true
        };

        private Option<string> _jiraPasswordOption = new Option<string>("--jira-password", "-jp")
        {
            Description = "Your Jira password",
            Required = true
        };

        private Option<decimal> _daylyHoursOption = new Option<decimal>("--dayly-hours", "-dh")
        {
            Description = "Amount of your daily working hours",
            DefaultValueFactory = _ => 8m,
        };

        public LogWorkCommand() : base("log-work", "Log work to Jira")
        {
            Aliases.Add("lw");

            Options.Add(_filePathOption);
            Options.Add(_jiraUrlOption);
            Options.Add(_jiraLoginOption);
            Options.Add(_jiraPasswordOption);
            Options.Add(_daylyHoursOption);

            SetAction(CommandAction);
        }

        private async Task CommandAction(ParseResult parseResult)
        {
            var daylyHours = parseResult.GetValue(_daylyHoursOption);

            var jiraOptions = new JiraOptions(
                parseResult.GetValue(_jiraUrlOption),
                parseResult.GetValue(_jiraLoginOption),
                parseResult.GetValue(_jiraPasswordOption));

            var filePath = parseResult.GetValue(_filePathOption);
            var parser = await Parser.CreateAsync(filePath);

            var timesheets = parser.Parse();
            if (timesheets.Any())
            {
                using (var httpClient = CreateHttpClient(jiraOptions))
                {
                    var daySeconds = 0;

                    var currentDate = timesheets[0].Date.Date;
                    OutputCurrentDate(currentDate);

                    var i = 0;
                    while (i < timesheets.Count)
                    {
                        var item = timesheets[i];

                        ProcessDateChange(ref currentDate, ref daySeconds, item, daylyHours);

                        daySeconds += item.SpentSeconds;

                        //await SendToJira(item, httpClient);
                        await parser.SetProcessed(item.Line);

                        AnsiConsole.MarkupLine($"[green]PROCESSED {item}[/]");

                        i++;
                    }

                    CheckHoursAmount(daySeconds, daylyHours);
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[lime]Done![/]");
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]No data found...[/]");
            }
        }

        private static async Task SendToJira(TimeSheet timeSheet, HttpClient httpClient)
        {
            var jiraTimesheet = JsonSerializer.Serialize(new
            {
                timeSpentSeconds = timeSheet.SpentSeconds,
                comment = timeSheet.Message,
                started = $"{timeSheet.Date:yyyy-MM-dd}T12:00:00.000+0300"
            });

            var requestContent =
                new StringContent(jiraTimesheet, Encoding.UTF8, "application/json");

            var response =
                await httpClient.PostAsync($"issue/{timeSheet.Issue}/worklog", requestContent);

            response.EnsureSuccessStatusCode();
        }

        private static HttpClient CreateHttpClient(JiraOptions jiraOptions)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(jiraOptions.Url)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var credsString = $"{jiraOptions.Login}:${jiraOptions.Password}";
            var credsBytes = Encoding.Default.GetBytes(credsString);
            var authHeader = "Basic " + Convert.ToBase64String(credsBytes);
            client.DefaultRequestHeaders.Add("Authorization", authHeader);

            return client;
        }

        private static void ProcessDateChange(
            ref DateTime currentDate,
            ref int daySeconds,
            TimeSheet item, 
            decimal daylyHours)
        {
            if (currentDate != item.Date)
            {
                CheckHoursAmount(daySeconds, daylyHours);
                daySeconds = 0;

                currentDate = item.Date;
                OutputCurrentDate(currentDate);
            }
        }

        private static void CheckHoursAmount(int daySeconds, decimal daylyHours)
        {
            if (daySeconds / 3600 != daylyHours)
            {
                var ts = new TimeSpan(0, 0, daySeconds);
                AnsiConsole.MarkupLine($"[orangered1]The amount of dayly hours does not equal {daylyHours}[/] [purple_2]({ts.Hours} hours {ts.Minutes} minutes)[/]");
            }
        }

        private static void OutputCurrentDate(DateTime currentDate)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]{currentDate:d}[/]");
        }
    }
}

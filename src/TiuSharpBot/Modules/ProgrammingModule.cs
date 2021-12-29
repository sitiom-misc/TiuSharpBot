using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using F23.StringSimilarity;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TioSharp;

namespace TiuSharpBot.Modules
{
    [Name("Programming"), Summary("Tools for the programmer")]
    public class ProgrammingModule : ModuleBase<SocketCommandContext>
    {
        private static readonly TioApi Compiler = new();
        private readonly IConfigurationRoot _config;
        public InteractiveService Interactive { get; set; }

        // Common identifiers, also used in highlight.js and thus discord code blocks
        private readonly Dictionary<string, string> _quickMap = new()
        {
            {
                "asm",
                "assembly"
            },
            {
                "c#",
                "cs"
            },
            {
                "c++",
                "cpp"
            },
            {
                "csharp",
                "cs"
            },
            {
                "f#",
                "fs"
            },
            {
                "fsharp",
                "fs"
            },
            {
                "js",
                "javascript"
            },
            {
                "nimrod",
                "nim"
            },
            {
                "py",
                "python"
            },
            {
                "q#",
                "qs"
            },
            {
                "rs",
                "rust"
            },
            {
                "sh",
                "bash"
            }
        };

        public ProgrammingModule(IConfigurationRoot config)
        {
            _config = config;
        }

        [Command("run"), Summary(@"Run the specified code.
__Usage__
tiu!run [--stats]
\`\`\`<language>
<code>
\`\`\`
[`input <input>`...]
[`compiler-flags <flag>`...]
[`command-line-options <option>`...]
[`args <arg>`...]

**Or,**

tiu!run <language> [--stats]
[`input <input>`...]
[`compiler-flags <flag>`...]
[`command-line-options <option>`...]
[`args <arg>`...]

`<attachment not exceeding 20 kio>`")]
        public async Task RunCode([Remainder][Summary("The message to parse.")] string message)
        {
            string[] args;
            string[] subArgs1;

            string language;
            string code;

            List<string> inputs = new();
            List<string> compilerFlags = new();
            List<string> commandLineOptions = new();
            List<string> arguments = new();

            var file = Context.Message.Attachments.ElementAtOrDefault(0);
            if (file != null)
            {
                if (file.Size > 20000)
                {
                    await ReplyAsync("File must be smaller than 20 kio");
                    return;
                }

                using (StringReader stringReader = new(message))
                {
                    args = new[]
                    {
                        await stringReader.ReadLineAsync(),
                        await stringReader.ReadToEndAsync()
                    };
                }

                subArgs1 = args[0].Split(' ', '\r', '\n');

                language = subArgs1[0];
                code = await (await new HttpClient().GetAsync(file.Url)).Content.ReadAsStringAsync();
                if (code.Length > 20000)
                {
                    await ReplyAsync("Code must be shorter than 20,000 characters");
                    return;
                }
            }
            else
            {
                args = message.Split("```");

                subArgs1 = args[0].Split(' ', '\r', '\n'); // Should return an array of size 3

                using StringReader stringReader = new(args[1]);
                language = await stringReader.ReadLineAsync();
                code = await stringReader.ReadToEndAsync();
            }

            bool showStats = subArgs1.Contains("--stats");

            foreach (string line in (file == null ? args[2] : args[1]).Split('\r', '\n').Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                if (line.StartsWith("input "))
                {
                    inputs.Add(string.Join(' ', line.Split(' ')[1..]).Trim('`'));
                }
                else if (line.StartsWith("compiler-flags "))
                {
                    compilerFlags.AddRange(line[15..].Trim('`').Split(' '));
                }
                else if (line.StartsWith("command-line-options "))
                {
                    commandLineOptions.AddRange(line[21..].Trim('`').Split(' '));
                }
                else if (line.StartsWith("args "))
                {
                    arguments.AddRange(line[10..].Trim('`').Split(' '));
                }
            }

            // Match language to available languages
            if (_quickMap.ContainsKey(language!))
            {
                language = _quickMap[language];
            }
            if (_config.GetSection(language).Exists())
            {
                language = _config[language];
            }
            else if (Compiler.Languages.All(l => l != language))
            {
                var l = new JaroWinkler();
                // Get first 10 matches with >85% similarity
                string matches = string.Join('\n',
                    Compiler.Languages.Where(c => l.Similarity(language, c) > .85).Take(10));
                Console.WriteLine(args[0]);
                string matchesReply = $"`{language}` not available.\n\n";
                if (!string.IsNullOrWhiteSpace(matches))
                {
                    matchesReply += $"**Did you mean:**\n{matches}";
                }

                await ReplyAsync(matchesReply);
                return;
            }

            var result = await CreateRunResponse(language, code, inputs.ToArray(), compilerFlags.ToArray(), args.ToArray(),
                showStats);

            var builder = new ComponentBuilder()
                .WithButton("Run Again", "tiu_run_again", ButtonStyle.Secondary, new Emoji("🔄"))
                .WithButton("Delete", "tiu_run_delete", ButtonStyle.Secondary, new Emoji("🗑"));

            RestUserMessage messageResult;
            // Send result as file if it exceeds maximum message limit
            if (result.Length > 2000 || result.Count(c => c.Equals('\n')) + 1 > 40)
            {
                await using var resultStream = new MemoryStream();
                await using var writer = new StreamWriter(resultStream);

                await writer.WriteAsync(result);
                await writer.FlushAsync();
                resultStream.Seek(0, SeekOrigin.Begin);

                messageResult = await Context.Channel.SendFileAsync(resultStream, "result.txt", components: builder.Build(),
                    messageReference: new MessageReference(Context.Message.Id));
            }
            else
            {
                messageResult = await Context.Channel.SendMessageAsync($"```\n{result}\n```", components: builder.Build(), messageReference: new MessageReference(Context.Message.Id));
            }


            // Message result interaction loop
            while (true)
            {
                var nextInteraction = await Interactive.NextInteractionAsync(
                    x => x is SocketMessageComponent c && c.Message.Id == messageResult.Id && c.User.Id == Context.User.Id,
                    timeout: TimeSpan.FromMinutes(1));

                switch (nextInteraction.Status)
                {
                    case InteractiveStatus.Success:
                        var customId = ((SocketMessageComponent)nextInteraction.Value).Data.CustomId;
                        switch (customId)
                        {
                            case "tiu_run_again":
                                result = await CreateRunResponse(language, code, inputs.ToArray(), compilerFlags.ToArray(),
                                    args.ToArray(),
                                    showStats);
                                if (result.Length > 2000 || result.Count(c => c.Equals('\n')) + 1 > 40)
                                {
                                    await using var resultStream = new MemoryStream();
                                    await using var writer = new StreamWriter(resultStream);

                                    await writer.WriteAsync(result);
                                    await writer.FlushAsync();
                                    resultStream.Seek(0, SeekOrigin.Begin);

                                    await messageResult.ModifyAsync(x =>
                                    {
                                        x.Attachments = new Optional<IEnumerable<FileAttachment>>(new[]
                                            { new FileAttachment(resultStream, "result.txt") });
                                    });
                                }
                                else
                                {
                                    await messageResult.ModifyAsync(x => { x.Content = $"```\n{result}\n```"; });
                                }
                                break;
                            case "tiu_run_delete":
                                await messageResult.DeleteAsync();
                                return;
                        }
                        break;
                    case InteractiveStatus.Timeout:
                    case InteractiveStatus.Canceled:
                        // Remove components from message
                        await messageResult.ModifyAsync(x => { x.Components = new ComponentBuilder().Build(); });
                        return;
                    default:
                        return;
                }
            }
        }

        [Command("lang", RunMode = RunMode.Async), Summary("Returns a list of available languages from tio.run")]
        public async Task ListLanguages()
        {
            await Compiler.RefreshLanguagesAsync();
            var pageContents = Compiler.Languages
                .Select((s, i) => new { Value = $"{i + 1}. {s}", Index = i })
                .GroupBy(x => x.Index / 10)
                .Select(grp => string.Join('\n', grp.Select(x => x.Value)))
                .ToArray();

            var paginator = new LazyPaginatorBuilder()
                .AddUser(Context.User)
                .WithPageFactory(GeneratePageAsync)
                .WithMaxPageIndex(pageContents.Length)
                .Build();

            var message = await Interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(1), resetTimeoutOnInput: true);

            // Remove the paginator buttons
            await message.Message.ModifyAsync(x => { x.Components = new ComponentBuilder().Build(); });

            Task<PageBuilder> GeneratePageAsync(int index)
            {
                var page = new PageBuilder()
                    .WithTitle("Languages List")
                    .WithColor(new Color(55, 129, 255))
                    .WithDescription(
                        $"{pageContents[index]}\n\nView them on [tio.run](https://tio.run/), or in [JSON format](https://tio.run/languages.json)")
                    .WithAuthor(
                        "tio.run",
                        "https://avatars.githubusercontent.com/u/24327566",
                        "https://tio.run/");

                return Task.FromResult(page);
            }
        }

        private static async Task<string> CreateRunResponse(string language, string code, string[] inputs, string[] compilerFlags, string[] args, bool showStats)
        {
            // Create and send response
            byte[] requestData = Compiler.CreateRequestData(language, code, inputs, compilerFlags, args);
            string result = await Compiler.SendAsync(requestData);

            if (showStats) return result;

            // Remove stats in result
            string[] lines = result.Split('\r', '\n');
            result = string.Join('\n', lines
                .SkipLast(5)) + '\n' + lines.TakeLast(1).ElementAt(0);

            return result;
        }
    }
}

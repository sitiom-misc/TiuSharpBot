using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace TiuSharpBot.Modules
{
	[Name("Help"), Summary("Get some help")]
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly CommandService _service;
		private readonly IConfigurationRoot _config;

		public HelpModule(CommandService service, IConfigurationRoot config)
		{
			_service = service;
			_config = config;
		}

		[Command("help"), Summary("Get some help")]
		public async Task HelpAsync()
		{
			string prefix = _config["prefix"];
			var builder = new EmbedBuilder
			{
				Description = "These are the commands you can use.\nSpecify a command to see more information.",
				Color = new Color(55, 129, 255),
				ThumbnailUrl = "https://cdn.discordapp.com/app-icons/757871270862782465/fda047d017c9f256691357f098a6436c.png"
			};

			foreach (ModuleInfo module in _service.Modules)
			{
				string description = string.Empty;
				for (var i = 0; i < module.Commands.Count; i++)
				{
					CommandInfo cmd = module.Commands[i];
					if (cmd.Name != module.Commands.ElementAtOrDefault(i - 1)?.Name)
					{
						PreconditionResult result = await cmd.CheckPreconditionsAsync(Context);
						if (result.IsSuccess)
							description += $"{prefix}{cmd.Name}\n";
					}
				}

				if (!string.IsNullOrWhiteSpace(description))
				{
					builder.AddField(x =>
					{
						x.Name = $"__**{module.Name}**__";
						x.Value = $"`{description}`";
						x.IsInline = true;
					});
				}
			}

			await ReplyAsync("", false, builder.Build());
		}

		[Command("help"), Summary("Get some specific help")]
		public async Task HelpAsync(string command)
		{
			var result = _service.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"Sorry, I couldn't find a command like `{command}`.");
				return;
			}

			var builder = new EmbedBuilder
			{
				Title = "Help",
				Color = new Color(55, 129, 255),
				ThumbnailUrl = "https://img.icons8.com/color/2x/help.png"
			};

			foreach (var match in result.Commands)
			{
				var cmd = match.Command;

				builder.AddField(new EmbedFieldBuilder
				{
					Name = $"`{string.Join(", ", cmd.Aliases)}`",
					Value = string.IsNullOrWhiteSpace(cmd.Summary) ? "*No description provided.*" : cmd.Summary
				});

				if (cmd.Parameters.Count != 0)
				{
					string arguments = cmd.Parameters.Aggregate(string.Empty, (current, parameter) => current + '`' + (parameter.IsMultiple ? $"[{parameter}...]" : $"<{parameter}>") + $": {parameter.Type.Name}`: {(string.IsNullOrWhiteSpace(parameter.Summary) ? "*No description provided.*" : parameter.Summary)}\n");
					builder.AddField(new EmbedFieldBuilder
					{
						Name = "__Arguments__",
						Value = string.IsNullOrWhiteSpace(arguments) ? "*No description provided.*" : $"{arguments}\n"
					});
				}
			}

			await ReplyAsync("", false, builder.Build());
		}
	}
}

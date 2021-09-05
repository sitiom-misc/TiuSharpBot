using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Net;
using Newtonsoft.Json;

namespace TiuSharpBot.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _discord.Ready += Client_Ready;
            _commands = commands;
        }

        private async Task Client_Ready()
        {
            var pingCommand = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Measure bot latency");

            try
            {
                await _discord.Rest.CreateGlobalCommand(pingCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        public async Task StartAsync()
        {
            await _discord.LoginAsync(TokenType.Bot, _config["BOT_TOKEN"]);
            await _discord.StartAsync();
            await _discord.SetGameAsync($"{_config["BOT_PREFIX"]}help", null, ActivityType.Listening);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }
    }
}

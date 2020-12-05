using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace TiuSharpBot.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfigurationRoot configuration)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _config = configuration;
        }

        public async Task StartAsync()
        {
            await _discord.LoginAsync(TokenType.Bot, _config["DISCORD_TOKEN"]);
            await _discord.StartAsync();
            await _discord.SetGameAsync("your PBL", null, ActivityType.Watching);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }
    }
}

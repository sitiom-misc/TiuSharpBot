using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace TiuSharpBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return; // Ignore self

            var context = new SocketCommandContext(_discord, msg);

            // Check if the message has a valid command prefix
            int argPos = 0;
            if (msg.HasStringPrefix(_config["BOT_PREFIX"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var typingState = context.Channel.EnterTypingState();
                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                typingState.Dispose();

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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
            if (!(s is SocketUserMessage msg)) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return; // Ignore self

            var context = new SocketCommandContext(_discord, msg);

            // Check if the message has a valid command prefix
            int argPos = 0;
            if (msg.HasStringPrefix(_config["BOT_PREFIX"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                IResult result;

                using (context.Channel.EnterTypingState())
                {
                    result = await _commands.ExecuteAsync(context, argPos, _provider);
                }

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
            else if (msg.Channel is IPrivateChannel && msg.Attachments.Count > 0)
            {
                await msg.Channel.SendMessageAsync("Hi, your submission will be looked on by the officers now.");

                // Send to iJSD Server
                await _discord.GetGuild(756114649144885258).GetTextChannel(765938417263312898)
                    .SendMessageAsync($"From {msg.Author.Mention}:\n{msg.Attachments.ElementAt(0).Url}");
            }
        }
    }
}

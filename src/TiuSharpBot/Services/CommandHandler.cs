using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

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
            _discord.InteractionCreated += Client_InteractionCreated;
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            if (arg is SocketSlashCommand command)
            {
                await SlashCommandHandler(command);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand interaction)
        {
            switch (interaction.Data.Name)
            {
                // Only ping command as of now
                case "ping":
                    await interaction.RespondAsync("ℹ️ | Pong!");
                    var responseTimestamp = (await interaction.GetOriginalResponseAsync()).CreatedAt;

                    await interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = $"ℹ️ | Pong! - Time taken: **{interaction.CreatedAt - responseTimestamp:fff}ms**";
                    });
                    break;
            }
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

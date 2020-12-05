using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TiuSharpBot.Services
{
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            discord.Log += OnLogAsync;
            commands.Log += OnLogAsync;
        }

        private static Task OnLogAsync(LogMessage msg)
            => Console.Out.WriteLineAsync($"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}");
    }
}

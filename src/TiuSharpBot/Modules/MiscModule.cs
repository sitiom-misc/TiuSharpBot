﻿using Discord;
using Discord.Commands;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TiuSharpBot.Modules
{
    [Name("Misc"), Summary("Miscellaneous stuff")]
    public class MiscModule : ModuleBase<SocketCommandContext>
    {
        private readonly string[] _responses =
        {
            "Most likely.",
            "Very doubtful",
            "It is certain.",
            "You may rely on it.",
            "Cannot predict now.",
            "Reply hazy, try again.",
            "My reply is no.",
            "As I see it, yes.",
            "Yes - definitely.",
            "Ask again later.",
            "Concentrate and ask again.",
            "It is decidedly so.",
            "My sources say no.",
            "Yes.",
            "Without a doubt.",
            "Outlook not so good.",
            "Outlook good.",
            "Don't count on it.",
            "Signs point to yes.",
            "Better not tell you now."
        };

        [Command("ask"), Summary("Ask a question, He answers all.")]
        public async Task Ask([Remainder][Summary("The question")] string question)
        {
            await ReplyAsync(_responses[new Random().Next(0, 19)], messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("ping")]
        [Summary("Measure bot latency")]
        public async Task GetLatencyAsync()
        {
            var message = await ReplyAsync("ℹ️ | Pong!");
            await message.ModifyAsync(x =>
            {
                x.Content = $"ℹ️ | Pong! - Time taken: **{message.Timestamp - Context.Message.Timestamp:fff}ms**";
            });
        }

        [Command("signofthecross")]
        [Summary("Sacramental in the Church of Tiu.")]
        public async Task SignOfTheCross()
        {
            await ReplyAsync("In the name of .Where(), .Select(), and .Aggregate(). Amen!", messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("face"), Summary("Generate a face from https://thispersondoesnotexist.com/")]
        public async Task ThisPersonDoesNotExist()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2");

            var image = await (await client.GetAsync("https://thispersondoesnotexist.com/image")).Content.ReadAsStreamAsync();

            await Context.Channel.SendFileAsync(image, "face.jpg", messageReference: new MessageReference(Context.Message.Id));
        }
    }
}

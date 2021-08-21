using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MitsukuApi;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using RestClient = RestSharp.RestClient;

namespace TiuSharpBot.Modules
{
    [Name("Misc"), Summary("Miscellaneous stuff")]
    public class MiscModule : InteractiveBase<SocketCommandContext>
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

        [Command("speak"), Summary("Speak a specified voice.")]
        public async Task VoCodeSpeak(string speaker, string message)
        {
            RestClient client = new("https://mumble.stream/speak_spectrogram");
            RestRequest request = new(Method.POST)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddDecompressionMethod(DecompressionMethods.GZip);
            request.AddDecompressionMethod(DecompressionMethods.Deflate);
            request.AddDecompressionMethod(DecompressionMethods.Brotli);

            request.AddJsonBody($"{{\"text\":\"{message}\",\"speaker\":\"{speaker}\"}}");

            IRestResponse response = await client.ExecutePostAsync(request);

            JObject jObject = JObject.Parse(response.Content);
            string encodedAudio = (string)jObject["audio_base64"];
            MemoryStream data = new(Convert.FromBase64String(encodedAudio!));

            await Context.Channel.SendFileAsync(data, "audio.wav", messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("talk"), Summary("Start a conversation with Sir Tiu. Conversation ends with 2 minutes of inactivity.")]
        public async Task Talk([Remainder] string message)
        {
            MitsukuChatBot mitsuku = new();
            MitsukuResponse reply = await mitsuku.SendMessageAsync(message);

            foreach (string msg in reply.Responses)
            {
                await ReplyAsync(msg);
            }

            do
            {
                SocketMessage response = await NextMessageAsync(true, true, TimeSpan.FromMinutes(1.5));

                if (response != null)
                {
                    using var typingState = Context.Channel.EnterTypingState();
                    reply = await mitsuku.SendMessageAsync(response.Content);

                    foreach (string msg in reply.Responses)
                    {
                        await ReplyAsync(msg);
                    }
                }
                else
                    break;
            } while (true);
        }

        [Command("ping")]
        [Summary("Displays the current latency of the bot.")]
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
        public async Task ThisPersonDoesNotExists() =>
            await Context.Channel.SendFileAsync(
                new MemoryStream(new WebClient
                {
                    Headers =
                    {
                        [HttpRequestHeader.UserAgent] =
                            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"
                    }
                }.DownloadData("https://thispersondoesnotexist.com/image")), "face.jpg",
                messageReference: new MessageReference(Context.Message.Id));
    }
}

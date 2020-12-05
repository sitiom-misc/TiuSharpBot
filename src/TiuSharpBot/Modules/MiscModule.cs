using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MitsukuApi;
using Newtonsoft.Json.Linq;
using RestSharp;
using TiuSharpBot.Models;
using TiuSharpBot.Utils.Callbacks;
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
		public List<ScoreEntry> ScoreEntries;

		[Command("ask"), Summary("Ask a question, He answers all.")]
		public async Task Ask([Remainder] [Summary("The message to ask")]
			string word)
		{
			await ReplyAsync($"> {word}\n{_responses[new Random().Next(0, 19)]}");
		}

		[Command("speak"), Summary("Speak a specified voice.")]
		public async Task VoCodeSpeak(string speaker, string message)
		{
			RestClient client = new RestClient("https://mumble.stream/speak_spectrogram");
			RestRequest request = new RestRequest(Method.POST)
			{
				RequestFormat = DataFormat.Json
			};
			request.AddDecompressionMethod(DecompressionMethods.GZip);
			request.AddDecompressionMethod(DecompressionMethods.Deflate);
			request.AddDecompressionMethod(DecompressionMethods.Brotli);

			request.AddJsonBody($"{{\"text\":\"{message}\",\"speaker\":\"{speaker}\"}}");

			IRestResponse response = await client.ExecutePostAsync(request);

			Console.WriteLine(response.Content);
			JObject jObject = JObject.Parse(response.Content);
			string encodedAudio = (string)jObject["audio_base64"];
			MemoryStream data = new MemoryStream(Convert.FromBase64String(encodedAudio!));

			await Context.Channel.SendFileAsync(data, "audio.wav", $"> {message}");
		}

		[Command("talk"), Summary("Start a conversation with Sir Tiu. Conversation ends with 2 minutes of inactivity.")]
		public async Task Talk([Remainder] string message)
		{
			MitsukuChatBot mitsuku = new MitsukuChatBot();
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

		[Command("leaderboard"), Summary("Set up a dream.lo leaderboard to track")]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		public async Task Leaderboard([Summary("The public code of the dream.lo leaderboard")] string publicCode)
		{
			string uri = $"http://dreamlo.com/lb/{publicCode}/json-asc";

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			await using Stream stream = response.GetResponseStream();
			try
			{
				//Deserialize Result
				JObject leaderboard = JObject.Parse(await new StreamReader(stream).ReadToEndAsync());

				List<JToken> results = leaderboard["dreamlo"]?["leaderboard"]?["entry"]?.Children()
					.ToList();
				ScoreEntries = (results ?? throw new InvalidOperationException())
					.Select(result => result.ToObject<ScoreEntry>()).Take(10).ToList();

				EmbedBuilder builder = new EmbedBuilder
				{
					Title = "🏆 Labyrinth Top 10 Leaderboards",
					Color = new Color(179, 10, 14),
					ThumbnailUrl = "https://cdn.discordapp.com/app-icons/770277582460551178/99850e049466bf1ed9370e3eed73ed00.png",
					Footer = new EmbedFooterBuilder
					{
						Text = "Note: Changing your discord name (not server nickname) may result in your discord tag not showing in the Leaderboard"
					},
					Description = "__**Rankings:**__\n"
				};

				string[] topTenEmojis = { "🥇", "🥈", "🥉", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
				for (var i = 0; i < ScoreEntries.Count; i++)
				{
					ScoreEntry scoreEntry = ScoreEntries[i];
					string username = scoreEntry.Name.Split('#')[0];
					string discriminator = scoreEntry.Name.Split('#')[1];
					builder.Description +=
						$"{topTenEmojis[i]} - {Context.Client.GetUser(username, discriminator)?.Mention ?? $"@{username}#{discriminator}"} - 🔹 **`{scoreEntry.Score} points`**\n\n";
				}

				var message = await ReplyAsync(null, false, builder.Build());
				await ReplyAsync($"Leaderboard set to track on {MentionUtils.MentionChannel(Context.Channel.Id)}!");
				await new LeaderboardReactionCallback(Interactive, Context, message, ScoreEntries, uri).StartAsync();
			}
			catch
			{
				await ReplyAsync("Invalid dream.lo leaderboard code.");
			}
		}


		[Command("face"), Summary("Generate a face from https://thispersondoesnotexist.com/")]
		public async Task ThisPersonDoesNotExists() =>
			await Context.Channel.SendFileAsync(
				new MemoryStream(new WebClient
				{
					Headers =
					{
						// you need to fake a HttpRequestHeader so the site let you download the image.
						[HttpRequestHeader.UserAgent] =
							"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"
					}
				}.DownloadData("https://thispersondoesnotexist.com/image")), "face.jpg",
				Context.Message.Author.Mention);
	}
}

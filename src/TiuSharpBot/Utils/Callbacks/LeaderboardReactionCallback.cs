using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using TiuSharpBot.Models;

namespace TiuSharpBot.Utils.Callbacks
{
	public class LeaderboardReactionCallback : IReactionCallback
	{
		public SocketCommandContext Context { get; }
		public InteractiveService Interactive { get; }
		public IUserMessage Message { get; }

		public RunMode RunMode => RunMode.Async;
		public ICriterion<SocketReaction> Criterion { get; } = new Criteria<SocketReaction>();

		public TimeSpan? Timeout { get; }
		public List<ScoreEntry> ScoreEntries { get; set; }
		public string Uri { get; set; }


		public LeaderboardReactionCallback(InteractiveService interactive,
			SocketCommandContext sourceContext, IUserMessage message, List<ScoreEntry> scoreEntries, string uri)
		{
			Interactive = interactive;
			Context = sourceContext;
			Message = message;
			Timeout = TimeSpan.FromMinutes(1);
			ScoreEntries = scoreEntries;
			Uri = uri;

			Interactive.AddReactionCallback(Message, this);
		}

		public async Task StartAsync()
		{
			await Message.AddReactionAsync(new Emoji("🛑"));

			while (true)
			{
				await Task.Delay(Timeout.Value);
				await UpdateLeaderboardAsync();
			}
		}

		private async Task UpdateLeaderboardAsync()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Uri);
			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new StreamReader(stream ?? throw new InvalidOperationException());

			string json = await reader.ReadToEndAsync();
			//Deserialize Result
			JObject leaderboard = JObject.Parse(json);

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

			await Message.ModifyAsync(x => { x.Embed = builder.Build(); });
		}

		// true if we should stop listening, else false
		public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
		{
			if (reaction.Emote.Name != "🛑") return false;

			if (reaction.UserId == Context.User.Id)
			{
				await Message.DeleteAsync().ConfigureAwait(false);
				return true;
			}

			await Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
			return false;
		}
	}
}
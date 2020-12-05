using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace TiuSharpBot.Utils.Callbacks
{
	public class WasteBasketReactionCallback : IReactionCallback
	{
		public SocketCommandContext Context { get; }
		public InteractiveService Interactive { get; }
		public IUserMessage Message { get; }

		public RunMode RunMode => RunMode.Async;
		public ICriterion<SocketReaction> Criterion { get; } = new Criteria<SocketReaction>();

		public TimeSpan? Timeout { get; }


		public WasteBasketReactionCallback(InteractiveService interactive,
			SocketCommandContext sourceContext, IUserMessage message, TimeSpan? timeout = null)
		{
			Interactive = interactive;
			Context = sourceContext;
			Message = message;
			Timeout = timeout;

			Interactive.AddReactionCallback(Message, this);
		}

		public async Task StartAsync()
		{
			await Message.AddReactionAsync(new Emoji("🗑"));

			if (Timeout != null)
			{
				await Task.Delay(Timeout.Value);

				Interactive.RemoveReactionCallback(Message);
				await Message.RemoveReactionAsync(new Emoji("🗑"), Context.Client.CurrentUser);
			}
		}

		// true if we should stop listening, else false
		public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
		{
			if (reaction.Emote.Name != "🗑") return false;

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
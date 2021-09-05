using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiuSharpBot.Modules
{
    [Name("Games"), Summary("Play various games!")]
    public class GamesModule : ModuleBase<SocketCommandContext>
    {
        private readonly string[] _hangmanGraphics =
        {
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |
      |
__      |       __",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |
__      |       __",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso0:808148053801697350>
__      |       __",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso1:808148053558165515> 
__      |       __",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso2:808148053785706506>
__      |       __",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso2:808148053785706506>
__      |       __   <:bottom0:808148053876801607>",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso2:808148053785706506>
__      |       __   <:bottom1:808148053818605568>",
            @"‎‏‏‎‎‏‏‎‎       __            __
      |            |
      |          :worried:
      |          <:torso2:808148053785706506>
__      |       __   <:bottom2:808148053742977035>"
        };

        public InteractiveService Interactive { get; set; }

        [Command("hangman"), Summary("Play hangman. You will be asked to set the prompt and the rest will try to answer.")]
        public async Task Hangman()
        {
            IDMChannel dmChannel = await Context.User.CreateDMChannelAsync();
            bool FromSourceUserDmCriteria(SocketMessage message) => message.Author.Id == Context.User.Id && message.Channel.Id == dmChannel.Id;

            await ReplyAsync($"Waiting for {Context.User.Mention} to set the prompt...");
            await Context.User.SendMessageAsync("Enter the prompt.");
            var response = await Interactive.NextMessageAsync(FromSourceUserDmCriteria, timeout: TimeSpan.FromMinutes(1));
            if (response.IsTimeout)
            {
                await Context.User.SendMessageAsync("Timed out.");
                await ReplyAsync("Timed out.");
                return;
            }
            // Check if message only contains letters and spaces
            while (!response.Value.Content.All(c =>
                char.IsLetter(c) || c == ' '))
            {
                await Context.User.SendMessageAsync("Your input should only contain words separated by spaces.");
                response = await Interactive.NextMessageAsync(FromSourceUserDmCriteria, timeout: TimeSpan.FromMinutes(1));
                if (!response.IsTimeout) continue;
                await Context.User.SendMessageAsync("Timed out.");
                await ReplyAsync("Timed out.");
                return;
            }
            await Context.User.SendMessageAsync("Prompt set!");

            // Setup game variables
            // Consistent capitalization
            string word = response.Value.Content.ToLower();
            IUserMessage hangmanMessage = null;
            List<char> guessedLetters = new();
            bool isWon = false;

            await ReplyAsync("Starting game!");
            for (var i = 0; i < _hangmanGraphics.Length; i++)
            {
                string hangmanGraphic = _hangmanGraphics[i];
                bool isCorrectLetter;
                bool isAlreadyGuessed;
                do
                {
                    StringBuilder message = new($"{hangmanGraphic}\n\n");

                    foreach (char c in word)
                    {
                        if (guessedLetters.Contains(c))
                        {
                            message.Append($":regional_indicator_{c}: ");
                        }
                        else if (c == ' ')
                        {
                            message.Append(' ');
                        }
                        else
                        {
                            message.Append("⬛ ");
                        }
                    }

                    if (hangmanMessage is not null)
                    {
                        await hangmanMessage.DeleteAsync();
                    }

                    hangmanMessage = await ReplyAsync(message.ToString());
                    // Check for win (No more hidden squares / All letters revealed)
                    if (!hangmanMessage.Content.Contains('⬛') &&
                        !hangmanMessage.Content.Contains(":black_large_square:"))
                    {
                        isWon = true;
                        // Break inner loop
                        break;
                    }
                    // Loss
                    if (i == _hangmanGraphics.Length - 1)
                    {
                        break;
                    }

                    SocketMessage nextMessage;
                    do
                    {
                        nextMessage = (await Interactive.NextMessageAsync(msg => msg.Channel.Id == Context.Channel.Id)).Value;
                    } while (nextMessage is null ||
                             nextMessage.Content.Length != 1 && char.IsLetter(nextMessage.Content, 0));

                    char guessedLetter = char.ToLower(nextMessage.Content[0]);

                    isCorrectLetter = word.Contains(guessedLetter);
                    isAlreadyGuessed = guessedLetters.Contains(guessedLetter);

                    if (!isAlreadyGuessed)
                    {
                        guessedLetters.Add(guessedLetter);
                    }
                    else
                    {
                        await ReplyAsync("You have already tried that letter!",
                            messageReference: new MessageReference(nextMessage.Id),
                            allowedMentions: AllowedMentions.None);
                    }
                } while (isCorrectLetter || isAlreadyGuessed);

                // Break outer loop
                if (isWon) break;
            }

            await ReplyAsync(isWon ? $"Congratulations! You have guessed the word: `{word}`" : $"You lost! The word is: `{word}`");
        }
    }
}

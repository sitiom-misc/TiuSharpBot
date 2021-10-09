using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace TiuSharpBot.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("knospecrole")]
        [Summary("Kicks all users without a specific role. Does not kick users higher than the bot in the role hierarchy.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickNoSpecificRole(SocketRole role)
        {
            var kickedUsers = 0;

            foreach (var user in Context.Guild.Users)
            {
                if (user.IsBot || user.Roles.Any(r => r.Id == role.Id) || user.Hierarchy > Context.Guild.CurrentUser.Hierarchy) continue;
                await user.KickAsync();
                kickedUsers++;
            }

            await ReplyAsync($"{kickedUsers} users kicked.");
        }
    }
}

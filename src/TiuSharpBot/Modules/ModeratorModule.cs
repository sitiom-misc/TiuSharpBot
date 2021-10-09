using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TiuSharpBot.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("knospecrole")]
        [Summary("Kicks all users without a specific role.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickNoSpecificRole(SocketRole role)
        {
            var kickedUsers = 0;

            foreach (var user in Context.Guild.Users)
            {
                if (user.IsBot || user.Roles.All(r => r.Id != role.Id)) continue;
                await user.KickAsync();
                kickedUsers++;
            }

            await ReplyAsync($"{kickedUsers} users kicked.");
        }
    }
}

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TiuSharpBot.Modules
{
    [Name("Math"), Summary("Do some math I guess")]
    public class MathModule : ModuleBase<SocketCommandContext>
    {
	    [Command("multiply"), Summary("Get the product of two numbers.")]
	    public async Task Multiply(int a, int b)
        {
            int product = a * b;
            await ReplyAsync($"The product of `{a} * {b}` is `{product}`.");
        }

	    [Command("addmany"), Summary("Get the sum of many numbers")]
	    public async Task AddMany(params int[] numbers)
        {
            int sum = numbers.Sum();
            await ReplyAsync($"The sum of `{string.Join(", ", numbers)}` is `{sum}`.");
        }

	    [Command("goldbach"), Summary("Get all prime sum pairs of a given even integer.")]
	    public async Task Goldbach([Summary("The number to evaluate.")]int num)
        {
	        // Check all sum combinations and return the first one which both pairs are prime
	        if (num % 2 == 0)
	        {
		        StringBuilder sb = new StringBuilder();
		        sb.AppendLine("The pairs are:");
                for (int i = 2, j = num - i; j >= 2 && i <= num / 2; i++, j--)
		        {
			        if (IsPrime(i) && IsPrime(j))
			        {
				        sb.AppendLine($"{num} = {i} + {j}");
			        }
		        }

                await ReplyAsync(sb.ToString());
	        }
	        else
	        {
		        await ReplyAsync("Please revise. Your number is not even.");
	        }
        }

        private bool IsPrime(int n)
        {
	        for (int j = 2; j < n / 2; j++)
	        {
		        if (n % j == 0)
		        {
			        return false;
		        }
	        }
	        return true;
        }
    }
}

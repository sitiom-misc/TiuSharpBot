using System.Threading.Tasks;

namespace TiuSharpBot
{
    class Program
    {
        public static async Task Main()
            => await new Startup().RunAsync();
    }
}

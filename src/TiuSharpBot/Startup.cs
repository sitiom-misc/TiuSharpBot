using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Fergun.Interactive;
using TiuSharpBot.Services;

namespace TiuSharpBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("default_langs.yml")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();

            // Start services
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();
            await provider.GetRequiredService<StartupService>().StartAsync();

            // Keep the program running
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<Random>()
            .AddSingleton(Configuration);
        }
    }
}

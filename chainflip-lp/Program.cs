namespace ChainflipLp
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure.Options;
    using ChainflipLp.Modules;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Telegram.Bot;

    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new();
        
        public static void Main()
        {
            var ct = CancellationTokenSource.Token;

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
                Log.Fatal(
                    (Exception)eventArgs.ExceptionObject,
                    "Encountered a fatal exception, exiting program.");
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.MachineName.ToLowerInvariant()}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            
            var container = ConfigureServices(configuration, ct);
            var logger = container.GetRequiredService<ILogger<Program>>();
            var applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            
            logger.LogInformation(
                "Starting {ApplicationName}",
                applicationName);
            
            Console.CancelKeyPress += (_, eventArgs) =>
            { 
                logger.LogInformation("Requesting stop...");

                CancellationTokenSource.Cancel();

                eventArgs.Cancel = true;
            };

            try
            {
                #if DEBUG
                Console.WriteLine($"Press ENTER to start {applicationName}...");
                Console.ReadLine();
                #endif

                ValidatePools(container.GetRequiredService<IOptions<BotConfiguration>>());
                
                var runner = container.GetRequiredService<Runner>();
                
                var task = runner.Start(CancellationTokenSource.Token);

                Console.WriteLine("Running... Press CTRL + C to exit.");
                task.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                var tasksCancelled = false;
                if (e is AggregateException ae)
                    tasksCancelled = ae.InnerExceptions.All(x => x is TaskCanceledException);

                if (!tasksCancelled)
                {
                    logger.LogCritical(e, "Encountered a fatal exception, exiting program.");
                    throw;
                }
            }
            
            logger.LogInformation("Stopping...");
            
            // Allow some time for flushing before shutdown.
            Log.CloseAndFlush();
            Thread.Sleep(1000);
        }

        private static void ValidatePools(IOptions<BotConfiguration> configuration)
        {
            var pools = configuration.Value.Pools;
            var total = pools.Sum(pool => pool.Slice.Value);
            if (Math.Abs(total - 100) > 1)
                throw new Exception("Total percentage slice of all pools should be 100.");
        }

        private static AutofacServiceProvider ConfigureServices(
            IConfiguration configuration,
            CancellationToken ct)
        {
            var services = new ServiceCollection();

            var builder = new ContainerBuilder();

            builder
                .RegisterModule(new LoggingModule(configuration, services));

            var botConfiguration = configuration
                .GetSection(BotConfiguration.Section)
                .Get<BotConfiguration>()!;
            
            services
                .ConfigureAndValidate<BotConfiguration>(configuration.GetSection(BotConfiguration.Section))
                                    
                .AddHttpClient(
                    "NodeRpc",
                    x =>
                    {
                        x.BaseAddress = new Uri(botConfiguration.NodeRpcUrl);
                        x.DefaultRequestHeaders.UserAgent.ParseAdd("chainflip-lp");
                    })
                
                .Services
                
                .AddHttpClient(
                    "LpRpc",
                    x =>
                    {
                        x.BaseAddress = new Uri(botConfiguration.LpRpcUrl);
                        x.DefaultRequestHeaders.UserAgent.ParseAdd("chainflip-lp");
                    });
            
            builder
                .Register(_ => new TelegramBotClient(botConfiguration.TelegramToken))
                .SingleInstance();
            
            builder
                .RegisterType<Runner>()
                .SingleInstance();
            
            builder
                .Populate(services);

            return new AutofacServiceProvider(builder.Build());
        }
    }
}

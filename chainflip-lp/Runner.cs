namespace ChainflipLp
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.Model;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Telegram.Bot;

    public partial class Runner
    {
        private readonly ILogger<Runner> _logger;
        private readonly BotConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramBotClient _telegramClient;

        public Runner(
            ILogger<Runner> logger,
            IOptions<BotConfiguration> options,
            IHttpClientFactory httpClientFactory,
            TelegramBotClient telegramClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = options.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        }

        public async Task Start(CancellationToken ct)
        {
            _logger.LogInformation(
                "Starting {TaskName}", nameof(Runner));

            try
            {
                if (!_configuration.EnableLp.Value)
                {
                    _logger.LogInformation("LP not enabled, stopping.");
                    return;
                }

                while (!ct.IsCancellationRequested)
                {
                    _logger.LogDebug("Creating RPC client.");
                    using var nodeClient = _httpClientFactory.CreateClient("NodeRpc");
                    using var lpClient = _httpClientFactory.CreateClient("LpRpc");

                    var account = await Account.GetAccount(
                        _logger,
                        _configuration,
                        nodeClient,
                        ct);

                    account.DisplayAccountBalances();
                    account.DisplayPoolOrders();
                    account.DisplayTotalBalances();
                    
                    await account.UpdateOrders(
                        lpClient,
                        _telegramClient,
                        ct);

                    await account.CleanupDustOrders(
                        lpClient,
                        ct);
                    
                    await account.PlaceOrders(
                        lpClient,
                        _telegramClient,
                        ct);
                    
                    // TODO: Future plans, listen for incoming swaps and JIT it

                    await WaitForNextLoop(ct);
                }
            }
            catch (AggregateException ex)
            {
                ex.Handle(e =>
                {
                    _logger.LogCritical(
                        "Encountered {ExceptionName}: {Message}",
                        e.GetType().Name,
                        e.Message);
                    return true;
                });

                throw;
            }
        }

        private async Task WaitForNextLoop(CancellationToken ct)
        {
            var delay = _configuration.QueryDelay.Value.RandomizeTime();
            _logger.LogInformation("Waiting {QueryDelay}ms for next check.", delay);
            await Task.Delay(delay, ct);
        }
    }
}
namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;

    public partial class Account
    {
        private readonly ILogger _logger;
        private readonly AccountResponse _account;
        private readonly List<PoolOrders> _poolOrders;

        private readonly OrderManager _orderManager;
        
        private readonly Dictionary<string, Dictionary<string, double>> _assetBalances;
        private readonly Dictionary<string, Dictionary<string, double>> _totalBalances;

        private Account(
            ILogger logger,
            BotConfiguration configuration,
            AccountResponse account, 
            List<PoolOrders> poolOrders)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _poolOrders = poolOrders ?? throw new ArgumentNullException(nameof(poolOrders));

            _orderManager = new OrderManager(
                logger,
                configuration);
            
            _assetBalances = CalculateAssetBalance(
                account.Result.Balances,
                poolOrders);
            
            _totalBalances = CalculateTotalBalances(
                account.Result.Balances,
                poolOrders);
        }

        public void DisplayAccountBalances()
        {
            _logger.LogInformation("Account Balance");
            _logger.LogInformation("---------------");

            foreach (var chain in _account.Result.Balances.Balances)
            foreach (var asset in chain.Value)
            {
                _logger.LogInformation(
                    "{Asset}/{Chain}: ${Balance} (${Earned} earned)",
                    asset.Key,
                    chain.Key,
                    asset.Value.ToNumeric().ToString(Constants.DollarString),
                    _account.Result.EarnedFees.Fees[chain.Key][asset.Key].ToNumeric().ToString(Constants.DollarString));
            }
        }
        
        public void DisplayTotalBalances()
        {
            _logger.LogInformation("Total Balance");
            _logger.LogInformation("-------------");

            foreach (var chain in _totalBalances)
            foreach (var asset in chain.Value)
            {
                _logger.LogInformation(
                    "{Asset}/{Chain}: ${Balance}",
                    asset.Key,
                    chain.Key,
                    asset.Value.ToString(Constants.DollarString));
            }
        }

        public void DisplayPoolOrders()
        {
            foreach (var pool in _poolOrders)
            {
                pool.DisplayBuyOrders();
                pool.DisplaySellOrders();
            }
        }
        
        public async Task CleanupDustOrders(
            HttpClient lpClient,
            CancellationToken ct) =>
            await _orderManager
                .CleanupDustOrders(
                    _poolOrders,
                    lpClient,
                    ct);

        public async Task PlaceOrders(
            HttpClient lpClient,
            TelegramBotClient telegramClient,
            CancellationToken ct) =>
            await _orderManager
                .PlaceOrders(
                    _assetBalances,
                    _account.Result.Balances,
                    lpClient,
                    telegramClient,
                    ct);
    }
}
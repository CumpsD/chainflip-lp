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
        private readonly List<PoolOrders> _ourPoolOrders;
        private readonly List<PoolOrders> _allPoolOrders;

        private readonly OrderManager _orderManager;
        
        private readonly Dictionary<string, Dictionary<string, double>> _assetBalances;
        private readonly Dictionary<string, Dictionary<string, double>> _totalBalances;

        private Account(
            ILogger logger,
            BotConfiguration configuration,
            AccountResponse account, 
            List<PoolOrders> ourPoolOrders,
            List<PoolOrders> allPoolOrders)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _ourPoolOrders = ourPoolOrders ?? throw new ArgumentNullException(nameof(ourPoolOrders));
            _allPoolOrders = allPoolOrders ?? throw new ArgumentNullException(nameof(allPoolOrders));

            _orderManager = new OrderManager(
                logger,
                configuration);
            
            _assetBalances = CalculateAssetBalance(
                account.Result.Balances,
                ourPoolOrders);
            
            _totalBalances = CalculateTotalBalances(
                account.Result.Balances,
                ourPoolOrders);
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

            double totalBalance = 0;
            foreach (var chain in _totalBalances)
            foreach (var asset in chain.Value)
            {
                totalBalance += asset.Value;
                
                _logger.LogInformation(
                    "{Asset}/{Chain}: ${Balance}",
                    asset.Key,
                    chain.Key,
                    asset.Value.ToString(Constants.DollarString));
            }
            
            _logger.LogInformation("-------------");
            _logger.LogInformation(
                "Total: ${Balance}",
                totalBalance.ToString(Constants.DollarString));
        }

        public void DisplayPoolOrders()
        {
            foreach (var pool in _ourPoolOrders)
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
                    _ourPoolOrders,
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

        public async Task UpdateOrders(
            HttpClient lpClient,
            TelegramBotClient telegramClient,
            CancellationToken ct)=>
            await _orderManager
                .UpdateOrders(
                    _ourPoolOrders,
                    _allPoolOrders,
                    lpClient,
                    telegramClient,
                    ct);
    }
}
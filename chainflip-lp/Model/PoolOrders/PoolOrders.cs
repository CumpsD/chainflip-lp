namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;

    public partial class PoolOrders
    {
        private readonly ILogger _logger;
        private readonly PoolConfiguration _pool;
        private readonly PoolOrdersResponse _poolOrders;

        public string Id => $"{_pool.Asset}/{_pool.Chain}";

        public string Chain => _pool.Chain;
        public string Asset => _pool.Asset;
        
        public IEnumerable<Order> Buys => _poolOrders.Result.LimitOrders.Buys;
        
        public IEnumerable<Order> Sells => _poolOrders.Result.LimitOrders.Sells;

        public long MinBuyTick => _pool.MinBuyTick.Value;
        public long MaxBuyTick => _pool.MaxBuyTick.Value;
        public long MinSellTick => _pool.MinSellTick.Value;
        public long MaxSellTick => _pool.MaxSellTick.Value;
        
        private PoolOrders(
            ILogger logger,
            PoolConfiguration pool,
            PoolOrdersResponse poolOrders)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _poolOrders = poolOrders ?? throw new ArgumentNullException(nameof(poolOrders));
        }
        
        public void DisplayBuyOrders() =>
            DisplayOrders(
                $"{_pool.Asset}/{_pool.Chain} Buy Orders",
                _poolOrders.Result.LimitOrders.Buys);

        public void DisplaySellOrders() =>
            DisplayOrders(
                $"{_pool.Asset}/{_pool.Chain} Sell Orders",
                _poolOrders.Result.LimitOrders.Sells);
        
        private void DisplayOrders(
            string title,
            IReadOnlyCollection<Order> orders)
        {
            if (orders.Count == 0)
                return;

            _logger.LogInformation(title);
            _logger.LogInformation(new('-', title.Length));
            foreach (var order in orders)
            {
                _logger.LogInformation(
                    "[{Id}] ${Amount}/${OriginalAmount} @ tick {Tick} // Earned Fees: ${Fees}",
                    order.Id,
                    order.Amount.ToNumeric().ToString(Constants.DollarString),
                    order.OriginalAmount.ToNumeric().ToString(Constants.DollarString),
                    order.Tick,
                    order.Fees.ToNumeric().ToString(Constants.DollarString));
            }
        }
    }
}
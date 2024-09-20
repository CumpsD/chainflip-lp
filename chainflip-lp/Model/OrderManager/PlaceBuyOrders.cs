namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;
    using xxHashSharp;

    public partial class OrderManager
    {
        private const string BuyOrderQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "lp_update_limit_order",
                "params": {
                    "base_asset": { "chain": "REPLACE_CHAIN", "asset": "REPLACE_ASSET" },
                    "quote_asset": { "chain": "Ethereum", "asset": "USDC" },
                    "side": "buy",
                    "id": "REPLACE_ID",
                    "tick": REPLACE_BUY_TICK,
                    "amount_change": {
                        "increase": "REPLACE_AMOUNT"
                    },
                    "wait_for: "InBlock"
                }
            }
            """;
        
        private async Task PlaceBuyOrders(
            IReadOnlyDictionary<string, Dictionary<string, double>> balances, 
            AccountBalances balance, 
            HttpClient client,
            ITelegramBotClient telegramClient, 
            CancellationToken cancellationToken)
        {
            var totalBalance = balances.Sum(x => x.Value.Sum(y => y.Value));
            
            // If there is USDC balance, check the pool distribution and place buy orders accordingly
            var usdcBalance = balance.Ethereum.UsdcBalance;
            if (usdcBalance.ToNumeric() > _configuration.MinimumOrderSize.Value)
            {
                // We have USDC, figure out where to place a buy order
                // Check each pool their slice compared to the total balance
                // Grab the pool with the biggest difference and throw the balance in there

                var poolToUse = _configuration.Pools[0];
                var maxPoolDifference = 0d;
                foreach (var pool in _configuration.Pools)
                {
                    var expectedPoolSlice = totalBalance / 100 * pool.Slice.Value;
                    var currentPoolSlice = balances[pool.Chain][pool.Asset];
                    var poolDifference = expectedPoolSlice - currentPoolSlice;

                    _logger.LogInformation(
                        "{Asset}/{Chain} is expected to have ${ExpectedSlice} and currently has ${CurrentSlice}, which is ${Difference} difference.",
                        pool.Asset,
                        pool.Chain,
                        expectedPoolSlice.ToString(Constants.DollarString),
                        currentPoolSlice.ToString(Constants.DollarString),
                        poolDifference.ToString(Constants.DollarString));

                    if (poolDifference <= maxPoolDifference) 
                        continue;
                    
                    maxPoolDifference = poolDifference;
                    poolToUse = pool;
                }
                
                _logger.LogWarning(
                    "Placing {Asset}/{Chain} buy order for ${Balance} USDC",
                    poolToUse.Asset,
                    poolToUse.Chain,
                    usdcBalance.ToNumeric().ToString(Constants.DollarString));

                await PlaceBuyOrder(
                    poolToUse,
                    usdcBalance,
                    client,
                    cancellationToken);

                await NotifyTelegram(
                    telegramClient,
                    $"Placed {poolToUse.Asset}/{poolToUse.Chain} buy order for {usdcBalance.ToNumeric().ToString(Constants.DollarString)} USDC",
                    cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "{Asset}/{Chain} Threshold not met, only got ${Balance}, required ${MinimumOrderSize}",
                    "USDC",
                    "Ethereum",
                    usdcBalance.ToNumeric().ToString(Constants.DollarString),
                    _configuration.MinimumOrderSize.Value.ToString(Constants.DollarString));
            }
        }
        
        private async Task PlaceBuyOrder(
            PoolConfiguration pool,
            string balance,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = BuyOrderQuery
                .Replace("REPLACE_CHAIN", pool.Chain)
                .Replace("REPLACE_ASSET", pool.Asset)
                .Replace("REPLACE_ID", GenerateAssetId(pool.Chain, pool.Asset, "buy"))
                .Replace("REPLACE_AMOUNT", balance)
                .Replace("REPLACE_BUY_TICK", pool.MinBuyTick.Value.ToString());

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "PlaceBuyOrder returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    query);

                return;
            }

            _logger.LogError(
                "PlaceBuyOrder returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);
        }
        
        private static string GenerateAssetId(string chain, string asset, string side)
        { 
            var id = $"{asset.ToLower()}-{chain.ToLower()}-{side.ToLower()}";
            var hashValue = xxHash.CalculateHash(Encoding.UTF8.GetBytes(id));
            return hashValue.ToString("x8");
        }
    }
}
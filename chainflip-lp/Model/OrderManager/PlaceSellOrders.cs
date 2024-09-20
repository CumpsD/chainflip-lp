namespace ChainflipLp.Model
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;

    public partial class OrderManager
    {
        private const string SellOrderQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "lp_update_limit_order",
                "params": {
                    "base_asset": { "chain": "REPLACE_CHAIN", "asset": "REPLACE_ASSET" },
                    "quote_asset": { "chain": "Ethereum", "asset": "USDC" },
                    "side": "sell",
                    "id": "REPLACE_ID",
                    "tick": REPLACE_SELL_TICK,
                    "amount_change": {
                        "increase": "REPLACE_AMOUNT"
                    },
                    "wait_for: "InBlock"
                }
            }
            """;
        
        private async Task PlaceSellOrders(
            AccountBalances balance,
            HttpClient client, 
            ITelegramBotClient telegramClient,
            CancellationToken cancellationToken)
        {
            // If there is any USDT balance: place a sell order for it
            // If there is any USDC.eth balance: place a sell order it
            foreach (var pool in _configuration.Pools)
            {
                var poolBalance = balance.Balances[pool.Chain][pool.Asset];
                if (poolBalance.ToNumeric() > _configuration.MinimumOrderSize.Value)
                {
                    _logger.LogWarning(
                        "Placing {Asset}/{Chain} sell order for ${Balance} {Asset}/{Chain}",
                        pool.Asset,
                        pool.Chain,
                        poolBalance.ToNumeric().ToString(Constants.DollarString),
                        pool.Asset,
                        pool.Chain);

                    await PlaceSellOrder(
                        pool,
                        poolBalance,
                        client,
                        cancellationToken);

                    await NotifyTelegram(
                        telegramClient,
                        $"Placed {pool.Asset}/{pool.Chain} sell order for {poolBalance.ToNumeric().ToString(Constants.DollarString)} {pool.Asset}/{pool.Chain}",
                        cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "{Asset}/{Chain} Threshold not met, only got ${Balance}, required ${MinimumOrderSize}",
                        pool.Asset,
                        pool.Chain,
                        poolBalance.ToNumeric().ToString(Constants.DollarString),
                        _configuration.MinimumOrderSize.Value.ToString(Constants.DollarString));
                }
            }
        }

        private async Task PlaceSellOrder(
            PoolConfiguration pool,
            string balance,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = SellOrderQuery
                .Replace("REPLACE_CHAIN", pool.Chain)
                .Replace("REPLACE_ASSET", pool.Asset)
                .Replace("REPLACE_ID", GenerateAssetId(pool.Chain, pool.Asset, "sell"))
                .Replace("REPLACE_AMOUNT", balance)
                .Replace("REPLACE_SELL_TICK", pool.MinSellTick.Value.ToString());

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "PlaceSellOrder returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    query);

                return;
            }

            _logger.LogError(
                "PlaceSellOrder returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);
        }
    }
}
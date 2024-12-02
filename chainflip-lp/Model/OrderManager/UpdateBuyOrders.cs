namespace ChainflipLp.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Infrastructure;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;

    public partial class OrderManager
    {
        private const string UpdateBuyOrderQuery =
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
                        "increase": "0x0"
                    }
                }
            }
            """;
        
        // Buys: -Infinity - 4
        private async Task UpdateBuyOrders(
            PoolOrders ourOrders,
            List<PoolOrders> allPoolOrders,
            HttpClient client,
            ITelegramBotClient telegramClient,
            CancellationToken cancellationToken)
        {
            var otherOrders = allPoolOrders
                .Where(x =>
                    x.Chain == ourOrders.Chain &&
                    x.Asset == ourOrders.Asset)
                .SelectMany(x => x.Buys)
                .Where(x => x.Amount.ToNumeric() > _configuration.AmountIgnoreLimit.Value)
                .Where(x => x.LiquidityProvider != _configuration.LpAccount)
                .Where(x => x.Tick <= ourOrders.MaxBuyTick)
                .ToList();

            var ourOrder = ourOrders.Buys.SingleOrDefault();
            if (ourOrder == null)
                return;
            
            var ourTick = ourOrder.Tick;

            if (otherOrders.Count == 0)
                return;
            
            var otherTick = otherOrders.Max(x => x.Tick);

            var newTick = otherTick + 1;
            
            if (newTick > ourOrders.MaxBuyTick)
                newTick = ourOrders.MaxBuyTick;
            
            if (newTick < ourOrders.MinBuyTick)
                newTick = ourOrders.MinBuyTick;

            if (newTick == ourTick)
                return;
            
            // Update it to otherTick + 1
            _logger.LogInformation(
                "[{Id}] {Asset}/{Chain} ${Amount}/${OriginalAmount} @ tick {OldTick} updating buy tick {NewTick}",
                ourOrder.Id,
                ourOrders.Asset,
                ourOrders.Chain,
                ourOrder.Amount.ToNumeric().ToString(Constants.DollarString),
                ourOrder.OriginalAmount.ToNumeric().ToString(Constants.DollarString),
                ourOrder.Tick,
                newTick);
            
            await UpdateBuyOrder(
                ourOrders.Chain,
                ourOrders.Asset,
                newTick,
                client,
                cancellationToken);

            if (_configuration.AnnounceTickChanges.Value)
            {
                await NotifyTelegram(
                    telegramClient,
                    $"Updated {ourOrders.Asset}/{ourOrders.Chain} buy order from tick {ourOrder.Tick} to {newTick} (${ourOrder.Amount.ToNumeric().ToString(Constants.DollarString)}/${ourOrder.OriginalAmount.ToNumeric().ToString(Constants.DollarString)})",
                    false,
                    cancellationToken);
            }
        }
        
        private async Task UpdateBuyOrder(
            string chain,
            string asset,
            long newTick,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = UpdateBuyOrderQuery
                .Replace("REPLACE_CHAIN", chain)
                .Replace("REPLACE_ASSET", asset)
                .Replace("REPLACE_ID", GenerateAssetId(chain, asset, "buy"))
                .Replace("REPLACE_BUY_TICK", newTick.ToString());

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "UpdateBuyOrder returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    query);

                return;
            }

            _logger.LogError(
                "UpdateBuyOrder returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);
        }
    }
}
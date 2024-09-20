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
        private const string UpdateSellOrderQuery =
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
                    "tick": REPLACE_SELL_TICK
                }
            }
            """;
        
        // Sells: -4 - Infinity
        private async Task UpdateSellOrders(
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
                .SelectMany(x => x.Sells)
                .Where(x => x.LiquidityProvider != _configuration.LpAccount)
                .ToList();

            var ourOrder = ourOrders.Sells.SingleOrDefault();
            if (ourOrder == null)
                return;
            
            var ourTick = ourOrder.Tick;
            var otherTick = otherOrders.Min(x => x.Tick);

            // Can't make it any better
            if (ourTick == ourOrders.MaxSellTick)
                return;

            var newTick = otherTick - 1;

            if (newTick < ourOrders.MinSellTick)
                newTick = ourOrders.MinSellTick;

            if (newTick == ourTick)
                return;
            
            // Update it to otherTick - 1
            _logger.LogInformation(
                "[{Id}] {Asset}/{Chain} ${Amount}/${OriginalAmount} @ tick {OldTick} updating tick {NewTick}",
                ourOrder.Id,
                ourOrders.Asset,
                ourOrders.Chain,
                ourOrder.Amount.ToNumeric().ToString(Constants.DollarString),
                ourOrder.OriginalAmount.ToNumeric().ToString(Constants.DollarString),
                ourOrder.Tick,
                newTick);
            
            await UpdateSellOrder(
                ourOrders.Chain,
                ourOrders.Asset,
                newTick,
                client,
                cancellationToken);
            
            await NotifyTelegram(
                telegramClient,
                $"Updated {ourOrders.Asset}/{ourOrders.Chain} sell order from tick {ourOrder.Tick} to {newTick}",
                cancellationToken);
        }
        
        private async Task UpdateSellOrder(
            string chain,
            string asset,
            long newTick,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = SellOrderQuery
                .Replace("REPLACE_CHAIN", chain)
                .Replace("REPLACE_ASSET", asset)
                .Replace("REPLACE_ID", GenerateAssetId(chain, asset, "sell"))
                .Replace("REPLACE_SELL_TICK", newTick.ToString());

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "UpdateSellOrder returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    query);

                return;
            }

            _logger.LogError(
                "UpdateSellOrder returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);
        }
    }
}
namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;

    public partial class OrderManager
    {
        private const string CloseOrderQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "lp_set_limit_order",
                "params": {
                    "base_asset": { "chain": "REPLACE_CHAIN", "asset": "REPLACE_ASSET" },
                    "quote_asset": { "chain": "Ethereum", "asset": "USDC" },
                    "side": "REPLACE_SIDE",
                    "id": "REPLACE_ID",
                    "sell_amount": "0"
                }
            }
            """;
        
        public async Task CleanupDustOrders(
            List<PoolOrders> poolOrders, 
            HttpClient lpClient, 
            CancellationToken ct)
        {
            foreach (var poolOrder in poolOrders)
            {
                await CleanupDustOrders(poolOrder, x => x.Buys, "buy", lpClient, ct);
                await CleanupDustOrders(poolOrder, x => x.Sells, "sell", lpClient, ct);
            }
        }

        private async Task CleanupDustOrders(
            PoolOrders pool, 
            Func<PoolOrders, IEnumerable<Order>> orders,
            string side,
            HttpClient lpClient,
            CancellationToken ct)
        {
            foreach (var order in orders(pool))
            {
                if (order.Amount.ToNumeric() > _configuration.DustOrderSize.Value)
                    continue;
                
                await CleanupDustOrder(
                    order, 
                    side,
                    pool.Chain,
                    pool.Asset,
                    lpClient,
                    ct);
            }
        }

        private async Task CleanupDustOrder(
            Order order,
            string side,
            string chain,
            string asset,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            _logger.LogWarning(
                "Cleaning up dust order {OrderId} for ${Balance} {Asset}/{Chain}",
                order.Id,
                order.Amount.ToNumeric().ToString(Constants.DollarString),
                asset,
                chain);
            
            var query = CloseOrderQuery
                .Replace("REPLACE_CHAIN", chain)
                .Replace("REPLACE_ASSET", asset)
                .Replace("REPLACE_ID", order.Id)
                .Replace("REPLACE_SIDE", side);

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "CleanupDustOrder returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    query);

                return;
            }

            _logger.LogError(
                "CleanupDustOrder returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);
        }
    }
}
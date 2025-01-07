namespace ChainflipLp.Model
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Net.Mime;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using ChainflipLp.Configuration;
    using ChainflipLp.RpcModel;
    using Microsoft.Extensions.Logging;

    public partial class PoolOrders
    {
        private const string OurPoolOrdersQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "cf_pool_orders",
                "params": {
                    "base_asset": { "chain": "REPLACE_CHAIN", "asset": "REPLACE_ASSET" },
                    "quote_asset": { "chain": "Ethereum", "asset": "USDC" },
                    "lp": "REPLACE_ACCOUNT"
                }
            }
            """;
        
        public static async Task<PoolOrders> GetOurPoolOrders(
            ILogger logger,
            BotConfiguration configuration,
            PoolConfiguration pool, 
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var ourPoolOrders = await GetOurPoolOrdersInternal(
                logger,
                configuration,
                pool,
                client,
                cancellationToken);
            
            if (ourPoolOrders == null)
                throw new NullReferenceException("Failed to fetch our pool orders.");

            return new PoolOrders(
                logger,
                pool,
                ourPoolOrders);
        }
        
        private static async Task<PoolOrdersResponse?> GetOurPoolOrdersInternal(
            ILogger logger,
            BotConfiguration configuration,
            PoolConfiguration pool, 
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = OurPoolOrdersQuery
                .Replace("REPLACE_CHAIN", pool.Chain)
                .Replace("REPLACE_ASSET", pool.Asset)
                .Replace("REPLACE_ACCOUNT", configuration.LpAccount);

            var response = await client.PostAsync(
                string.Empty,
                new StringContent(
                    query,
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                logger.LogDebug(
                    "GetOurPoolOrdersInternal returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    content,
                    query);
                
                return JsonSerializer.Deserialize<PoolOrdersResponse>(content);
            }

            logger.LogError(
                "GetOurPoolOrdersInternal returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);

            return null;
        }
    }
}
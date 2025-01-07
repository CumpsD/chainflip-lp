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
        private const string AllPoolOrdersQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "cf_pool_orders",
                "params": {
                    "base_asset": { "chain": "REPLACE_CHAIN", "asset": "REPLACE_ASSET" },
                    "quote_asset": { "chain": "Ethereum", "asset": "USDC" }
                }
            }
            """;
        
        public static async Task<PoolOrders> GetAllPoolOrders(
            ILogger logger,
            PoolConfiguration pool, 
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var allPoolOrders = await GetAllPoolOrdersInternal(
                logger,
                pool,
                client,
                cancellationToken);
            
            if (allPoolOrders == null)
                throw new NullReferenceException("Failed to fetch all pool orders.");

            return new PoolOrders(
                logger,
                pool,
                allPoolOrders);
        }
        
        private static async Task<PoolOrdersResponse?> GetAllPoolOrdersInternal(
            ILogger logger,
            PoolConfiguration pool, 
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = AllPoolOrdersQuery
                .Replace("REPLACE_CHAIN", pool.Chain)
                .Replace("REPLACE_ASSET", pool.Asset);

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
                    "GetAllPoolOrdersInternal returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    content,
                    query);
                
                return JsonSerializer.Deserialize<PoolOrdersResponse>(content);
            }

            logger.LogError(
                "GetAllPoolOrdersInternal returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);

            return null;
        }
    }
}
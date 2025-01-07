namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
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

    public partial class Account
    {
        private const string AccountQuery =
            """
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "cf_account_info",
                "params": {
                    "account_id": "REPLACE_ACCOUNT"
                }
            }
            """;
        
        public static async Task<Account> GetAccount(
            ILogger logger,
            BotConfiguration configuration,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var account = await GetAccountInternal(
                logger,
                configuration,
                client,
                cancellationToken);

            if (account == null)
                throw new NullReferenceException("Failed to fetch account details.");

            var ourPoolOrders = new List<PoolOrders>();
            var allPoolOrders = new List<PoolOrders>();
            foreach (var pool in configuration.Pools)
            {
                ourPoolOrders.Add(
                    await PoolOrders.GetOurPoolOrders(
                        logger,
                        configuration,
                        pool,
                        client,
                        cancellationToken));
                
                allPoolOrders.Add(
                    await PoolOrders.GetAllPoolOrders(
                        logger,
                        pool,
                        client,
                        cancellationToken));
            }

            return new Account(
                logger,
                configuration,
                account,
                ourPoolOrders,
                allPoolOrders);
        }
        
        private static async Task<AccountResponse?> GetAccountInternal(
            ILogger logger,
            BotConfiguration configuration,
            HttpClient client,
            CancellationToken cancellationToken)
        {
            var query = AccountQuery
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
                    "GetAccount returned {StatusCode}: {Content}\nRequest: {Request}",
                    response.StatusCode,
                    content,
                    query);
                
                return JsonSerializer.Deserialize<AccountResponse>(content);
            }

            logger.LogError(
                "GetAccount returned {StatusCode}: {Error}\nRequest: {Request}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                query);

            return null;
        }
    }
}
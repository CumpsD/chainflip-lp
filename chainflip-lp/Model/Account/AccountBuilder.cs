namespace ChainflipLp.Model
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Net.Mime;
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

            var allPoolOrders = new List<PoolOrders>();
            foreach (var pool in configuration.Pools)
            {
                allPoolOrders.Add(
                    await PoolOrders.GetPoolOrders(
                        logger,
                        configuration,
                        pool,
                        client,
                        cancellationToken));
            }

            return new Account(
                logger,
                configuration,
                account,
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
                // var content = await response.Content.ReadAsStringAsync(cancellationToken);
                // return JsonSerializer.Deserialize<AccountResponse>(content);
                
                return await response
                    .Content
                    .ReadFromJsonAsync<AccountResponse>(cancellationToken: cancellationToken);
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
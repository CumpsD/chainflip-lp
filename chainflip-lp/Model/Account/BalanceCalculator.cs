namespace ChainflipLp.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using ChainflipLp.Infrastructure;
    using ChainflipLp.RpcModel;

    public partial class Account
    {
        private static Dictionary<string, Dictionary<string, double>> CalculateAssetBalance(
            AccountBalances balance,
            IReadOnlyCollection<PoolOrders> poolOrders)
        {
            var balances = new Dictionary<string, Dictionary<string, double>>();
            
            foreach (var chain in balance.Balances)
            foreach (var asset in chain.Value)
            {
                var key = $"{asset.Key}/{chain.Key}";
                var orders = poolOrders.FirstOrDefault(x => x.Id == key);

                var total = orders == null
                    ? balance.Balances[chain.Key][asset.Key].ToNumeric()
                    : balance.Balances[chain.Key][asset.Key].ToNumeric() +
                      orders.Buys.Sum(order => order.Amount.ToNumeric()) +
                      orders.Sells.Sum(order => order.Amount.ToNumeric());

                if (!balances.ContainsKey(chain.Key))
                    balances.Add(chain.Key, new Dictionary<string, double>());
                
                balances[chain.Key].Add(
                    asset.Key,
                    total);
            }

            return balances;
        }
        
        private static Dictionary<string, Dictionary<string, double>> CalculateTotalBalances(
            AccountBalances balance,
            IReadOnlyCollection<PoolOrders> poolOrders)
        {
            var balances = new Dictionary<string, Dictionary<string, double>>
            {
                { "Ethereum", new Dictionary<string, double>() }
            };
            
            // All buy orders are in USDC
            balances["Ethereum"].Add(
                "USDC", 
                balance.Balances["Ethereum"]["USDC"].ToNumeric() +
                poolOrders.Sum(x => x.Buys.Sum(y => y.Amount.ToNumeric())));
            
            // Sell orders can be anything but USDC
            foreach (var chain in balance.Balances)
            foreach (var asset in chain.Value)
            {
                if (chain.Key == "Ethereum" && asset.Key == "USDC")
                    continue;

                var key = $"{asset.Key}/{chain.Key}";
                var orders = poolOrders.FirstOrDefault(x => x.Id == key);

                var total = orders == null
                    ? balance.Balances[chain.Key][asset.Key].ToNumeric()
                    : balance.Balances[chain.Key][asset.Key].ToNumeric() +
                      orders.Sells.Sum(order => order.Amount.ToNumeric());

                if (!balances.ContainsKey(chain.Key))
                    balances.Add(chain.Key, new Dictionary<string, double>());
                
                balances[chain.Key].Add(
                    asset.Key,
                    total);
            }

            return balances;
        }
    }
}
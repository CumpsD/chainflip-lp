namespace ChainflipLp.RpcModel
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class AccountResponse
    {
        [JsonPropertyName("jsonrpc")] 
        public string Version { get; set; }
        
        [JsonPropertyName("result")] 
        public AccountResult Result { get; set; }
    }

    public class AccountResult
    {
        [JsonPropertyName("balances")] 
        public AccountBalances Balances { get; set; }
        
        [JsonPropertyName("earned_fees")] 
        public AccountFees EarnedFees { get; set; }
    }

    public class AccountBalances
    {
        [JsonPropertyName("Ethereum")] 
        public EthereumBalance Ethereum { get; set; }

        [JsonPropertyName("Arbitrum")] 
        public ArbitrumBalance Arbitrum { get; set; }
        
        [JsonIgnore]
        public Dictionary<string, Dictionary<string, string>> Balances
        {
            get
            {
                var balances = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "Ethereum", new Dictionary<string, string>
                        {
                            { "USDC", Ethereum.UsdcBalance },
                            { "USDT", Ethereum.UsdtBalance },
                        }
                    },
                    {
                        "Arbitrum", new Dictionary<string, string>
                        {
                            { "USDC", Arbitrum.UsdcBalance },
                        }
                    }
                };

                return balances;
            }
        }
    }
    
    public class AccountFees
    {
        [JsonPropertyName("Ethereum")] 
        public EthereumFeesBalance Ethereum { get; set; }
        
        [JsonPropertyName("Arbitrum")] 
        public ArbitrumFeesBalance Arbitrum { get; set; }
        
        [JsonIgnore]
        public Dictionary<string, Dictionary<string, ulong>> Fees
        {
            get
            {
                var balances = new Dictionary<string, Dictionary<string, ulong>>
                {
                    {
                        "Ethereum", new Dictionary<string, ulong>
                        {
                            { "USDC", Ethereum.UsdcBalance },
                            { "USDT", Ethereum.UsdtBalance },
                        }
                    },
                    {
                        "Arbitrum", new Dictionary<string, ulong>
                        {
                            { "USDC", Arbitrum.UsdcBalance },
                        }
                    }
                };

                return balances;
            }
        }
    }

    public class EthereumBalance
    {
        [JsonPropertyName("USDC")] 
        public string UsdcBalance { get; set; }
        
        [JsonPropertyName("USDT")] 
        public string UsdtBalance { get; set; }
    }
    
    public class EthereumFeesBalance
    {
        [JsonPropertyName("USDC")] 
        public ulong UsdcBalance { get; set; }
        
        [JsonPropertyName("USDT")] 
        public ulong UsdtBalance { get; set; }
    }
    
    public class ArbitrumBalance
    {
        [JsonPropertyName("USDC")] 
        public string UsdcBalance { get; set; }
    }
    
    public class ArbitrumFeesBalance
    {
        [JsonPropertyName("USDC")] 
        public ulong UsdcBalance { get; set; }
    }
}
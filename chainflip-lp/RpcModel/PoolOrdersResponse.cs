namespace ChainflipLp.RpcModel
{
    using System.Text.Json.Serialization;

    public class PoolOrdersResponse
    {
        [JsonPropertyName("jsonrpc")] 
        public string Version { get; set; }
        
        [JsonPropertyName("result")] 
        public PoolOrders Result { get; set; }
    }

    public class PoolOrders
    {
        [JsonPropertyName("limit_orders")] 
        public LimitOrders LimitOrders { get; set; }
    }

    public class LimitOrders
    {
        [JsonPropertyName("asks")] 
        public Order[] Sells { get; set; }
        
        [JsonPropertyName("bids")] 
        public Order[] Buys { get; set; }
    }

    public class Order
    {
        [JsonPropertyName("lp")] 
        public string LiquidityProvider { get; set; }
        
        [JsonPropertyName("id")] 
        public string Id { get; set; }
        
        [JsonPropertyName("tick")] 
        public long Tick { get; set; }
        
        [JsonPropertyName("sell_amount")] 
        public string Amount { get; set; }
        
        [JsonPropertyName("fees_earned")] 
        public string Fees { get; set; }
        
        [JsonPropertyName("original_sell_amount")] 
        public string OriginalAmount { get; set; }
    }
}
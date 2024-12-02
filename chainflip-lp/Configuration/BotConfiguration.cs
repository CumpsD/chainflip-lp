namespace ChainflipLp.Configuration
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    
    public class BotConfiguration
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Convention for configuration is .Section")]
        public const string Section = "Bot";
        
        [Required]
        [NotNull]
        public bool? EnableLp
        {
            get; init;
        }  
        
        [Required]
        [NotNull]
        public int? QueryDelay
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public string? NodeRpcUrl
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public string? LpAccount
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public string? LpRpcUrl
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public double? DustOrderSize
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public double? MinimumOrderSize
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public string? TelegramToken
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public long? TelegramChannelId
        {
            get; init;
        }

        [Required]
        [NotNull]
        public bool? AnnounceTickChanges
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public int? AmountIgnoreLimit
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public PoolConfiguration[]? Pools 
        {
            get; init;
        }
    }

    public class PoolConfiguration
    {
        [Required]
        [NotNull]
        public string? Chain
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public string? Asset
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public double? Slice
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public int? MinBuyTick
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public int? MaxBuyTick
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public int? MinSellTick
        {
            get; init;
        }
        
        [Required]
        [NotNull]
        public int? MaxSellTick
        {
            get; init;
        }
    }
}
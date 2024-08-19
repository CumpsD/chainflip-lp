namespace ChainflipLp.Infrastructure
{
    using System;

    public static class RandomizeTimeExtension
    {
        public static int RandomizeTime(this int originalTimeInMs) 
            => originalTimeInMs + Random.Shared.Next(-5000, 5000);
    }
}
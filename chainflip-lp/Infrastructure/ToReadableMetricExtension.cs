namespace ChainflipLp.Infrastructure
{
    using System;
    using Humanizer;

    public static class ToReadableMetricExtension
    {
        public static string ToReadableMetric(this double value)
        {
            return value >= 1000 
                ? value.ToMetric(decimals: 2) 
                : Math.Round(value, 2).ToString("###0.00");
        }
    }
}
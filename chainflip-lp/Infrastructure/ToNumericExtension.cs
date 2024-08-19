namespace ChainflipLp.Infrastructure
{
    using System;
    using System.Globalization;

    public static class ToNumericExtension
    {
        public static double ToNumeric(this ulong amount) 
            => amount / Math.Pow(10, 6);
        
        public static double ToNumeric(this string amount) 
            => ulong.Parse(amount[2..], NumberStyles.HexNumber) / Math.Pow(10, 6);
    }
}
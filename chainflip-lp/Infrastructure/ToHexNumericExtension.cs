namespace ChainflipLp.Infrastructure
{
    using Nethereum.Hex.HexTypes;

    public static class ToHexNumericExtension
    {
        public static string ToHexNumeric(this double amount)
        {
            var nativeAmount = Nethereum.Util.UnitConversion.Convert.ToWei(amount, 6);
            return new HexBigInteger(nativeAmount).HexValue;
        }
    }
}
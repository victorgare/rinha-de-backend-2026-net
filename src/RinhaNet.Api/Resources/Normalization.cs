using System.Collections.Frozen;

namespace RinhaNet.Api.Resources
{
    public static class Normalization
    {
        public static readonly FrozenDictionary<string, int> Normalized = FrozenDictionary.Create(
            new KeyValuePair<string, int>(MaxAmount, 10000),
            new KeyValuePair<string, int>(MaxInstallments, 12),
            new KeyValuePair<string, int>(AmountVsAvgRatio, 10),
            new KeyValuePair<string, int>(MaxMinutes, 1440),
            new KeyValuePair<string, int>(MaxKm, 1000),
            new KeyValuePair<string, int>(MaxTxCount24h, 20),
            new KeyValuePair<string, int>(MaxMerchantAvgAmount, 10000)
        );

        public const string MaxAmount = "max_amount";
        public const string MaxInstallments = "max_installments";
        public const string AmountVsAvgRatio = "amount_vs_avg_ratio";
        public const string MaxMinutes = "max_minutes";
        public const string MaxKm = "max_km";
        public const string MaxTxCount24h = "max_tx_count_24h";
        public const string MaxMerchantAvgAmount = "max_merchant_avg_amount";
    }
}

using System.Collections.Frozen;

namespace RinhaNet.Api.Resources
{
    public static class MccRisk
    {
        public static readonly FrozenDictionary<string, float> Risk = FrozenDictionary.Create(
            new KeyValuePair<string, float>("5411", 0.15f),
            new KeyValuePair<string, float>("5812", 0.3f),
            new KeyValuePair<string, float>("5912", 0.2f),
            new KeyValuePair<string, float>("5944", 0.45f),
            new KeyValuePair<string, float>("7801", 0.8f),
            new KeyValuePair<string, float>("7802", 0.75f),
            new KeyValuePair<string, float>("7995", 0.85f),
            new KeyValuePair<string, float>("4511", 0.35f),
            new KeyValuePair<string, float>("5311", 0.25f),
            new KeyValuePair<string, float>("5999", 0.5f)
        );
    }
}

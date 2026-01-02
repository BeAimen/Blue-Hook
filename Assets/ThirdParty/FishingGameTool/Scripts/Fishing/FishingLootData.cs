using UnityEngine;
using System;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.Fishing.LootData
{
    public enum LootTier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    };

    public enum LootType
    {
        Fish,
        Item
    };

    [Serializable]
    public struct LootWeightRange
    {
        public float _minWeight;
        public float _maxWeight;
    }

    [CreateAssetMenu(fileName = "Fishing Loot Data", menuName = "Fishing Game Tool/New Fishing Loot")]
    public class FishingLootData : ScriptableObject
    {
        [BetterHeader("Fishing Loot Settings", 20), Space]
        public LootTier _lootTier;

        [InfoBox("Fish represents loot that imitates fish behavior, such as swimming and evading. Item represents loot that is an object or item to be retrieved, not exhibiting fish behavior.")]
        public LootType _lootType;
        [Space]
        public LootWeightRange _weightRange;
        [InfoBox("Specifies the percentage chance of catching a loot.")]
        public float _lootRarity;
        [Space]
        public string _lootName;
        [TextArea(10,40)]
        public string _lootDescription;
        public GameObject _lootPrefab;
    }
}

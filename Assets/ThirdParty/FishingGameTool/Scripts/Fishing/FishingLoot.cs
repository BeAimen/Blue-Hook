using FishingGameTool.Fishing.LootData;
using System.Collections.Generic;
using UnityEngine;

namespace FishingGameTool.Fishing.Loot
{
    [AddComponentMenu("Fishing Game Tool/Fishing Loot")]
    public class FishingLoot : MonoBehaviour
    {
        public List<FishingLootData> _fishingLoot = new List<FishingLootData>();
        

        /// <summary>
        /// Returns the list of fishing loot that can be caught.
        /// </summary>
        /// <returns>List of FishingLootData representing potential loot obtainable while fishing.</returns>
        public List<FishingLootData> GetFishingLoot() 
        { 
            return _fishingLoot; 
        }
    }
}

using HarmonyLib;
using UnityEngine;

namespace MoreShardsAndCoins;

[HarmonyPatch(typeof(LootTable), "InitLootTable")]
public class LootTable_InitLootTable_Patch
{
    static void Postfix(LootTable __instance)
    {
        var stats = __instance.GetComponent<Stats>();
        if (stats == null)
        {
            Debug.LogWarning("[MoreShardsAndCoins] No Stats component found on LootTable.");
            return;
        }

        Debug.Log($"[MoreShardsAndCoins] Enemy Level: {stats.Level}");

        if (stats.Level < 15)
        {
            Debug.Log("[MoreShardsAndCoins] Skipping loot — enemy is below level 15.");
            return;
        }

        float planarChance = Plugin.GetNormalizedChance(Plugin.PlanarShardDropChancePercent);
        float sivakChance = Plugin.GetNormalizedChance(Plugin.SivakruxDropChancePercent);

        if (Random.value < planarChance)
        {
            __instance.ActualDrops.Add(GameData.GM.PlanarShard);
            __instance.special = true;
            Debug.Log("[MoreShardsAndCoins] Added Planar Shard to drop.");
        }

        if (Random.value < sivakChance)
        {
            __instance.ActualDrops.Add(GameData.GM.Sivak);
            __instance.special = true;
            Debug.Log("[MoreShardsAndCoins] Added Sivakrux to drop.");
        }
    }
}
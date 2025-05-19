using HarmonyLib;
using UnityEngine;

namespace MoreShardsAndCoins;

[HarmonyPatch(typeof(LootTable), "InitLootTable")]
public class LootTable_InitLootTable_Patch
{
    static void Postfix(LootTable __instance)
    {
        var stats = __instance.GetComponent<Stats>();
        if (stats == null || stats.Level < 16)
            return;

        float planarChance = Plugin.GetNormalizedChance(Plugin.PlanarShardDropChancePercent);
        float sivakChance = Plugin.GetNormalizedChance(Plugin.SivakruxDropChancePercent);

        // Add Planar Shard
        if (!__instance.ActualDrops.Contains(GameData.GM.PlanarShard) && Random.value < planarChance)
        {
            __instance.ActualDrops.Add(GameData.GM.PlanarShard);
            __instance.special = true;
        }

        // Add Sivakrux
        if (!__instance.ActualDrops.Contains(GameData.GM.Sivak) && Random.value < sivakChance)
        {
            __instance.ActualDrops.Add(GameData.GM.Sivak);
            __instance.special = true;
        }
    }
}
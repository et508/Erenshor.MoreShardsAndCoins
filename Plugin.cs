using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MoreShardsAndCoins;

[BepInPlugin("et508.erenshor.moreshardsandcoins", "More Shards and Coins", "1.0.1")]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<float> PlanarShardDropChancePercent;
    public static ConfigEntry<float> SivakruxDropChancePercent;

    private void Awake()
    {
        PlanarShardDropChancePercent = Config.Bind(
            "Drop Chance",
            "PlanarShardDropChancePercent",
            0.1f, // Default: 0.1%
            new ConfigDescription(
                "Chance to drop Planar Stone Shards (0.0–100.0%). Default: 0.1. Reload scene or wait for new respawns for changes to apply.",
                new AcceptableValueRange<float>(0f, 100f)
            ));

        SivakruxDropChancePercent = Config.Bind(
            "Drop Chance",
            "SivakruxDropChancePercent",
            0.01f, // Default: 0.01%
            new ConfigDescription(
                "Chance to drop Sivakrux (0.0–100.0%). Default: 0.01. Reload scene or wait for new respawns for changes to apply.",
                new AcceptableValueRange<float>(0f, 100f)
            ));

        Harmony harmony = new Harmony("et508.erenshor.moreshardsandcoins");
        harmony.PatchAll();
        Logger.LogInfo("More Shards and Coins loaded.");
    }

    public static float GetNormalizedChance(ConfigEntry<float> entry)
    {
        return Mathf.Clamp01(entry.Value / 100f);
    }
}
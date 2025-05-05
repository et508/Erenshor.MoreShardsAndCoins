using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MoreShardsAndCoins;

[BepInPlugin("et508.erenshor.moreshardsandcoins", "More Shards and Coins", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<int> PlanarShardDropChancePercent;
    public static ConfigEntry<int> SivakruxDropChancePercent;

    private void Awake()
    {
        PlanarShardDropChancePercent = Config.Bind(
            "Drop Chance",
            "PlanarShardDropChancePercent",
            100,
            new ConfigDescription("Chance to drop Planar Stone Shards (0-100%).",
                new AcceptableValueRange<int>(0, 100)));

        SivakruxDropChancePercent = Config.Bind(
            "Drop Chance",
            "SivakruxDropChancePercent",
            100,
            new ConfigDescription("Chance to drop Sivakrux (0-100%).",
                new AcceptableValueRange<int>(0, 100)));

        Harmony harmony = new Harmony("et508.erenshor.moreshardsandcoins");
        harmony.PatchAll();
        Logger.LogInfo("More Shards and Coins loaded.");
    }

    public static float GetNormalizedChance(ConfigEntry<int> entry)
    {
        return Mathf.Clamp01(entry.Value / 100f);
    }
}
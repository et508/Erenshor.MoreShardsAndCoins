using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreShardsAndCoins
{
    [HarmonyPatch(typeof(LootTable), "InitLootTable")]
    public static class LootTable_InitLootTable_ShardsTranspiler
    {
        // Both Sivak & PlanarShard branches use 0.001f literal in your snippet
        private const float TargetLiteral = 0.001f;
        // Use a tiny epsilon instead of Mathf.Approximately to avoid Unity dependency in transpiler
        private const float Eps = 1e-7f;

        private static readonly MethodInfo MI_ConfigPlanar =
            AccessTools.Method(typeof(LootTable_InitLootTable_ShardsTranspiler), nameof(ConfigPlanarChance));
        private static readonly MethodInfo MI_ConfigSivak =
            AccessTools.Method(typeof(LootTable_InitLootTable_ShardsTranspiler), nameof(ConfigSivakruxChance));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (MI_ConfigPlanar == null || MI_ConfigSivak == null)
            {
                Plugin.Log.LogError("[MSC] Transpiler: Could not reflect config methods; aborting.");
                return instructions;
            }

            var src = new List<CodeInstruction>(instructions);
            var dst = new List<CodeInstruction>(src.Count);

            bool sivakReplaced  = false;
            bool planarReplaced = false;

            for (int i = 0; i < src.Count; i++)
            {
                var ci = src[i] ?? new CodeInstruction(OpCodes.Nop);

                // Look for ldc.r4 <~0.001f>
                if (IsLdcR4(ci, TargetLiteral))
                {
                    // We expect a single "load-like" op then a MUL (i.e., "<const> * serverLootRate")
                    // In your build, serverLootRate is a local => ldloc.* then mul
                    // But be flexible: accept any "load-like", then a MUL.
                    if (i + 2 < src.Count && IsLoadLike(src[i + 1]) && src[i + 2]?.opcode == OpCodes.Mul)
                    {
                        // Identify which drop block this 0.001f threshold controls
                        var kind = PeekAheadForItemKind(src, i, 40);

                        if (kind == ItemKind.Sivak && !sivakReplaced)
                        {
                            // Replace literal + skip "* serverLootRate"
                            dst.Add(new CodeInstruction(OpCodes.Call, MI_ConfigSivak));
                            i += 2; // skip the load-like and mul
                            sivakReplaced = true;
                            continue;
                        }
                        else if (kind == ItemKind.PlanarShard && !planarReplaced)
                        {
                            dst.Add(new CodeInstruction(OpCodes.Call, MI_ConfigPlanar));
                            i += 2;
                            planarReplaced = true;
                            continue;
                        }
                        else
                        {
                            // This is likely the DemoBuild 0.001f or an unrelated branch—leave as-is
                        }
                    }
                }

                // Default passthrough
                dst.Add(ci);
            }

            if (!sivakReplaced)
                Plugin.Log.LogWarning("[MSC] Transpiler: Sivakrux threshold not replaced (0.001f * serverLootRate tied to GM.Sivak).");
            if (!planarReplaced)
                Plugin.Log.LogWarning("[MSC] Transpiler: Planar Shard threshold not replaced (0.001f * serverLootRate tied to GM.PlanarShard).");

            return dst;
        }

        // === Helpers ===

        private enum ItemKind { Unknown, Sivak, PlanarShard }

        private static ItemKind PeekAheadForItemKind(List<CodeInstruction> src, int startIndex, int maxLookahead)
        {
            int end = Math.Min(src.Count - 1, startIndex + Math.Max(0, maxLookahead));
            for (int k = startIndex; k <= end; k++)
            {
                var instr = src[k];
                if (instr == null) continue;

                // Field references (e.g., ... GM.Sivak / GM.PlanarShard)
                if (instr.operand is FieldInfo fi && fi != null)
                {
                    string n = fi.Name;
                    if (n == "Sivak")       return ItemKind.Sivak;
                    if (n == "PlanarShard") return ItemKind.PlanarShard;
                }

                // Property getters (e.g., callvirt get_Sivak / get_PlanarShard)
                if (instr.operand is MethodInfo mi && mi != null)
                {
                    string n = mi.Name;
                    if (n == "get_Sivak")        return ItemKind.Sivak;
                    if (n == "get_PlanarShard")  return ItemKind.PlanarShard;
                }
            }
            return ItemKind.Unknown;
        }

        private static bool IsLdcR4(CodeInstruction ci, float target)
        {
            if (ci == null) return false;
            if (ci.opcode != OpCodes.Ldc_R4) return false;
            if (ci.operand == null) return false;
            if (!(ci.operand is float f)) return false;
            return Math.Abs(f - target) <= Eps;
        }

        private static bool IsLoadLike(CodeInstruction ci)
        {
            if (ci == null) return false;
            var op = ci.opcode;

            // Any ldloc.* (local), ldfld/ldsfld (field), call/callvirt (getter), ldarg.* (arg load), dup (rare but harmless)
            return op == OpCodes.Ldloc || op == OpCodes.Ldloc_S || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1
                || op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3
                || op == OpCodes.Ldsfld || op == OpCodes.Ldfld
                || op == OpCodes.Ldarg || op == OpCodes.Ldarg_S || op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg_1
                || op == OpCodes.Ldarg_2 || op == OpCodes.Ldarg_3
                || op == OpCodes.Call || op == OpCodes.Callvirt
                || op == OpCodes.Dup;
        }

        // === Config-backed thresholds ===

        public static float ConfigPlanarChance()
        {
            try
            {
                float v = Plugin.GetNormalizedChance(Plugin.PlanarShardDropChancePercent);
                return Clamp01(v);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[MSC] ConfigPlanarChance error: {ex}");
                return TargetLiteral; // fallback to original literal
            }
        }

        public static float ConfigSivakruxChance()
        {
            try
            {
                float v = Plugin.GetNormalizedChance(Plugin.SivakruxDropChancePercent);
                return Clamp01(v);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[MSC] ConfigSivakruxChance error: {ex}");
                return TargetLiteral;
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}

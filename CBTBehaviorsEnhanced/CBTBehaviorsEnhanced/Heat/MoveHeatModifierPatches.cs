using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat
{
    [HarmonyPatch(typeof(Mech), "SprintHeat", MethodType.Getter)]
    public static class Mech_SprintHeat_Getter
    {
        public static bool Prepare()
        {
            return Mod.Config.Heat.EnableHeatMovementMods;
        }

        public static void Postfix(Mech __instance, ref int __result)
        {
            var sprintHeatMod = __instance.StatCollection.GetValue<float>(ModStats.SprintHeatMod);
            var sprintHeatMult = __instance.StatCollection.GetValue<float>(ModStats.SprintHeatMult);
            var tempResult = Mathf.RoundToInt((__result + sprintHeatMod) * sprintHeatMult);
            Mod.HeatLog.Trace?.Write($"Mech_SprintHeat_Getter - {CombatantUtils.Label(__instance)} processing sprint heat modifiers: (original {__result} + mod {sprintHeatMod}) x mult {sprintHeatMult} = {tempResult}");
            __result = tempResult;
        }
    }

    [HarmonyPatch(typeof(Mech), "WalkHeat", MethodType.Getter)]
    public static class Mech_WalkHeat_Getter
    {
        public static bool Prepare()
        {
            return Mod.Config.Heat.EnableHeatMovementMods;
        }

        public static void Postfix(Mech __instance, ref int __result)
        {
            var walkHeatMod = __instance.StatCollection.GetValue<float>(ModStats.WalkHeatMod);
            var walkHeatMult = __instance.StatCollection.GetValue<float>(ModStats.WalkHeatMult);
            var tempResult = Mathf.RoundToInt((__result + walkHeatMod) * walkHeatMult);
            Mod.HeatLog.Trace?.Write($"Mech_WalkHeat_Getter - {CombatantUtils.Label(__instance)} processing walk heat modifiers: (original {__result} + mod {walkHeatMod}) x mult {walkHeatMult} = {tempResult}");
            __result = tempResult;
        }
    }
}

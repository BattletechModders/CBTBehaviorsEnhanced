using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using System;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    // Mitigate DFA self damage based upon piloting skill
    [HarmonyPatch(typeof(Mech), "TakeWeaponDamage")]
    public static class Mech_TakeWeaponDamage {
        public static void Prefix(Mech __instance, ref float damageAmount, DamageType damageType) {
            // 	public override void TakeWeaponDamage(WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType)
            if (damageType == DamageType.DFASelf) {
                float damageReduction = 1.0f - __instance.PilotCheckMod(Mod.Config.Piloting.DFAReductionMulti);
                float reducedDamage = (float)Math.Max(0f, Math.Floor(damageReduction * damageAmount));
                Mod.Log.Debug($" Reducing DFA damage for actor: {CombatantUtils.Label(__instance)} from original damage: {damageAmount} by {damageReduction:P1} to {reducedDamage}");
                damageAmount = reducedDamage;
            }
        }
    }

    // Forces your startup sequence to only sink your capacity, not the ratio HBS uses
    [HarmonyPatch(typeof(Mech), "ApplyStartupHeatSinks")]
    public static class Mech_ApplyStartupHeatSinks {
        public static bool Prefix(Mech __instance, int stackID) {
            Mod.Log.Trace("M:ASHS - entered.");
            __instance.ApplyHeatSinks(stackID);
            return false;
        }
    }

}

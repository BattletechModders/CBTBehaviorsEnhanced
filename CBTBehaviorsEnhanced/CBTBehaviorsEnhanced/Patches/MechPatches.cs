using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    // Initialize statistics. InitEffectStats is invoked in the middle of the InitStats function, before effects are applied.
    [HarmonyPatch(typeof(Mech), "InitEffectStats")]
    [HarmonyAfter("MechEngineer.Features.Engine")]
    public static class Mech_InitEffectStats {
        public static void Postfix(Mech __instance) {
            Mod.Log.Trace("M:I entered.");

            // Initialize mod-specific statistics
            __instance.StatCollection.AddStatistic<int>(ModStats.MovementPenalty, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.FiringPenalty, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.ActuatorDamageMalus, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.RunMultiMod, 0f);
            __instance.StatCollection.AddStatistic<bool>(ModStats.HullBreachImmunity, true);

            // Override the heat and shutdown levels
            List<int> sortedKeys = Mod.Config.Heat.Shutdown.Keys.ToList().OrderBy(x => x).ToList();
            int overheatThreshold = sortedKeys.First();
            int maxHeat = sortedKeys.Last();
            Mod.Log.Info($"Setting overheat threshold to {overheatThreshold} and maxHeat to {maxHeat} for actor:{CombatantUtils.Label(__instance)}");
            __instance.StatCollection.Set<int>(ModStats.MaxHeat, maxHeat);
            __instance.StatCollection.Set<int>(ModStats.OverHeatLevel, overheatThreshold);

            // Disable default heat penalties
            __instance.StatCollection.Set<bool>(ModStats.IgnoreHeatToHitPenalties, false);
            __instance.StatCollection.Set<bool>(ModStats.IgnoreHeatMovementPenalties, false);
        }
    }

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
            Mod.Log.Debug($" Actor: {CombatantUtils.Label(__instance)} sinking {__instance.AdjustedHeatsinkCapacity} at startup.");
            __instance.ApplyHeatSinks(stackID);
            return false;
        }
    }

    // Deliberately skip to prevent any structure damage
    [HarmonyPatch(typeof(Mech), "CheckForHeatDamage")]
    public static class Mech_CheckForHeatDamage {
        static bool Prefix(Mech __instance, int stackID, string attackerID) {
            return false;
        }
    }

    // Log the current heat at the end of actrivation
    [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
    public static class Mech_OnActivationEnd {
        private static void Prefix(Mech __instance, string sourceID, int stackItemID) {

            Mod.Log.Debug($"Actor: {__instance.DisplayName}_{__instance.GetPilot().Name} has currentHeat: {__instance.CurrentHeat}" +
                $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");
        }
    }

    /* Override walk speed. Reference also: 
     *   MechEngineer.Features.ShutdownInjuryProtection
     *   MechEngineer.Features.Engine
     */
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxWalkDistance", MethodType.Getter)]
    public static class Mech_MaxWalkDistance_Get {
        public static void Postfix(Mech __instance, ref float __result) {
            Mod.Log.Trace("M:MWD:GET entered.");
            __result = MechHelper.FinalWalkSpeed(__instance);
        }
    }

    // Override walk speed (see above)
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxBackwardDistance", MethodType.Getter)]
    public static class Mech_MaxBackwardDistance_Get {
        public static void Postfix(Mech __instance, ref float __result) {
            Mod.Log.Trace("M:MBD:GET entered.");
            __result = MechHelper.FinalWalkSpeed(__instance);
        }
    }

    // Override run speed to 1.5 x walking semantics
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxSprintDistance", MethodType.Getter)]
    public static class Mech_MaxSprintDistance_Get {
        public static void Postfix(Mech __instance, ref float __result) {
            Mod.Log.Trace("M:MSD:GET entered.");
            __result = MechHelper.FinalRunSpeed(__instance);

            //This is an easy place to put this where it will always be checked. This is the key to full non-interleaved combat.
            if (__instance.Combat.TurnDirector.IsInterleaved) {
                __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
            } else {
                __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, false);
            }
        }
    }

    // Override run speed to 1.5 x walking semantics
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxMeleeEngageRangeDistance", MethodType.Getter)]
    public static class Mech_MaxMeleeEngageRangeDistance_Get {
        public static void Postfix(Mech __instance, ref float __result) {
            Mod.Log.Trace("M:MMERD:GET entered.");
            // TODO: Should this be Run or Walk speed?
            __result = MechHelper.FinalRunSpeed(__instance);
        }
    }

    // TODO: Memoize this; its invoked multiple times
    // Apply an attack modifier for shooting when overheated
    [HarmonyPatch(typeof(ToHit), "GetHeatModifier")]
    public static class ToHit_GetHeatModifier {
        public static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker) {
            Mod.Log.Trace("TH:GHM entered.");
            if (attacker is Mech mech && mech.IsOverheated) {

                float penalty = 0f;
                foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                    if (mech.CurrentHeat >= kvp.Key) {
                        penalty = kvp.Value;
                    }
                }

                Mod.Log.Trace($"  AttackPenalty: {penalty:+0;-#} from heat: {mech.CurrentHeat} for actor: {CombatantUtils.Label(attacker)}");
                __result = penalty;
            }
        }
    }

}

using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CustAmmoCategories;
using Harmony;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches
{

    // Initialize statistics. InitEffectStats is invoked in the middle of the InitStats function, before effects are applied.
    [HarmonyPatch(typeof(Mech), "InitEffectStats")]
    [HarmonyAfter("MechEngineer.Features.Engine")]
    public static class Mech_InitEffectStats
    {

        public static void Postfix(Mech __instance)
        {
            Mod.Log.Trace?.Write("M:I entered.");

            // Initialize mod-specific statistics
            __instance.StatCollection.AddStatistic<int>(ModStats.MovementPenalty, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.FiringPenalty, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.ActuatorDamageMalus, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.RunMultiMod, 0f);
            __instance.StatCollection.AddStatistic<bool>(ModStats.HullBreachImmunity, true);

            // Setup melee stats
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackMod, 0);

            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackerDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeAttackerDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackerInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeAttackerInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackMod, 0);

            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackerDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveAttackerDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackerInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveAttackerInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.KickAttackMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickExtraHitsCount, 0f);

            __instance.StatCollection.AddStatistic<int>(ModStats.KickTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.KickTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.PunchAttackMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchExtraHitsCount, 0f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamage, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.PunchTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamageMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetInstability, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.PunchTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<bool>(ModStats.PunchIsPhysicalWeapon, false);
            __instance.StatCollection.AddStatistic<string>(ModStats.PhysicalWeaponLocationTable, "");
            __instance.StatCollection.AddStatistic<int>(ModStats.PhysicalWeaponAttackMod, 0);

            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponExtraHitsCount, 0f);

            // Don't initialize these so their presence can signify the choice
            //__instance.StatCollection.AddStatistic<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnHit, false);
            //__instance.StatCollection.AddStatistic<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnMiss, false);
            //__instance.StatCollection.AddStatistic<bool>(ModStats.PhysicalWeaponUnsteadyTargetOnHit, false);

            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetDamage, 0f);
            __instance.StatCollection.AddStatistic<int>(ModStats.PhysicalWeaponTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetDamageMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetInstability, 0f);
            __instance.StatCollection.AddStatistic<int>(ModStats.PhysicalWeaponTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetInstabilityMulti, 1f);

            // Override the heat and shutdown levels
            List<int> sortedKeys = Mod.Config.Heat.Shutdown.Keys.ToList().OrderBy(x => x).ToList();
            int overheatThreshold = sortedKeys.First();
            int maxHeat = sortedKeys.Last();
            Mod.Log.Info?.Write($"Setting overheat threshold to {overheatThreshold} and maxHeat to {maxHeat} for actor:{CombatantUtils.Label(__instance)}");
            __instance.StatCollection.Set<int>(ModStats.MaxHeat, maxHeat);
            __instance.StatCollection.Set<int>(ModStats.OverHeatLevel, overheatThreshold);

            // Disable default heat penalties
            __instance.StatCollection.Set<bool>(ModStats.IgnoreHeatToHitPenalties, false);
            __instance.StatCollection.Set<bool>(ModStats.IgnoreHeatMovementPenalties, false);

        }
    }

    // TODO: This is redundant since we've replaced DFASelfDamage, I think? Just remove?
    // Mitigate DFA self damage based upon piloting skill
    [HarmonyPatch(typeof(Mech), "TakeWeaponDamage")]
    public static class Mech_TakeWeaponDamage
    {
        public static void Prefix(Mech __instance, ref float damageAmount, DamageType damageType)
        {
            // 	public override void TakeWeaponDamage(WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType)
            if (damageType == DamageType.DFASelf)
            {
                float damageReduction = 1.0f - __instance.PilotCheckMod(Mod.Config.Piloting.DFAReductionMulti);
                float reducedDamage = (float)Math.Max(0f, Math.Floor(damageReduction * damageAmount));
                Mod.Log.Debug?.Write($" Reducing DFA damage for actor: {CombatantUtils.Label(__instance)} from original damage: {damageAmount} by {damageReduction:P1} to {reducedDamage}");
                damageAmount = reducedDamage;
            }
        }
    }

    // Forces your startup sequence to only sink your capacity, not the ratio HBS uses
    [HarmonyPatch(typeof(Mech), "ApplyStartupHeatSinks")]
    public static class Mech_ApplyStartupHeatSinks
    {
        public static bool Prefix(Mech __instance, int stackID)
        {
            Mod.Log.Trace?.Write("M:ASHS - entered.");
            Mod.Log.Debug?.Write($" Actor: {CombatantUtils.Label(__instance)} sinking {__instance.AdjustedHeatsinkCapacity} at startup.");
            __instance.ApplyHeatSinks(stackID);
            return false;
        }
    }

    // Deliberately skip to prevent any structure damage
    [HarmonyPatch(typeof(Mech), "CheckForHeatDamage")]
    public static class Mech_CheckForHeatDamage
    {
        static bool Prefix(Mech __instance, int stackID, string attackerID)
        {
            return false;
        }
    }

    // Log the current heat at the end of actrivation
    [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
    static class Mech_OnActivationEnd
    {
        [HarmonyAfter("us.frostraptor.SkillBasedInit")]
        static void Prefix(Mech __instance)
        {

            Mod.HeatLog.Debug?.Write($"ON_ACTIVATION_END:PRE - Actor: {__instance.DistinctId()} has currentHeat: {__instance.CurrentHeat}" +
                $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");

            // Invalidate any melee state the actor may have set
            ModState.MeleeStates = null;
        }

        [HarmonyAfter("io.mission.modrepuation", "us.frostraptor.SkillBasedInit")]
        static void Postfix(Mech __instance)
        {
            Mod.HeatLog.Debug?.Write($"ON_ACTIVATION_END:POST - Actor: {__instance.DistinctId()} has currentHeat: {__instance.CurrentHeat}" +
                $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");

            if (__instance.UsedHeatSinksCap() != 0)
            {
                Mod.HeatLog.Warn?.Write("MECH ACTIVATION COMPLETE, BUT HEAT SINKS REMAIN USED! FORCE-CLEARING HS.");
                __instance.clearUsedHeatSinksCap();
            }
        }
    }

    /* Override walk speed. Reference also: 
     *   MechEngineer.Features.ShutdownInjuryProtection
     *   MechEngineer.Features.Engine
     */
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxWalkDistance", MethodType.Getter)]
    public static class Mech_MaxWalkDistance_Get
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("M:MWD:GET entered.");
            __result = MechHelper.FinalWalkSpeed(__instance);
        }
    }

    // Override walk speed (see above)
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxBackwardDistance", MethodType.Getter)]
    public static class Mech_MaxBackwardDistance_Get
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("M:MBD:GET entered.");
            __result = MechHelper.FinalWalkSpeed(__instance);
        }
    }

    // Override run speed to 1.5 x walking semantics
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxSprintDistance", MethodType.Getter)]
    public static class Mech_MaxSprintDistance_Get
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("M:MSD:GET entered.");
            __result = MechHelper.FinalRunSpeed(__instance);

            //This is an easy place to put this where it will always be checked. This is the key to full non-interleaved combat.
            if (__instance.Combat.TurnDirector.IsInterleaved)
            {
                __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
            }
            else
            {
                __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, false);
            }
        }
    }

    // Override run speed to 1.5 x walking semantics
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxMeleeEngageRangeDistance", MethodType.Getter)]
    public static class Mech_MaxMeleeEngageRangeDistance_Get
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("M:MMERD:GET entered.");
            // TODO: Should this be Run or Walk speed?
            __result = MechHelper.FinalRunSpeed(__instance);
        }
    }



}

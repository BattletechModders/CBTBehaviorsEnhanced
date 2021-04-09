using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CustAmmoCategories;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

            // --- CHARGE STATS ---
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackMod, 0);

            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackerDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeAttackerDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeAttackerInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeAttackerInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.ChargeTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetDamageReductionMulti, 1f);
            __instance.StatCollection.AddStatistic<float>(ModStats.ChargeTargetInstabReductionMulti, 1f);

            // --- DFA STATS ---
            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackMod, 0);

            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackerDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveAttackerDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveAttackerInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveAttackerInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.DeathFromAboveTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetDamageReductionMulti, 1f);
            __instance.StatCollection.AddStatistic<float>(ModStats.DeathFromAboveTargetInstabReductionMulti, 1f);

            // --- KICK STATS ---
            __instance.StatCollection.AddStatistic<int>(ModStats.KickAttackMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickExtraHitsCount, 0f);

            __instance.StatCollection.AddStatistic<int>(ModStats.KickTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetDamageMulti, 1f);
            __instance.StatCollection.AddStatistic<int>(ModStats.KickTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetDamageReductionMulti, 1f);
            __instance.StatCollection.AddStatistic<float>(ModStats.KickTargetInstabReductionMulti, 1f);

            // --- PUNCH STATS ---
            __instance.StatCollection.AddStatistic<int>(ModStats.PunchAttackMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchExtraHitsCount, 0f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamage, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.PunchTargetDamageMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamageMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetInstability, 0);
            __instance.StatCollection.AddStatistic<int>(ModStats.PunchTargetInstabilityMod, 0);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetInstabilityMulti, 1f);

            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamageReductionMulti, 1f);
            __instance.StatCollection.AddStatistic<float>(ModStats.PunchTargetInstabReductionMulti, 1f);

            // --- PHYSICAL WEAPON STATS ---
            __instance.StatCollection.AddStatistic<bool>(ModStats.PunchIsPhysicalWeapon, false);
            __instance.StatCollection.AddStatistic<bool>(ModStats.PhysicalWeaponIgnoreActuators, false);

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

            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetDamageReductionMulti, 1f);
            __instance.StatCollection.AddStatistic<float>(ModStats.PhysicalWeaponTargetInstabReductionMulti, 1f);

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
            Mod.HeatLog.Info?.Write($"AT END OF TURN: ACTOR: {__instance.DistinctId()} has currentHeat: {__instance.CurrentHeat}" +
                $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");

            // Invalidate any melee state the actor may have set
            ModState.MeleeStates = null;

            // Make the checks for ammo explosions, etc
            float heatCheck = __instance.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);
            float pilotCheck = __instance.PilotCheckMod(Mod.Config.SkillChecks.ModPerPointOfPiloting);
            Mod.HeatLog.Debug?.Write($" Actor: {__instance.DistinctId()} has heatCheckMod: {heatCheck}  pilotingCheckMod: {pilotCheck}");

            MultiSequence sequence = new MultiSequence(__instance.Combat);
            bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(__instance, __instance.CurrentHeat, sequence.RootSequenceGUID, sequence.SequenceGUID, heatCheck);
            bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(__instance, __instance.CurrentHeat, sequence.SequenceGUID, heatCheck);
            bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(__instance, __instance.CurrentHeat, sequence.SequenceGUID, heatCheck);
            bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(__instance, __instance.CurrentHeat, sequence.SequenceGUID, heatCheck);
            Mod.HeatLog.Info?.Write($"  failedInjuryCheck: {failedInjuryCheck}  failedSystemFailureCheck: {failedSystemFailureCheck}  " +
                $"failedAmmoCheck: {failedAmmoCheck}  failedVolatileAmmoCheck: {failedVolatileAmmoCheck}");

            bool failedShutdownCheck = false;
            if (!__instance.IsShutDown)
            {
                // Resolve Shutdown + Fall
                failedShutdownCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, __instance.CurrentHeat, __instance, heatCheck, ModText.FT_Check_Shutdown);
                Mod.HeatLog.Info?.Write($"  failedShutdownCheck: {failedShutdownCheck}");
                if (failedShutdownCheck)
                {
                    Mod.HeatLog.Info?.Write($"-- Shutdown check failed for unit {CombatantUtils.Label(__instance)}, forcing unit to shutdown");

                    string debuffText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Shutdown_Failed_Overide]).ToString();
                    sequence.AddChildSequence(new ShowActorInfoSequence(__instance, debuffText,
                        FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                    MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance);
                    //{
                    //    RootSequenceGUID = __instance.SequenceGUID
                    //};
                    sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                    if (__instance.IsOrWillBeProne)
                    {
                        bool failedFallingCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.ShutdownFallThreshold, __instance, pilotCheck, ModText.FT_Check_Fall);
                        Mod.HeatLog.Debug?.Write($"  failedFallingCheck: {failedFallingCheck}");
                        if (failedFallingCheck)
                        {
                            Mod.HeatLog.Info?.Write("   Pilot check from shutdown failed! Forcing a fall!");
                            string fallDebuffText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Shutdown_Fall]).ToString();
                            sequence.AddChildSequence(new ShowActorInfoSequence(__instance, fallDebuffText,
                                FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                            MechFallSequence mfs = new MechFallSequence(__instance, "Overheat", new Vector2(0f, -1f));
                            //{
                            //    RootSequenceGUID = __instance.SequenceGUID
                            //};
                            sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                        }
                        else
                        {
                            Mod.HeatLog.Info?.Write($"Pilot check to avoid falling passed. Applying unstead to unit.");
                            __instance.ApplyUnsteady();
                        }
                    }
                    else
                    {
                        Mod.HeatLog.Debug?.Write("Unit is already prone, skipping.");
                    }
                }
            }
            else
            {
                Mod.HeatLog.Debug?.Write("Unit is already shutdown, skipping.");
            }

            if (failedInjuryCheck || failedSystemFailureCheck || failedAmmoCheck || failedVolatileAmmoCheck || failedShutdownCheck)
            {

                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
            }

        }

        [HarmonyAfter("io.mission.modrepuation", "us.frostraptor.SkillBasedInit")]
        static void Postfix(Mech __instance)
        {
            Mod.HeatLog.Debug?.Write($"ON_ACTIVATION_END:POST - Actor: {__instance.DistinctId()} has currentHeat: {__instance.CurrentHeat}" +
                $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");
        }
    }

    // Override max engage distance to be sprinting
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("MaxMeleeEngageRangeDistance", MethodType.Getter)]
    public static class Mech_MaxMeleeEngageRangeDistance_Get
    {
        public static void Postfix(Mech __instance, ref float __result)
        {
            if (SharedState.Combat != null)
                __result = __instance.ModifiedRunDistance();
        }
    }

    // Override max engage distance to be sprinting
    [HarmonyPatch(typeof(Mech), "DamageLocation")]
    [HarmonyPriority(Priority.Last)]
    public static class Mech_DamageLocation
    {
        public static void Prefix(Mech __instance)
        {
            // Invalidate any held state on damage
            ModState.InvalidateState(__instance);
        }
    }

}

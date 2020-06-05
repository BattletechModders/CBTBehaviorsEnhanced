using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using FluffyUnderware.DevTools.Extensions;
using Harmony;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee {


    [HarmonyPatch(typeof(MechMeleeSequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_BuildMeleeDirectorSequence
    {
        static void Prefix(MechMeleeSequence __instance)
        {
            // TODO: If this happens before the above... need to grab the selected melee type from state
            Mod.Log.Info($"Setting current melee type to: {__instance.selectedMeleeType} and weapon to: {__instance.OwningMech.MeleeWeapon}");
            ModState.MeleeWeapon = __instance.OwningMech.MeleeWeapon;
            ModState.MeleeType = __instance.selectedMeleeType;

            if (ModState.MeleeStates?.SelectedState != null)
            {
                ModState.MeleeType = ModState.MeleeStates.SelectedState.AttackAnimation;

                // Modify the owning mech melee weapon to do the 'first' hit
                float targetDamage = ModState.MeleeStates.SelectedState?.TargetDamageClusters?.Length > 0 ?
                    ModState.MeleeStates.SelectedState.TargetDamageClusters[0] : 0;
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, ModState.MeleeStates.SelectedState.TargetInstability);
                Mod.Log.Info($"For {CombatantUtils.Label(__instance.OwningMech)} set melee weapon damage: {targetDamage}  and instability: {ModState.MeleeStates.SelectedState.TargetInstability}");
            }
        }

        static void Postfix(MechMeleeSequence __instance)
        {
            ModState.MeleeType = MeleeAttackType.NotSet;
            ModState.MeleeWeapon = null;
        }
    }


    [HarmonyPatch(typeof(MechMeleeSequence), "OnMeleeComplete")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_OnMeleeComplete
    {
        static void Prefix(MechMeleeSequence __instance, MessageCenterMessage message, AttackStackSequence ___meleeSequence)
        {
            Mod.Log.Trace("MMS:OMC entered.");

            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            Mod.Log.Info($"== Resolving cluster damage, instability, and unsteady on melee attacker: {CombatantUtils.Label(__instance.OwningMech)} and target: {CombatantUtils.Label(__instance.MeleeTarget)}.");
            if (attackCompleteMessage.stackItemUID == ___meleeSequence.SequenceGUID && ModState.MeleeStates?.SelectedState != null)
            {
                // Target stability and unsteady - always applies as we're always a mech
                if (ModState.MeleeStates.SelectedState.ForceUnsteadyOnAttacker)
                {
                    Mod.Log.Info(" -- Forcing attacker to become unsteady from attack!");
                    __instance.OwningMech.ApplyUnsteady();
                }
                if (ModState.MeleeStates.SelectedState.AttackerInstability != 0)
                {
                    Mod.Log.Info($" -- Adding {ModState.MeleeStates.SelectedState.AttackerInstability} absolute instability to attacker.");
                    __instance.OwningMech.AddAbsoluteInstability(ModState.MeleeStates.SelectedState.AttackerInstability, StabilityChangeSource.Attack, "-1");
                }

                // Attacker cluster damage
                if (ModState.MeleeStates.SelectedState.AttackerDamageClusters.Length > 0)
                {
                    Mod.Log.Info($" -- Applying {ModState.MeleeStates.SelectedState.AttackerDamageClusters.Sum()} damage to attacker as {ModState.MeleeStates.SelectedState.AttackerDamageClusters.Length} clusters.");
                    AttackHelper.CreateImaginaryAttack(__instance.OwningMech, __instance.OwningMech, __instance.SequenceGUID, ModState.MeleeStates.SelectedState.AttackerDamageClusters);
                }

                // Target stability and unsteady - only applies to mech targets
                if (__instance.MeleeTarget is Mech targetMech)
                {
                    if (ModState.MeleeStates.SelectedState.ForceUnsteadyOnTarget)
                    {
                        Mod.Log.Info(" -- Forcing target to become unsteady from attack!");
                        targetMech.ApplyUnsteady();
                    }
                    if (ModState.MeleeStates.SelectedState.TargetInstability != 0)
                    {
                        Mod.Log.Info($" -- Adding {ModState.MeleeStates.SelectedState.TargetInstability} absolute instability to target.");
                        targetMech.AddAbsoluteInstability(ModState.MeleeStates.SelectedState.TargetInstability, StabilityChangeSource.Attack, "-1");
                    }
                }

                // Target cluster damage - first attack was applied through melee weapon
                if (ModState.MeleeStates.SelectedState.TargetDamageClusters.Length > 1)
                {
                    // The target already got hit by the first cluster as the weapon damage. Only add the additional hits
                    float[] clusterDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.SubArray(1, ModState.MeleeStates.SelectedState.TargetDamageClusters.Length);
                    Mod.Log.Info($" -- Applying {clusterDamage.Sum()} damage to target as {clusterDamage.Length} clusters.");
                    AttackHelper.CreateImaginaryAttack(__instance.OwningMech, __instance.MeleeTarget, __instance.SequenceGUID, clusterDamage);
                }

                Mod.Log.Info($"== Done.");
            }

            // Reset melee state
            ModState.MeleeStates = null;
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "CompleteOrders")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_CompleteOrders
    {
        static void Postfix(MechMeleeSequence __instance)
        {
            Mod.Log.Trace("MMS:CO - entered.");

            // Base method checks for target knockdown. Do the same for the attacker.
            if (!__instance.OwningMech.IsDead)
            {
                __instance.OwningMech.CheckForInstability();
                __instance.OwningMech.HandleKnockdown(__instance.RootSequenceGUID, __instance.owningActor.GUID, Vector2.one, null);
            }


            // Invalidate our melee state as we're done
            ModState.MeleeStates = null;
            ModState.MeleeType = MeleeAttackType.NotSet;
            ModState.MeleeWeapon = null;
        }
    }

    // Force the source or target to make a piloting check or fall down
    //[HarmonyPatch(typeof(MechMeleeSequence), "OnMeleeComplete")]
    //public static class MechMeleeSequence_OnMeleeComplete {
    //    public static void Postfix(MechMeleeSequence __instance, MessageCenterMessage message) {
    //        AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;

    //        if (__instance.selectedMeleeType == MeleeAttackType.Kick || __instance.selectedMeleeType == MeleeAttackType.Stomp) {
    //            if (attackCompleteMessage.attackSequence.attackCompletelyMissed) {
    //                Mod.Log.Debug($" Kick attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} failed.");
    //                // Check for source falling
    //                float sourceMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
    //                bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.MissedKickFallChance, __instance.OwningMech, sourceMulti, ModText.FT_Melee_Kick);
    //                if (!sourcePassed) {
    //                    Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check from missed kick, forcing fall.");
    //                    MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Melee_Kick);
    //                } 
    //            } else {
    //                Mod.Log.Debug($" Kick attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} succeeded.");
    //                // Check for target falling
    //                if (__instance.MeleeTarget is Mech targetMech) {
    //                    float targetMulti = targetMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
    //                    bool targetPassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.HitByKickFallChance, targetMech, targetMulti, ModText.FT_Melee_Kick);
    //                    if (!targetPassed) {
    //                        Mod.Log.Info($"Target actor: {CombatantUtils.Label(targetMech)} failed pilot check from kick, forcing fall.");
    //                        MechHelper.AddFallingSequence(targetMech, __instance, ModText.FT_Melee_Kick);
    //                    }
    //                } else {
    //                    Mod.Log.Debug($"Target actor: {CombatantUtils.Label(__instance.MeleeTarget)} is not a mech, cannot fall - skipping.");
    //                }
    //            }
    //        }

    //        if (__instance.selectedMeleeType == MeleeAttackType.Charge || __instance.selectedMeleeType == MeleeAttackType.Tackle) {
    //            if (!attackCompleteMessage.attackSequence.attackCompletelyMissed) {
    //                Mod.Log.Debug($" Charge attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} succeeded.");

    //                // Check for source falling
    //                float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
    //                bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.MadeChargeFallChance, __instance.OwningMech, sourceSkillMulti, ModText.FT_Melee_Charge);
    //                if (!sourcePassed) {
    //                    Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check from charge, forcing fall.");
    //                    MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Melee_Charge);
    //                }

    //                // Check for target falling
    //                if (__instance.MeleeTarget is Mech targetMech) {
    //                    float targetSkillMulti = targetMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
    //                    bool targetPassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.HitByChargeFallChance, targetMech, targetSkillMulti, ModText.FT_Melee_Charge);
    //                    if (!targetPassed) {
    //                        Mod.Log.Info($"Target actor: {CombatantUtils.Label(targetMech)} failed pilot check from charge, forcing fall.");
    //                        MechHelper.AddFallingSequence(targetMech, __instance, ModText.FT_Melee_Charge);
    //                    }
    //                } else {
    //                    Mod.Log.Debug($"Target actor: {CombatantUtils.Label(__instance.MeleeTarget)} is not a mech, cannot fall - skipping.");
    //                }
    //            } else {
    //                Mod.Log.Debug($" Charge attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} failed.");
    //            }
    //        }

    //        // Reset melee state
    //        ModState.MeleeStates = null;
    //    }
    //}
}

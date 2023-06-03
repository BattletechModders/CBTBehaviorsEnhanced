using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CBTBehaviorsEnhanced.MeleeStates;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using FluffyUnderware.DevTools.Extensions;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using us.frostraptor.modUtils;
using static FSM.StringStateMachine;

namespace CBTBehaviorsEnhanced.Melee
{

    [HarmonyPatch(typeof(MechDFASequence), "Init")]
    [HarmonyPatch(new Type[] { typeof(Mech), typeof(ICombatant), typeof(List<Weapon>), typeof(Vector3), typeof(Quaternion) })]
    static class MechDFASequence_Init
    {
        //Init(Mech mech, ICombatant DFATarget, List<Weapon> requestedWeapons, Vector3 finalJumpDestination, Quaternion finalJumpRotation)
        static void Postfix(MechDFASequence __instance, Mech mech, ICombatant DFATarget,
            List<Weapon> requestedWeapons, Vector3 finalJumpDestination)
        {
            try
            {
                // Find the selectedAttack we should use for this sequence
                MeleeAttack selectedAttack = ModState.GetSelectedAttack(mech);
                if (selectedAttack == null)
                {
                    Mod.MeleeLog.Warn?.Write($"Melee sequence {__instance.SequenceGUID} has no pre-selected attack state, will have to autoselected. Let Frost know as this should not happen!");
                    MeleeState meleeState = ModState.AddorUpdateMeleeState(mech, finalJumpDestination, DFATarget as AbstractActor);
                    if (meleeState == null)
                    {
                        Mod.Log.Error?.Write($"Could not build DFA state for selected melee attack - this should NEVER happen!");
                        return;
                    }
                    selectedAttack = meleeState.DFA;
                }

                if (selectedAttack == null || !selectedAttack.IsValid || !(selectedAttack is DFAAttack))
                {
                    Mod.Log.Error?.Write($"Could not select a valid attack for the selected sequence - this should NEVER happen!");
                    return;
                }

                // Check to see if we have an imaginary weapon to use; if not create it
                (Weapon meleeWeapon, Weapon dfaWeapon) weapons = ModState.GetFakedWeapons(mech);

                // Create the weapon + representation 
                ModState.AddOrUpdateMeleeSequenceState(__instance.SequenceGUID, selectedAttack, weapons.dfaWeapon);

                // TODO: Filter selected weapons by melee
                StringBuilder sb = new StringBuilder();
                foreach (Weapon weapon in requestedWeapons)
                {
                    sb.Append(weapon.UIName);
                    sb.Append(",");
                }
                Mod.MeleeLog.Info?.Write($"  -- Initial requested weapons: {sb}");

                // Modify the owning mech DFA melee weapon to do the 'first' hit
                float targetDamage = selectedAttack.TargetDamageClusters?.Length > 0 ?
                   selectedAttack.TargetDamageClusters[0] : 0;
                __instance.OwningMech.DFAWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                __instance.OwningMech.DFAWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set DFA weapon damage: {targetDamage}  and instability: {selectedAttack.TargetInstability}");

                // Cache the attacker's original DFASelfDamage value and set it to zero, so we can apply our own damage
                ModState.OriginalDFASelfDamage = __instance.OwningMech.StatCollection.GetValue<float>(ModStats.HBS_DFA_Self_Damage);
                __instance.OwningMech.StatCollection.Set<float>(ModStats.HBS_DFA_Self_Damage, 0f);
                __instance.OwningMech.StatCollection.Set<bool>(ModStats.HBS_DFA_Causes_Self_Unsteady, false);

                // Make sure we use the target's damage table
                ModState.ForceDamageTable = selectedAttack.TargetTable;

            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to initialize DFA sequence {__instance.SequenceGUID}!");
            }
        }
    }

    [HarmonyPatch(typeof(MechDFASequence), "OnAdded")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_OnAdded
    {
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
        {
            if (!__runOriginal) return;

            Mod.MeleeLog.Info?.Write($"DFASequence added for attacker: {__instance.OwningMech.DistinctId()} from position: {__instance.DesiredMeleePosition}  " +
                $"against target: {__instance.MeleeTarget.DistinctId()}");
        }
    }



    [HarmonyPatch(typeof(MechDFASequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_BuildMeleeDirectorSequence
    {
        static void Postfix(MechDFASequence __instance)
        {
            // TODO: If this happens before the above... need to grab the selected melee type from state
            Mod.MeleeLog.Info?.Write($"Setting current melee type to: {MeleeAttackType.DFA} and weapon to: {__instance.OwningMech.DFAWeapon.UIName}");

            (MeleeAttack meleeAttack, Weapon fakeWeapon) seqState = ModState.GetMeleeSequenceState(__instance.SequenceGUID);
            if (seqState.meleeAttack != null)
            {
                //// Modify the owning mech DFA melee weapon to do the 'first' hit
                //float targetDamage = seqState.meleeAttack.TargetDamageClusters?.Length > 0 ?
                //    seqState.meleeAttack.TargetDamageClusters[0] : 0;
                //__instance.OwningMech.DFAWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                //__instance.OwningMech.DFAWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                //Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set DFA weapon damage: {targetDamage}  and instability: {seqState.meleeAttack.TargetInstability}");

                //// Cache the attacker's original DFASelfDamage value and set it to zero, so we can apply our own damage
                //ModState.OriginalDFASelfDamage = __instance.OwningMech.StatCollection.GetValue<float>(ModStats.HBS_DFA_Self_Damage);
                //__instance.OwningMech.StatCollection.Set<float>(ModStats.HBS_DFA_Self_Damage, 0f);
                //__instance.OwningMech.StatCollection.Set<bool>(ModStats.HBS_DFA_Causes_Self_Unsteady, false);

                //// Make sure we use the target's damage table
                //ModState.ForceDamageTable = seqState.meleeAttack.TargetTable;

                // Filter any weapons from requested weapons. This works because BuildMeleeDirectorSequence is called immediately before BuildWeaponDirectorSequence
                if (Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack)
                {
                    Mod.MeleeLog.Debug?.Write($"Filtering DFA weapons by attack type: {seqState.meleeAttack.Label}");
                    List<Weapon> allowedWeapons = new List<Weapon>();
                    foreach (Weapon weapon in __instance.requestedWeapons)
                    {
                        if (seqState.meleeAttack.IsRangedWeaponAllowed(weapon))
                        {
                            Mod.MeleeLog.Debug?.Write($" -- Weapon: {weapon.UIName} is allowed by melee type.");
                            allowedWeapons.Add(weapon);
                        }
                    }
                    Mod.MeleeLog.Debug?.Write($"  -- After filtering {allowedWeapons.Count} weapons will be used.");

                    __instance.requestedWeapons.Clear();
                    __instance.requestedWeapons.AddRange(allowedWeapons);
                }
            }
        }
    }

    // Apply the attacker's instability before they move. They will naturally dump some stability
    //   due to movement, so adding here ensures it gets calculated properly
    [HarmonyPatch(typeof(MechDFASequence), "ExecuteJump")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_ExecuteJump
    {
        static void Prefix(ref bool __runOriginal, MechDFASequence __instance)
        {
            if (!__runOriginal) return;

            (MeleeAttack meleeAttack, Weapon fakeWeapon) seqState = ModState.GetMeleeSequenceState(__instance.SequenceGUID);
            if (seqState.meleeAttack != null && seqState.meleeAttack.AttackerInstability != 0 && __instance.OwningMech.isHasStability())
            {
                Mod.MeleeLog.Info?.Write($" -- Adding {seqState.meleeAttack.AttackerInstability} absolute instability to attacker.");
                __instance.OwningMech.AddAbsoluteInstability(seqState.meleeAttack.AttackerInstability, StabilityChangeSource.Attack, "-1");
            }
        }
    }

    [HarmonyPatch(typeof(MechDFASequence), "OnMeleeComplete")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_OnMeleeComplete
    {
        static void Prefix(ref bool __runOriginal, MechDFASequence __instance, MessageCenterMessage message)
        {
            if (!__runOriginal) return;

            Mod.Log.Trace?.Write("MMS:OMC entered.");

            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            Mod.MeleeLog.Info?.Write($"== Resolving cluster damage, instability, and unsteady on DFA attacker: {CombatantUtils.Label(__instance.OwningMech)} and " +
                $"target: {CombatantUtils.Label(__instance.DFATarget)}.");
            (MeleeAttack meleeAttack, Weapon fakeWeapon) seqState = ModState.GetMeleeSequenceState(__instance.SequenceGUID);

            if (attackCompleteMessage.stackItemUID == __instance.meleeSequence.SequenceGUID && seqState.meleeAttack != null)
            {
                // Check to see if the target was hit
                bool targetWasHit = false;
                foreach (AttackDirector.AttackSequence attackSequence in __instance.meleeSequence.directorSequences)
                {
                    if (!attackSequence.attackCompletelyMissed)
                    {
                        targetWasHit = true;
                        Mod.MeleeLog.Info?.Write($" -- AttackSequence: {attackSequence.stackItemUID} hit the target.");
                    }
                    else
                    {
                        Mod.MeleeLog.Info?.Write($" -- AttackSequence: {attackSequence.stackItemUID} missed the target.");
                    }
                }

                // Attacker unsteady is interpreted as 'dump evasion' for vehicles
                if ((targetWasHit && seqState.meleeAttack.UnsteadyAttackerOnHit) ||
                    (!targetWasHit && seqState.meleeAttack.UnsteadyAttackerOnMiss))
                {
                    bool forceAttackerUnsteady = false;
                    if (__instance.OwningMech.isHasStability() && !__instance.OwningMech.IsOrWillBeProne)
                    {
                        Mod.MeleeLog.Info?.Write(" -- Forcing attacker to become unsteady from attack!");
                        forceAttackerUnsteady = true;
                    }
                    __instance.OwningMech.DumpEvasion(forceUnsteady: forceAttackerUnsteady);
                }

                // Attacker cluster damage
                if (targetWasHit && !__instance.OwningMech.IsDead)
                {
                    // Make sure we use the attackers's damage table
                    ModState.ForceDamageTable = seqState.meleeAttack.AttackerTable;
                    if (seqState.meleeAttack.AttackerDamageClusters.Length > 0)
                    {
                        try
                        {
                            Mod.MeleeLog.Info?.Write($" -- Applying {seqState.meleeAttack.AttackerDamageClusters.Sum()} damage to attacker as {seqState.meleeAttack.AttackerDamageClusters.Length} clusters.");
                            AttackHelper.CreateImaginaryAttack(__instance.OwningMech, seqState.fakeWeapon, __instance.OwningMech, __instance.SequenceGUID,
                                seqState.meleeAttack.AttackerDamageClusters, DamageType.Melee, MeleeAttackType.Kick);
                        }
                        catch (Exception e)
                        {
                            Mod.Log.Error?.Write(e, "FAILED TO APPLY DFA DAMAGE TO ATTACKER");
                        }
                    }
                }

                if (targetWasHit)
                {

                    // Target mech stability and unsteady

                    if (__instance.DFATarget is Mech targetMech && targetMech.isHasStability() && !targetMech.IsProne)
                    {
                        if (seqState.meleeAttack.TargetInstability != 0)
                        {
                            Mod.MeleeLog.Info?.Write($" -- Adding {seqState.meleeAttack.TargetInstability} absolute instability to target.");
                            targetMech.AddAbsoluteInstability(seqState.meleeAttack.TargetInstability, StabilityChangeSource.Attack, "-1");
                        }

                        if (seqState.meleeAttack.OnTargetMechHitForceUnsteady)
                        {
                            Mod.MeleeLog.Info?.Write(" -- Forcing target to become unsteady from attack!");
                            targetMech.DumpEvasion(forceUnsteady: true);
                        }

                    }

                    // Target vehicle evasion damage
                    if (__instance.DFATarget is Vehicle || __instance.DFATarget.FakeVehicle() || __instance.DFATarget.NavalUnit())
                    {
                        AbstractActor targetActor = __instance.DFATarget as AbstractActor;
                        if (seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved != 0 && targetActor.EvasivePipsCurrent > 0)
                        {
                            Mod.MeleeLog.Info?.Write($" -- Removing {seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved} from target vehicle.");
                            int modifiedPips = targetActor.EvasivePipsCurrent - seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved;
                            if (modifiedPips < 0) modifiedPips = 0;

                            targetActor.EvasivePipsCurrent = modifiedPips;
                            SharedState.Combat.MessageCenter.PublishMessage(new EvasiveChangedMessage(targetActor.GUID, targetActor.EvasivePipsCurrent));
                        }
                    }

                    // Target cluster damage - first attack was applied through melee weapon
                    if (seqState.meleeAttack.TargetDamageClusters.Length > 1 && !__instance.DFATarget.IsDead)
                    {
                        try
                        {
                            // Make sure we use the attackers's damage table
                            ModState.ForceDamageTable = seqState.meleeAttack.TargetTable;

                            // The target already got hit by the first cluster as the weapon damage. Only add the additional hits
                            float[] clusterDamage = seqState.meleeAttack.TargetDamageClusters.SubArray(1, seqState.meleeAttack.TargetDamageClusters.Length);
                            Mod.MeleeLog.Info?.Write($" -- Applying {clusterDamage.Sum()} damage to target as {clusterDamage.Length} clusters.");
                            AttackHelper.CreateImaginaryAttack(__instance.OwningMech, seqState.fakeWeapon, __instance.DFATarget, __instance.SequenceGUID, clusterDamage,
                                DamageType.Melee, MeleeAttackType.DFA);
                        }
                        catch (Exception e)
                        {
                            Mod.Log.Error?.Write(e, "FAILED TO APPLY DFA DAMAGE TO TARGET");
                        }
                    }
                }

                Mod.MeleeLog.Info?.Write($"== Done.");
            }

            // Restore the attacker's DFA damage
            __instance.OwningMech.StatCollection.Set<float>(ModStats.HBS_DFA_Self_Damage, ModState.OriginalDFASelfDamage);
            __instance.OwningMech.StatCollection.Set<bool>(ModStats.HBS_DFA_Causes_Self_Unsteady, true);

            // Reset melee state
            ModState.ForceDamageTable = DamageTable.NONE;
            ModState.OriginalDFASelfDamage = 0f;
        }
    }

    // Attack locations are calculated when the sequence is generated. Rebuild the attack sequence before firing to ensure we use the correct dictionary
    [HarmonyPatch(typeof(MechDFASequence), "FireWeapons")]
    static class MechDFASequence_FireWeapons
    {
        static void Prefix(ref bool __runOriginal, MechDFASequence __instance)
        {
            if (!__runOriginal) return;

            // Reset melee state
            ModState.ForceDamageTable = DamageTable.NONE;

            Mod.MeleeLog.Debug?.Write("Regenerating melee support weapons hit locations...");
            __instance.BuildWeaponDirectorSequence();
            Mod.MeleeLog.Debug?.Write(" -- Done!");
        }
    }

    [HarmonyPatch(typeof(MechDFASequence), "CompleteOrders")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_CompleteOrders
    {
        static void Postfix(MechDFASequence __instance)
        {
            Mod.Log.Trace?.Write("MMS:CO - entered.");

            // Base method checks for target knockdown. Do the same for the attacker.
            if (!__instance.OwningMech.IsDead)
            {
                __instance.OwningMech.CheckForInstability();
                __instance.OwningMech.HandleKnockdown(__instance.RootSequenceGUID, __instance.owningActor.GUID, Vector2.one, null);
            }

            // Invalidate our melee state as we're done
            ModState.ForceDamageTable = DamageTable.NONE;

            ModState.InvalidateState(__instance.OwningMech);
        }
    }
}

using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CBTBehaviorsEnhanced.MeleeStates;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using CustomUnits;
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

    [HarmonyPatch(typeof(MechMeleeSequence), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Mech), typeof(ICombatant), typeof(List<Weapon>), typeof(Vector3) })]
    public static class MechMeleeSequence_ctor
    {
        public static bool isValid = false;

        static void Postfix(MechMeleeSequence __instance, Mech mech, ICombatant meleeTarget,
            List<Weapon> requestedWeapons, Vector3 desiredMeleePosition)
        {
            try
            {
                // Find the selectedAttack we should use for this sequence
                MeleeAttack selectedAttack = ModState.GetSelectedAttack(mech);
                if (selectedAttack == null)
                {
                    Mod.MeleeLog.Warn?.Write($"Melee sequence {__instance.SequenceGUID} has no pre-selected attack state, will have to autoselected. Let Frost know as this should not happen!");
                    MeleeState meleeState = ModState.AddorUpdateMeleeState(mech, desiredMeleePosition, meleeTarget as AbstractActor);
                    if (meleeState == null)
                    {
                        Mod.Log.Error?.Write($"Could not build melee state for selected melee attack - this should NEVER happen!");
                        return;
                    }
                    selectedAttack = meleeState.GetHighestDamageAttackForUI();
                }

                if (selectedAttack == null || !selectedAttack.IsValid)
                {
                    Mod.Log.Error?.Write($"Could not select a valid attack for the selected sequence - this should NEVER happen!");
                    return;
                }

                // Check to see if we have an imaginary weapon to use; if not create it
                (Weapon meleeWeapon, Weapon dfaWeapon) weapons = ModState.GetFakedWeapons(mech);

                // Create the weapon + representation 
                ModState.AddOrUpdateMeleeSequenceState(__instance.SequenceGUID, selectedAttack, weapons.meleeWeapon);

                StringBuilder sb = new StringBuilder();
                foreach (Weapon weapon in requestedWeapons)
                {
                    sb.Append(weapon.UIName);
                    sb.Append(",");
                }
                Mod.MeleeLog.Info?.Write($"  -- Initial requested weapons: {sb}");

                // Modify the owning mech melee weapon to do the 'first' hit - but apply stab damage later
                float targetDamage = selectedAttack.TargetDamageClusters?.Length > 0 ?
                    selectedAttack.TargetDamageClusters[0] : 0;
                __instance.OwningMech.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                __instance.OwningMech.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set melee weapon damage: {targetDamage} and instability: {selectedAttack.TargetInstability}");
                // Associate the melee weapon itself with the CU special hit tables for unique tables (vtols, etc)

                // Make sure we use the targets's damage table
                ModState.ForceDamageTable = selectedAttack.TargetTable;
                __instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).SetValue<string>($"CBTBE_MELEE_{ModState.ForceDamageTable}");
                Mod.MeleeLog.Debug?.Write($" -- Weapon: {__instance.OwningMech.MeleeWeapon.UIName} is allowed. HitTable:{__instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).Value<string>()}");

                isValid = true;
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to initialize Melee sequence {__instance.SequenceGUID}!");
            }
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "OnAdded")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_OnAdded
    {
        private enum FakeMeleeSequenceState
        {
            None,
            Weapons,
            Moving,
            Melee,
            Finished
        }

        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
        {
            if (!__runOriginal) return;

            if (!MechMeleeSequence_ctor.isValid)
            {
                Mod.MeleeLog.Info?.Write($"  -- Invalid sequence in OnAdded, skipping!");

                __instance.MeleeTarget = null;
                __instance.OrdersAreComplete = true;
                return;
            }

            Mod.MeleeLog.Info?.Write($"MeleeSequence added for " +
                $"attacker: {__instance.OwningMech.DistinctId()} from pos: {__instance.DesiredMeleePosition}" +
                $"against target: {__instance.MeleeTarget.DistinctId()} at pos: {__instance.MeleeTarget.CurrentPosition}");

        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_BuildMeleeDirectorSequence
    {
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
        {
            if (!__runOriginal) return;

            // TODO: If this happens before the above... need to grab the selected melee type from state
            Mod.MeleeLog.Info?.Write($"Setting current melee animation to: {__instance.selectedMeleeType} and weapon to: {__instance.OwningMech.MeleeWeapon.UIName}");

            (MeleeAttack meleeAttack, Weapon fakeWeapon) seqState = ModState.GetMeleeSequenceState(__instance.SequenceGUID);
            if (seqState.meleeAttack != null)
            {
                //// Modify the owning mech melee weapon to do the 'first' hit - but apply stab damage later
                //float targetDamage = seqState.meleeAttack.TargetDamageClusters?.Length > 0 ?
                //    seqState.meleeAttack.TargetDamageClusters[0] : 0;
                //__instance.OwningMech.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                //__instance.OwningMech.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                //Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set melee weapon damage: {targetDamage} and instability: {seqState.meleeAttack.TargetInstability}");
                //// Associate the melee weapon itself with the CU special hit tables for unique tables (vtols, etc)

                //// Make sure we use the targets's damage table
                //ModState.ForceDamageTable = seqState.meleeAttack.TargetTable;
                //__instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).SetValue<string>($"CBTBE_MELEE_{ModState.ForceDamageTable}");
                //Mod.MeleeLog.Debug?.Write($" -- Weapon: {__instance.OwningMech.MeleeWeapon.UIName} is allowed. HitTable:{__instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).Value<string>()}");

                // Filter any weapons from requested weapons. This works because BuildMeleeDirectorSequence is called immediately before BuildWeaponDirectorSequence
                if (Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack)
                {
                    Mod.MeleeLog.Debug?.Write($"Filtering melee weapons by attack type: {seqState.meleeAttack.Label}");
                    List<Weapon> allowedWeapons = new List<Weapon>();
                    foreach (Weapon weapon in __instance.requestedWeapons)
                    {
                        if (seqState.meleeAttack.IsRangedWeaponAllowed(weapon))
                        {
                            allowedWeapons.Add(weapon);
                            // Associate the weapon with the CU special hit tables for unique tables (vtols, etc)
                            //weapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).SetValue<string>($"CBTBE_MELEE_{ModState.ForceDamageTable}");
                            Mod.MeleeLog.Debug?.Write($" -- Weapon: {weapon.UIName} is allowed. HitTable:{weapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).Value<string>()}");
                        }
                        else
                        {
                            Mod.MeleeLog.Debug?.Write($" -- Weapon: {weapon.UIName} cannot be used");
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
    [HarmonyPatch(typeof(MechMeleeSequence), "ExecuteMove")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_ExecuteMove
    {
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
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

    [HarmonyPatch(typeof(MechMeleeSequence), "ExecuteMelee")]
    static class MechMeleeSequence_ExecuteMelee
    {
        // Remove the BuildWeaponDirectorSequence, to prevent duplicate ammo consumption
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
        {
            if (!__runOriginal) return;

            __instance.BuildMeleeDirectorSequence();

            if (__instance.OwningMech.GameRep != null)
            {
                __instance.OwningMech.GameRep.ReturnToNeutralFacing(isParellelSequence: true, 0.5f, __instance.RootSequenceGUID, __instance.SequenceGUID, null);
            }
            if (__instance.OwningMech.GameRep != null)
            {
                __instance.OwningMech.GameRep.FadeThrottleAudio(0f, 50f, 1f);
            }
            SharedState.Combat.MessageCenter.AddSubscriber(MessageCenterMessageType.OnAttackComplete, __instance.OnMeleeComplete);
            SharedState.Combat.MessageCenter.AddSubscriber(MessageCenterMessageType.OnAttackSequenceFire, __instance.OnMeleeReady);

            AttackStackSequence meleeSequence = __instance.meleeSequence;

            SharedState.Combat.MessageCenter.PublishMessage(new AddParallelSequenceToStackMessage(meleeSequence));

            __runOriginal = false;
        }
    }

    // TODO: Applying all the self-damage and instab reduction here feels weird, as it could interact with the
    //   weapons fire poorly. Maybe these should be largely moved to the end?
    //   Also means weapons won't benefit from evasion strip when they are fired. Seems like a problem and won't be reflected
    //   in the UI when estimating
    [HarmonyPatch(typeof(MechMeleeSequence), "OnMeleeComplete")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_OnMeleeComplete
    {
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance, MessageCenterMessage message)
        {
            if (!__runOriginal) return;

            Mod.MeleeLog.Trace?.Write("MMS:OMC entered.");

            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            Mod.MeleeLog.Info?.Write($"== Resolving cluster damage, instability, and unsteady on melee attacker: {CombatantUtils.Label(__instance.OwningMech)} and target: {CombatantUtils.Label(__instance.MeleeTarget)}.");
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
                                seqState.meleeAttack.AttackerDamageClusters, DamageType.Melee, seqState.meleeAttack.AttackAnimation);
                        }
                        catch (Exception e)
                        {
                            Mod.Log.Error?.Write(e, "FAILED TO APPLY MELEE DAMAGE TO ATTACKER!");
                        }
                    }
                }

                if (targetWasHit)
                {
                    // Target mech instability and unsteady 
                    if (__instance.MeleeTarget is Mech targetMech && targetMech.isHasStability() && !targetMech.IsProne)
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
                    if (__instance.MeleeTarget is Vehicle || __instance.MeleeTarget.FakeVehicle() || __instance.MeleeTarget.NavalUnit())
                    {
                        AbstractActor targetActor = __instance.MeleeTarget as AbstractActor;
                        if (seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved != 0 && targetActor.EvasivePipsCurrent > 0)
                        {
                            Mod.MeleeLog.Info?.Write($" -- Removing {seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved} from target vehicle.");
                            int modifiedPips = targetActor.EvasivePipsCurrent - seqState.meleeAttack.OnTargetVehicleHitEvasionPipsRemoved;
                            if (modifiedPips < 0) modifiedPips = 0;

                            targetActor.EvasivePipsCurrent = modifiedPips;
                            SharedState.Combat.MessageCenter.PublishMessage(new EvasiveChangedMessage(targetActor.GUID, targetActor.EvasivePipsCurrent));
                        }
                    }

                    // Target cluster damage
                    if (seqState.meleeAttack.TargetDamageClusters.Length > 1 && !__instance.MeleeTarget.IsDead)
                    {
                        try
                        {
                            // Make sure we use the targets's damage table
                            ModState.ForceDamageTable = seqState.meleeAttack.TargetTable;

                            // The target already got hit by the first cluster as the weapon damage. Only add the additional hits
                            float[] clusterDamage = seqState.meleeAttack.TargetDamageClusters.SubArray(1, seqState.meleeAttack.TargetDamageClusters.Length);
                            Mod.MeleeLog.Info?.Write($" -- Applying {clusterDamage.Sum()} damage to target as {clusterDamage.Length} clusters.");
                            AttackHelper.CreateImaginaryAttack(__instance.OwningMech, seqState.fakeWeapon, __instance.MeleeTarget, __instance.SequenceGUID, clusterDamage,
                                DamageType.Melee, seqState.meleeAttack.AttackAnimation);

                        }
                        catch (Exception e)
                        {
                            Mod.Log.Error?.Write(e, "FAILED TO APPLY MELEE DAMAGE TO TARGET!");
                        }
                    }

                }

                Mod.MeleeLog.Info?.Write($"== Done.");
            }

            // Reset damage table for mechs only; troops need to persist through to the end
            if (!(__instance.OwningMech is TrooperSquad))
            {
                __instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).SetValue<string>(string.Empty);
                Mod.MeleeLog.Debug?.Write($"  -- Weapon: {__instance.OwningMech.MeleeWeapon.UIName} is reseted. HitTable:{__instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).Value<string>()}");
                ModState.ForceDamageTable = DamageTable.NONE;
            }

        }
    }

    // Attack locations are calculated when the sequence is generated. Rebuild the attack sequence before firing to ensure we use the correct dictionary
    [HarmonyPatch(typeof(MechMeleeSequence), "FireWeapons")]
    static class MechMeleeSequence_FireWeapons
    {
        static void Prefix(ref bool __runOriginal, MechMeleeSequence __instance)
        {
            if (!__runOriginal) return;

            Mod.MeleeLog.Debug?.Write("Regenerating melee support weapons hit locations...");
            __instance.BuildWeaponDirectorSequence();
            Mod.MeleeLog.Debug?.Write(" -- Done!");

            // Reset damage table 
            ModState.ForceDamageTable = DamageTable.NONE;
            try {
                __instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).SetValue<string>(string.Empty);
                Mod.MeleeLog.Debug?.Write($"  -- Weapon: {__instance.OwningMech.MeleeWeapon.UIName} is reseted. HitTable:{__instance.OwningMech.MeleeWeapon.StatCollection.GetOrCreateStatisic<string>(CustomAmmoCategories.SPECIAL_HIT_TABLE_NAME, string.Empty).Value<string>()}"); 
                ModState.ForceDamageTable = DamageTable.NONE;
            }
            catch (Exception e) {
                Mod.MeleeLog.Error?.Write(e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "CompleteOrders")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_CompleteOrders
    {
        static void Postfix(MechMeleeSequence __instance)
        {
            if (!MechMeleeSequence_ctor.isValid)
            {
                Mod.MeleeLog.Info?.Write($"  -- Invalid sequence in OnAdded, skipping!");
                return;
            }

            Mod.MeleeLog.Trace?.Write("MMS:CO - entered.");

            // Base method checks for target knockdown. Do the same for the attacker.
            if (!__instance.OwningMech.IsDead)
            {
                __instance.OwningMech.CheckForInstability();
                __instance.OwningMech.HandleKnockdown(__instance.RootSequenceGUID, __instance.owningActor.GUID, Vector2.one, null);
            }

            ModState.InvalidateState(__instance.OwningMech);
        }
    }
}

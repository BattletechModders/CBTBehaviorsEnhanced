using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using FluffyUnderware.DevTools.Extensions;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee {

    [HarmonyPatch(typeof(MechMeleeSequence), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Mech), typeof(ICombatant), typeof(List <Weapon>), typeof(Vector3)})]
    static class MechMeleeSequence_ctor
    {
        static void Postfix(MechMeleeSequence __instance, Mech mech, ICombatant meleeTarget, List<Weapon> requestedWeapons, Vector3 desiredMeleePosition, List<Weapon> ___requestedWeapons)
        {
            Mod.MeleeLog.Info?.Write($"Melee sequence {__instance.SequenceGUID} created for attacker: {mech.DistinctId()} vs. target: {meleeTarget.DistinctId()} using position: {desiredMeleePosition}");
            StringBuilder sb = new StringBuilder();
            foreach (Weapon weapon in requestedWeapons)
            {
                sb.Append(weapon.UIName);
                sb.Append(",");
            }
            Mod.MeleeLog.Info?.Write($"  -- Initial requested weapons: {sb}");


        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_BuildMeleeDirectorSequence
    {
        static void Prefix(MechMeleeSequence __instance, List<Weapon> ___requestedWeapons)
        {
            // TODO: If this happens before the above... need to grab the selected melee type from state
            Mod.MeleeLog.Info?.Write($"Setting current melee type to: {__instance.selectedMeleeType} and weapon to: {__instance.OwningMech.MeleeWeapon.UIName}");
            ModState.MeleeWeapon = __instance.OwningMech.MeleeWeapon;
            ModState.MeleeType = __instance.selectedMeleeType;

            if (ModState.MeleeStates?.SelectedState != null)
            {
                ModState.MeleeType = ModState.MeleeStates.SelectedState.AttackAnimation;

                // Modify the owning mech melee weapon to do the 'first' hit - but apply stab damage later
                float targetDamage = ModState.MeleeStates.SelectedState?.TargetDamageClusters?.Length > 0 ?
                    ModState.MeleeStates.SelectedState.TargetDamageClusters[0] : 0;
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set melee weapon damage: {targetDamage} and instability: {ModState.MeleeStates.SelectedState.TargetInstability}");

                // Make sure we use the targets's damage table
                ModState.ForceDamageTable = ModState.MeleeStates.SelectedState.TargetTable;

                // Filter any weapons from requested weapons. This works because BuildMeleeDirectorSequence is called immediately before BuildWeaponDirectorSequence
                if (Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack)
                {
                    Mod.MeleeLog.Debug?.Write($"Filtering melee weapons by attack type: {ModState.MeleeStates.SelectedState.Label}");
                    List<Weapon> allowedWeapons = new List<Weapon>();
                    foreach (Weapon weapon in ___requestedWeapons)
                    {
                        if (ModState.MeleeStates.SelectedState.IsRangedWeaponAllowed(weapon))
                        {
                            Mod.MeleeLog.Debug?.Write($" -- Weapon: {weapon.UIName} is allowed by melee type.");
                            allowedWeapons.Add(weapon);
                        }
                    }
                    ___requestedWeapons.Clear();
                    ___requestedWeapons.AddRange(allowedWeapons);
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
        static void Prefix(MechMeleeSequence __instance)
        {
            if (ModState.MeleeStates?.SelectedState != null && ModState.MeleeStates.SelectedState.AttackerInstability != 0)
            {
                Mod.MeleeLog.Info?.Write($" -- Adding {ModState.MeleeStates.SelectedState.AttackerInstability} absolute instability to attacker.");
                __instance.OwningMech.AddAbsoluteInstability(ModState.MeleeStates.SelectedState.AttackerInstability, StabilityChangeSource.Attack, "-1");
            }
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "ExecuteMelee")]
    static class MechMeleeSequence_ExecuteMelee
    {
        // Remove the BuildWeaponDirectorSequence, to prevent duplicate ammo consumption
        static bool Prefix(MechMeleeSequence __instance)
        {
            Traverse BuildMeleeDirectorSequenceT = Traverse.Create(__instance).Method("BuildMeleeDirectorSequence");
            BuildMeleeDirectorSequenceT.GetValue();

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

            // Reading meleeSequence as a property doesn't seem to work, because it always returns null. I'm unsure if this
            //   is a harmony bug... so read it directly, which seems to work.
            Traverse meleeSequenceT = Traverse.Create(__instance).Field("meleeSequence");
            AttackStackSequence meleeSequence = meleeSequenceT.GetValue<AttackStackSequence>();

            SharedState.Combat.MessageCenter.PublishMessage(new AddParallelSequenceToStackMessage(meleeSequence));

            return false;
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "OnMeleeComplete")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_OnMeleeComplete
    {
        static void Prefix(MechMeleeSequence __instance, MessageCenterMessage message, AttackStackSequence ___meleeSequence)
        {
            Mod.MeleeLog.Trace?.Write("MMS:OMC entered.");

            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            Mod.MeleeLog.Info?.Write($"== Resolving cluster damage, instability, and unsteady on melee attacker: {CombatantUtils.Label(__instance.OwningMech)} and target: {CombatantUtils.Label(__instance.MeleeTarget)}.");
            if (attackCompleteMessage.stackItemUID == ___meleeSequence.SequenceGUID && ModState.MeleeStates?.SelectedState != null)
            {
                // Check to see if the target was hit
                bool targetWasHit = false;
                foreach (AttackDirector.AttackSequence attackSequence in ___meleeSequence.directorSequences)
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

                if (!__instance.OwningMech.IsOrWillBeProne)
                {
                    // Target stability and unsteady - always applies as we're always a mech
                    if ((targetWasHit && ModState.MeleeStates.SelectedState.UnsteadyAttackerOnHit) ||
                        (!targetWasHit && ModState.MeleeStates.SelectedState.UnsteadyAttackerOnMiss))
                    { 
                        Mod.MeleeLog.Info?.Write(" -- Forcing attacker to become unsteady from attack!");
                        __instance.OwningMech.ApplyUnsteady();
                    }            
                }

                // Attacker cluster damage
                if (targetWasHit && !__instance.OwningMech.IsDead)
                {
                    // Make sure we use the attackers's damage table
                    ModState.ForceDamageTable = ModState.MeleeStates.SelectedState.AttackerTable;

                    if (ModState.MeleeStates.SelectedState.AttackerDamageClusters.Length > 0)
                    {
                        Mod.MeleeLog.Info?.Write($" -- Applying {ModState.MeleeStates.SelectedState.AttackerDamageClusters.Sum()} damage to attacker as {ModState.MeleeStates.SelectedState.AttackerDamageClusters.Length} clusters.");
                        AttackHelper.CreateImaginaryAttack(__instance.OwningMech, __instance.OwningMech, __instance.SequenceGUID,
                            ModState.MeleeStates.SelectedState.AttackerDamageClusters, DamageType.Melee, ModState.MeleeStates.SelectedState.AttackAnimation);
                    }
                }

                // Target stability and unsteady - only applies to mech targets
                if (targetWasHit && __instance.MeleeTarget is Mech targetMech && !targetMech.IsProne)
                {

                    if (ModState.MeleeStates.SelectedState.TargetInstability != 0)
                    {
                        Mod.MeleeLog.Info?.Write($" -- Adding {ModState.MeleeStates.SelectedState.TargetInstability} absolute instability to target.");
                        targetMech.AddAbsoluteInstability(ModState.MeleeStates.SelectedState.TargetInstability, StabilityChangeSource.Attack, "-1");
                    }

                    if (ModState.MeleeStates.SelectedState.UnsteadyTargetOnHit)
                    {
                        Mod.MeleeLog.Info?.Write(" -- Forcing target to become unsteady from attack!");
                        targetMech.ApplyUnsteady();
                    }
                }

                // Target cluster damage - first attack was applied through melee weapon
                if (targetWasHit && ModState.MeleeStates.SelectedState.TargetDamageClusters.Length > 1 && !__instance.MeleeTarget.IsDead)
                {
                    // Make sure we use the targets's damage table
                    ModState.ForceDamageTable = ModState.MeleeStates.SelectedState.TargetTable;

                    // The target already got hit by the first cluster as the weapon damage. Only add the additional hits
                    float[] clusterDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.SubArray(1, ModState.MeleeStates.SelectedState.TargetDamageClusters.Length);
                    Mod.MeleeLog.Info?.Write($" -- Applying {clusterDamage.Sum()} damage to target as {clusterDamage.Length} clusters.");
                    AttackHelper.CreateImaginaryAttack(__instance.OwningMech, __instance.MeleeTarget, __instance.SequenceGUID, clusterDamage,
                        DamageType.Melee, ModState.MeleeStates.SelectedState.AttackAnimation);
                }

                Mod.MeleeLog.Info?.Write($"== Done.");
            }

            // Reset melee state
            ModState.MeleeStates = null;
            ModState.ForceDamageTable = DamageTable.NONE;
        }
    }

    // Attack locations are calculated when the sequence is generated. Rebuild the attack sequence before firing to ensure we use the correct dictionary
    [HarmonyPatch(typeof(MechMeleeSequence), "FireWeapons")]
    static class MechMeleeSequence_FireWeapons
    {
        static void Prefix(MechMeleeSequence __instance)
        {
            // Reset melee state
            ModState.MeleeStates = null;
            ModState.ForceDamageTable = DamageTable.NONE;

            Mod.MeleeLog.Debug?.Write("Regenerating melee support weapons hit locations...");
            Traverse BuildWeaponDirectorSequenceT = Traverse.Create(__instance).Method("BuildWeaponDirectorSequence");
            if (BuildWeaponDirectorSequenceT == null)
            {
                Mod.Log.Error?.Write($"No method named BuildWeaponDirectorSequence found - no clue what will happen next!");
                System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                Mod.Log.Info?.Write($"  Error occured at: {t}");
            }
            else
            {
                BuildWeaponDirectorSequenceT.GetValue();
                Mod.MeleeLog.Debug?.Write(" -- Done!");
            }
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "CompleteOrders")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_CompleteOrders
    {
        static void Postfix(MechMeleeSequence __instance)
        {
            Mod.MeleeLog.Trace?.Write("MMS:CO - entered.");

            // Base method checks for target knockdown. Do the same for the attacker.
            if (!__instance.OwningMech.IsDead)
            {
                __instance.OwningMech.CheckForInstability();
                __instance.OwningMech.HandleKnockdown(__instance.RootSequenceGUID, __instance.owningActor.GUID, Vector2.one, null);
            }


            // Invalidate our melee state as we're done
            ModState.MeleeStates = null;
            ModState.MeleeType = MeleeAttackType.NotSet;
            ModState.ForceDamageTable = DamageTable.NONE;
            ModState.MeleeWeapon = null;
        }
    }
}

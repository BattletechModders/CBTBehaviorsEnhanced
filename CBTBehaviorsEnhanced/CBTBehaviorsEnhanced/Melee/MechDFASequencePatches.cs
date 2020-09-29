using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using FluffyUnderware.DevTools.Extensions;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee {


    [HarmonyPatch(typeof(MechDFASequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_BuildMeleeDirectorSequence
    {
        static void Prefix(MechDFASequence __instance, List<Weapon> ___requestedWeapons)
        {
            // TODO: If this happens before the above... need to grab the selected melee type from state
            Mod.MeleeLog.Info?.Write($"Setting current melee type to: {MeleeAttackType.DFA} and weapon to: {__instance.OwningMech.DFAWeapon.UIName}");
            ModState.MeleeWeapon = __instance.OwningMech.DFAWeapon;
            ModState.MeleeType = MeleeAttackType.DFA;

            if (ModState.MeleeStates?.SelectedState != null)
            {
                // Selected state *BETTER* be DFA here
                ModState.MeleeType = ModState.MeleeStates.SelectedState.AttackAnimation;

                // Modify the owning mech DFA melee weapon to do the 'first' hit
                float targetDamage = ModState.MeleeStates.SelectedState?.TargetDamageClusters?.Length > 0 ?
                    ModState.MeleeStates.SelectedState.TargetDamageClusters[0] : 0;
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, targetDamage);
                ModState.MeleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                Mod.MeleeLog.Info?.Write($"For {CombatantUtils.Label(__instance.OwningMech)} set melee weapon damage: {targetDamage}  and instability: {ModState.MeleeStates.SelectedState.TargetInstability}");

                // Cache the attacker's original DFASelfDamage value and set it to zero, so we can apply our own damage
                ModState.OriginalDFASelfDamage = __instance.OwningMech.StatCollection.GetValue<float>(ModStats.HBS_DFA_Self_Damage);
                __instance.OwningMech.StatCollection.Set<float>(ModStats.HBS_DFA_Self_Damage, 0f);
                __instance.OwningMech.StatCollection.Set<bool>(ModStats.HBS_DFA_Causes_Self_Unsteady, false);

                // Make sure we use the target's damage table
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
    [HarmonyPatch(typeof(MechDFASequence), "ExecuteJump")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_ExecuteJump
    {
        static void Prefix(MechDFASequence __instance)
        {
            if (ModState.MeleeStates?.SelectedState != null && ModState.MeleeStates.SelectedState.AttackerInstability != 0)
            {
                Mod.MeleeLog.Info?.Write($" -- Adding {ModState.MeleeStates.SelectedState.AttackerInstability} absolute instability to attacker.");
                __instance.OwningMech.AddAbsoluteInstability(ModState.MeleeStates.SelectedState.AttackerInstability, StabilityChangeSource.Attack, "-1");
            }
        }
    }

    [HarmonyPatch(typeof(MechDFASequence), "OnMeleeComplete")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechDFASequence_OnMeleeComplete
    {
        static void Prefix(MechDFASequence __instance, MessageCenterMessage message, AttackStackSequence ___meleeSequence)
        {
            Mod.Log.Trace?.Write("MMS:OMC entered.");

            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            Mod.MeleeLog.Info?.Write($"== Resolving cluster damage, instability, and unsteady on melee attacker: {CombatantUtils.Label(__instance.OwningMech)} and " +
                $"target: {CombatantUtils.Label(__instance.DFATarget)}.");
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
                            ModState.MeleeStates.SelectedState.AttackerDamageClusters, DamageType.Melee, MeleeAttackType.Kick);
                    }
                }

                // Target stability and unsteady - only applies to mech targets
                if (targetWasHit && __instance.DFATarget is Mech targetMech && !targetMech.IsProne)
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
                if (targetWasHit && ModState.MeleeStates.SelectedState.TargetDamageClusters.Length > 1 && !__instance.DFATarget.IsDead)
                {
                    // Make sure we use the attackers's damage table
                    ModState.ForceDamageTable = ModState.MeleeStates.SelectedState.TargetTable;

                    // The target already got hit by the first cluster as the weapon damage. Only add the additional hits
                    float[] clusterDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.SubArray(1, ModState.MeleeStates.SelectedState.TargetDamageClusters.Length);
                    Mod.MeleeLog.Info?.Write($" -- Applying {clusterDamage.Sum()} damage to target as {clusterDamage.Length} clusters.");
                    AttackHelper.CreateImaginaryAttack(__instance.OwningMech, __instance.DFATarget, __instance.SequenceGUID, clusterDamage,
                        DamageType.Melee, MeleeAttackType.DFA);
                }

                Mod.MeleeLog.Info?.Write($"== Done.");
            }

            // Restore the attacker's DFA damage
            __instance.OwningMech.StatCollection.Set<float>(ModStats.HBS_DFA_Self_Damage, ModState.OriginalDFASelfDamage);
            __instance.OwningMech.StatCollection.Set<bool>(ModStats.HBS_DFA_Causes_Self_Unsteady, true);

            // Reset melee state
            ModState.MeleeStates = null;
            ModState.ForceDamageTable = DamageTable.NONE;
            ModState.OriginalDFASelfDamage = 0f;
        }
    }

    // Attack locations are calculated when the sequence is generated. Rebuild the attack sequence before firing to ensure we use the correct dictionary
    [HarmonyPatch(typeof(MechDFASequence), "FireWeapons")]
    static class MechDFASequence_FireWeapons
    {
        static void Prefix(MechDFASequence __instance)
        {
            // Reset melee state
            ModState.MeleeStates = null;
            ModState.ForceDamageTable = DamageTable.NONE;

            Mod.MeleeLog.Debug?.Write("Regenerating melee support weapons hit locations...");
            Traverse BuildWeaponDirectorSequenceT = Traverse.Create(__instance).Method("BuildWeaponDirectorSequence");
            BuildWeaponDirectorSequenceT.GetValue();
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
            ModState.MeleeStates = null;
            ModState.MeleeType = MeleeAttackType.NotSet;
            ModState.ForceDamageTable = DamageTable.NONE;
            ModState.MeleeWeapon = null;
        }
    }
}

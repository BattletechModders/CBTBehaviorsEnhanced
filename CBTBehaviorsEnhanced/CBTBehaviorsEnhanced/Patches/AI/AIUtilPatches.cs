using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AIUtil;

namespace CBTBehaviorsEnhanced.Patches.AI
{
    // Modify the melee or DFA weapon to have the correct damage, and let the normal routine apply the EV calculation for
    //  the chance to hit. Add bonus damage for evasion break or knockdown.
    [HarmonyPatch(typeof(AIUtil), "ExpectedDamageForAttack")]
    static class AIUtil_ExpectedDamageForAttack
    {
        static void Prefix(AIUtil __instance, AbstractActor unit, AttackType attackType, List<Weapon> weaponList,
            ICombatant target, Vector3 attackPosition, Vector3 targetPosition, bool useRevengeBonus, 
            AbstractActor unitForBVContext)
        {
            Mech attackingMech = unit as Mech;
            AbstractActor targetActor = target as AbstractActor;
            Mech targetMech = target as Mech;

            if (attackingMech == null || targetActor == null) return; // Nothing to do
            if (attackType == AttackType.Shooting || attackType == AttackType.None || attackType == AttackType.Count) return; // nothing to do

            try
            {
                bool modifyAttack = false;
                MeleeState meleeState = null;
                Weapon meleeWeapon = null;

                bool isCharge = false;
                bool isMelee = false;
                if (attackType == AttackType.Melee && attackingMech?.Pathing?.GetMeleeDestsForTarget(targetActor)?.Count > 0)
                {
                    Mod.MeleeLog.Info?.Write($"=== Modifying {attackingMech.DistinctId()}'s melee attack damage for utility");

                    // Create melee options
                    ModState.MeleeStates = MeleeHelper.GetMeleeStates(attackingMech, attackPosition, targetActor);
                    ModState.MeleeStates.SelectedState = ModState.MeleeStates.GetHighestTargetDamageState();
                    
                    meleeWeapon = attackingMech.MeleeWeapon;
                    modifyAttack = true;
                    isMelee = true;

                    if (ModState.MeleeStates.SelectedState == ModState.MeleeStates.Charge)
                    {
                        isCharge = true;
                    }
                }

                bool isDFA = false;
                if (attackType == AttackType.DeathFromAbove && attackingMech?.JumpPathing?.GetDFADestsForTarget(targetActor)?.Count > 0)
                {
                    Mod.MeleeLog.Info?.Write($"=== Modifying {attackingMech.DistinctId()}'s DFA attack damage for utility");

                    // Create melee options
                    ModState.MeleeStates = MeleeHelper.GetMeleeStates(attackingMech, attackPosition, targetActor);
                    ModState.MeleeStates.SelectedState = ModState.MeleeStates.DFA;
                
                    meleeWeapon = attackingMech.DFAWeapon;
                    modifyAttack = true;
                    isDFA = true;
                }

                // No pathing dests for melee or DFA - skip
                if (!isMelee && !isDFA) return;

                meleeState = ModState.MeleeStates != null ? ModState.MeleeStates.SelectedState : null;
                if (modifyAttack && meleeState == null || !meleeState.IsValid)
                {
                    Mod.MeleeLog.Info?.Write($"Failed to find a valid melee state, marking melee weapons as 1 damage.");
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, 0);
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                    return;
                }

                if (modifyAttack && meleeState != null && meleeState.IsValid)
                {
                    // Set the DFA weapon's damage to our expected damage
                    float totalDamage = meleeState.TargetDamageClusters.Sum();
                    Mod.MeleeLog.Info?.Write($" - totalDamage: {totalDamage}");

                    // Check to see if the attack will unsteady a target
                    float evasionBreakUtility = 0f;
                    if (targetMech != null && targetMech.EvasivePipsCurrent > 0 &&
                         (meleeState.UnsteadyTargetOnHit || AttackHelper.WillUnsteadyTarget(meleeState.TargetInstability, targetMech))
                       )
                    {
                        // Target will lose their evasion pips
                        evasionBreakUtility = targetMech.EvasivePipsCurrent * Mod.Config.Melee.AI.EvasionPipRemovedUtility;
                        Mod.MeleeLog.Info?.Write($"  Adding {evasionBreakUtility} virtual damage to EV from " +
                            $"evasivePips: {targetMech.EvasivePipsCurrent} x bonusDamagePerPip: {Mod.Config.Melee.AI.EvasionPipRemovedUtility}");
                    }

                    float knockdownUtility = 0f;
                    if (targetMech != null && targetMech.pilot != null &&
                        AttackHelper.WillKnockdownTarget(meleeState.TargetInstability, targetMech, meleeState.UnsteadyTargetOnHit))
                    {
                        float centerTorsoArmorAndStructure = targetMech.GetMaxArmor(ArmorLocation.CenterTorso) + targetMech.GetMaxStructure(ChassisLocations.CenterTorso);
                        if (AttackHelper.WillInjuriesKillTarget(targetMech, 1))
                        {
                            knockdownUtility = centerTorsoArmorAndStructure * Mod.Config.Melee.AI.PilotInjuryMultiUtility;
                            Mod.MeleeLog.Info?.Write($"  Adding {knockdownUtility} virtual damage to EV from " +
                                $"centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}");
                        }
                        else
                        {
                            // Attack won't kill, so only apply a fraction equal to the totalHeath 
                            float injuryFraction = (targetMech.pilot.TotalHealth - 1) - (targetMech.pilot.Injuries + 1);
                            knockdownUtility = (centerTorsoArmorAndStructure * Mod.Config.Melee.AI.PilotInjuryMultiUtility) / injuryFraction;
                            Mod.MeleeLog.Info?.Write($"  Adding {knockdownUtility} virtual damage to EV from " +
                                $"(centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}) " +
                                $"/ injuryFraction: {injuryFraction}");
                        }
                    }

                    // Check to see how much evasion loss the attacker will have
                    //  use current pips + any pips gained from movement (charge)
                    float distance = (attackPosition + unit.CurrentPosition).magnitude;
                    int newPips = unit.GetEvasivePipsResult(distance, isDFA, isCharge, true);
                    int normedNewPips = (unit.EvasivePipsCurrent + newPips) > unit.StatCollection.GetValue<int>("MaxEvasivePips") ?
                        unit.StatCollection.GetValue<int>("MaxEvasivePips") : (unit.EvasivePipsCurrent + newPips);
                    float selfEvasionDamage = 0f;
                    if (meleeState.UnsteadyAttackerOnHit || meleeState.UnsteadyAttackerOnMiss)
                    {
                        // TODO: Should evaluate chance to hit, and apply these partial damage based upon success chances
                        selfEvasionDamage = normedNewPips * Mod.Config.Melee.AI.EvasionPipLostUtility;
                        Mod.MeleeLog.Info?.Write($"  Reducing virtual damage by {selfEvasionDamage} due to potential loss of {normedNewPips} pips.");
                    }

                    // Check to see how much damage the attacker will take
                    float selfDamage = 0f;
                    if (meleeState.AttackerDamageClusters.Length > 0)
                    {
                        selfDamage = meleeState.AttackerDamageClusters.Sum();
                        Mod.MeleeLog.Info?.Write($"  Reducing virtual damage by {selfDamage} due to attacker damage on attack.");
                    }

                    float virtualDamage = totalDamage + evasionBreakUtility + knockdownUtility - selfEvasionDamage - selfDamage;
                    Mod.MeleeLog.Info?.Write($"  Virtual damage calculated as {virtualDamage} = " +
                        $"totalDamage: {totalDamage} + evasionBreakUtility: {evasionBreakUtility} + knockdownUtility: {knockdownUtility}" +
                        $" - selfDamage: {selfDamage} - selfEvasionDamage: {selfEvasionDamage}");

                    Mod.MeleeLog.Info?.Write($"Setting weapon: {meleeWeapon.UIName} to virtual damage: {virtualDamage} for EV calculation");
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, virtualDamage);
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);

                    Mod.MeleeLog.Info?.Write($"=== Done modifying attack!");
                }
                else
                {
                    Mod.MeleeLog.Debug?.Write($"Attack is not melee {modifyAttack}, or melee state is invalid or null. I assume the normal AI will prevent action.");
                }
            }
            catch (Exception e)
            {
                Mod.MeleeLog.Error?.Write(e, $"Failed to calculate melee damage for {unit.DistinctId()} using attackType {attackType} due to error!");
            }


        }

    }
}

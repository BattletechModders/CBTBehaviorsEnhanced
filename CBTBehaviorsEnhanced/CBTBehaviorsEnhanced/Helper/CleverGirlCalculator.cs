using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class CleverGirlCalculator
    {
        // Determine the best possible melee attack for a given attacker, target, and position
        //  - Usable weapons will include a MeleeWeapon/DFAWeapon with the damage set to the expected virtual damage BEFORE toHit is applied
        //     this is necessary to allow the EV calculations to proccess in CG
        //  - attackPos has to be a valid attackPosition for the target. This can be the 'safest' position as evaluated by 
        //     FindBestPositionToMeleeFrom
        public static void OptimizeMelee(Mech attacker, AbstractActor target, Vector3 attackPos, 
            List<Weapon> canFireInMeleeWeapons,
            out List<Weapon> usableWeapons, out MeleeAttack selectedState, 
            out float virtualMeleeDamage, out float totalStateDamage)
        {
            usableWeapons = new List<Weapon>();
            selectedState = null;
            virtualMeleeDamage = 0f;
            totalStateDamage = 0f;

            Mech targetMech = target as Mech;

            try
            {
                Mod.AILog.Info?.Write($"=== Optimizing melee attack for attacker: {attacker.DistinctId()} vs. " +
                    $"target: {target.DistinctId()} at attackPos: {attackPos} with {canFireInMeleeWeapons.Count} melee ranged weapons.");

                Mod.AILog.Info?.Write($"Generating melee state - see melee log.");
                MeleeState meleeStates = MeleeHelper.GetMeleeStates(attacker, attackPos, target);

                // Iterate each state, add physical and weapon damage and evaluate virtual benefit from the sum
                Mod.AILog.Info?.Write($"Iterating non-DFA melee states.");
                float highestStateDamage = 0f;
                List<MeleeAttack> allStates = new List<MeleeAttack> { meleeStates.Charge, meleeStates.Kick, meleeStates.PhysicalWeapon, meleeStates.Punch };
                foreach (MeleeAttack meleeState in allStates)
                {
                    Mod.AILog.Info?.Write($"Evaluating damage for state: {meleeState.Label}");
                    if (!meleeState.IsValid)
                    {
                        Mod.AILog.Info?.Write($" -- melee state is invalid, skipping.");
                        continue;
                    }

                    Mod.AILog.Info?.Write($"  -- Checking ranged weapons");
                    float rangedDamage = 0;
                    float rangedStab = 0;
                    float rangedHeat = 0;
                    List<Weapon> stateWeapons = new List<Weapon>();
                    foreach (Weapon weapon in canFireInMeleeWeapons)
                    {
                        if (meleeState.IsRangedWeaponAllowed(weapon) && !weapon.AOECapable)
                        {
                            stateWeapons.Add(weapon);
                            rangedDamage += weapon.DamagePerShot * weapon.ShotsWhenFired;
                            rangedDamage += weapon.StructureDamagePerShot * weapon.ShotsWhenFired;
                            rangedStab += weapon.Instability() * weapon.ShotsWhenFired;
                            rangedHeat += weapon.HeatDamagePerShot * weapon.ShotsWhenFired;
                            Mod.AILog.Info?.Write($"  weapon: {weapon.UIName} adds damage: {weapon.DamagePerShot} " +
                                $"structDam: {weapon.StructureDamagePerShot} instab: {weapon.Instability()}  heat: {weapon.HeatDamagePerShot} " +
                                $"x shots: {weapon.ShotsWhenFired}");
                        }
                    }

                    float meleeDamage = meleeState.TargetDamageClusters.Sum();
                    float stateTotalDamage = meleeDamage + rangedDamage;
                    if (targetMech != null)
                    {
                        float totalTargetStab = meleeState.TargetInstability + rangedStab;
                        Mod.AILog.Info?.Write($"  - Calculating utility based upon total projected instab of: {totalTargetStab}");

                        // Apply evasion break and knockdown utility to the melee weapon
                        float evasionBreakUtility = 0f;
                        if (targetMech != null && targetMech.EvasivePipsCurrent > 0 &&
                             (meleeState.OnTargetMechHitForceUnsteady || AttackHelper.WillUnsteadyTarget(totalTargetStab, targetMech))
                           )
                        {
                            // Target will lose their evasion pips
                            evasionBreakUtility = targetMech.EvasivePipsCurrent * Mod.Config.Melee.AI.EvasionPipRemovedUtility;
                            Mod.AILog.Info?.Write($"  Adding {evasionBreakUtility} virtual damage to EV from " +
                                $"evasivePips: {targetMech.EvasivePipsCurrent} x bonusDamagePerPip: {Mod.Config.Melee.AI.EvasionPipRemovedUtility}");
                        }

                        float knockdownUtility = 0f;
                        if (targetMech != null && targetMech.pilot != null &&
                            AttackHelper.WillKnockdownTarget(totalTargetStab, targetMech, meleeState.OnTargetMechHitForceUnsteady))
                        {
                            float centerTorsoArmorAndStructure = targetMech.GetMaxArmor(ArmorLocation.CenterTorso) + targetMech.GetMaxStructure(ChassisLocations.CenterTorso);
                            if (AttackHelper.WillInjuriesKillTarget(targetMech, 1))
                            {
                                knockdownUtility = centerTorsoArmorAndStructure * Mod.Config.Melee.AI.PilotInjuryMultiUtility;
                                Mod.AILog.Info?.Write($"  Adding {knockdownUtility} virtual damage to EV from " +
                                    $"centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}");
                            }
                            else
                            {
                                // Attack won't kill, so only apply a fraction equal to the totalHeath 
                                float injuryFraction = (targetMech.pilot.TotalHealth - 1) - (targetMech.pilot.Injuries + 1);
                                knockdownUtility = (centerTorsoArmorAndStructure * Mod.Config.Melee.AI.PilotInjuryMultiUtility) / injuryFraction;
                                Mod.AILog.Info?.Write($"  Adding {knockdownUtility} virtual damage to EV from " +
                                    $"(centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}) " +
                                    $"/ injuryFraction: {injuryFraction}");
                            }
                        }

                        // Check to see how much evasion loss the attacker will have
                        //  use current pips + any pips gained from movement (charge)
                        float distance = (attackPos + attacker.CurrentPosition).magnitude;
                        int newPips = attacker.GetEvasivePipsResult(distance, false, false, true);
                        int normedNewPips = (attacker.EvasivePipsCurrent + newPips) > attacker.StatCollection.GetValue<int>("MaxEvasivePips") ?
                            attacker.StatCollection.GetValue<int>("MaxEvasivePips") : (attacker.EvasivePipsCurrent + newPips);
                        float selfEvasionDamage = 0f;
                        if (meleeState.UnsteadyAttackerOnHit || meleeState.UnsteadyAttackerOnMiss)
                        {
                            // TODO: Should evaluate chance to hit, and apply these partial damage based upon success chances
                            selfEvasionDamage = normedNewPips * Mod.Config.Melee.AI.EvasionPipLostUtility;
                            Mod.AILog.Info?.Write($"  Reducing virtual damage by {selfEvasionDamage} due to potential loss of {normedNewPips} pips.");
                        }

                        // Check to see how much damage the attacker will take
                        float selfDamage = 0f;
                        if (meleeState.AttackerDamageClusters.Length > 0)
                        {
                            selfDamage = meleeState.AttackerDamageClusters.Sum();
                            Mod.AILog.Info?.Write($"  Reducing virtual damage by {selfDamage} due to attacker damage on attack.");
                        }

                        float virtualDamage = evasionBreakUtility + knockdownUtility - selfEvasionDamage - selfDamage;
                        Mod.AILog.Info?.Write($"  Virtual damage calculated as {virtualDamage} = " +
                            $"evasionBreakUtility: {evasionBreakUtility} + knockdownUtility: {knockdownUtility}" +
                            $" - selfDamage: {selfDamage} - selfEvasionDamage: {selfEvasionDamage}");

                        stateTotalDamage += virtualDamage;
                        // Add to melee damage as well, to so it can be set on the melee weapon
                        meleeDamage += virtualDamage;
                    }

                    if (stateTotalDamage > highestStateDamage)
                    {
                        Mod.AILog.Debug?.Write($"  State {meleeState.Label} exceeds previous state damages, adding it as highest damage state");
                        highestStateDamage = stateTotalDamage;

                        totalStateDamage = stateTotalDamage;
                        virtualMeleeDamage = meleeDamage;
                        selectedState = meleeState;

                        usableWeapons.Clear();
                        usableWeapons.AddRange(stateWeapons);
                    }
                }
                Mod.AILog.Info?.Write($"Iteration complete.");

                Mod.AILog.Info?.Write($"=== Best state for attacker: {attacker.DistinctId()} vs. " +
                    $"target: {target.DistinctId()} at attackPos: {attackPos} is state: {selectedState?.Label} with " +
                    $"virtualMeleeDamage: {virtualMeleeDamage} and totalStateDamage: {totalStateDamage}");
            } catch (Exception e)
            {
                Mod.AILog.Warn?.Write(e, $"Failed to optimize melee attack! ");
                Mod.AILog.Warn?.Write($"  Attacker: {(attacker == null ? "IS NULL" : attacker.DistinctId())}");
                Mod.AILog.Warn?.Write($"  Target: {(target == null ? "IS NULL" : target.DistinctId())}");
            }
            return;
        }
    }
}

using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Mod.AILog.Info?.Write("AITUIL_EDFA - entered.");

            if (attackingMech == null || targetActor == null) return; // Nothing to do
            if (attackType == AttackType.Shooting || attackType == AttackType.None || attackType == AttackType.Count) return; // nothing to do

            try
            {
                Mod.AILog.Info?.Write($"=== Calculating expectedDamage for {attackingMech.DistinctId()} melee attack " +
                    $"from position: {attackPosition} against target: {target.DistinctId()} at position: {targetPosition}");
                Mod.AILog.Info?.Write($"  useRevengeBonus: {useRevengeBonus}");
                Mod.AILog.Info?.Write($"  --- weaponList:");
                if (weaponList != null)
                {
                    foreach (Weapon weapon in weaponList)
                        Mod.AILog.Info?.Write($"      {weapon?.UIName}");
                }

                bool modifyAttack = false;
                MeleeState meleeState = null;
                Weapon meleeWeapon = null;

                bool isCharge = false;
                bool isMelee = false;
                if (attackType == AttackType.Melee && attackingMech?.Pathing?.GetMeleeDestsForTarget(targetActor)?.Count > 0)
                {
                    Mod.AILog.Info?.Write($"Modifying {attackingMech.DistinctId()}'s melee attack damage for utility");

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
                    Mod.AILog.Info?.Write($"Modifying {attackingMech.DistinctId()}'s DFA attack damage for utility");

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
                    Mod.AILog.Info?.Write($"Failed to find a valid melee state, marking melee weapons as 1 damage.");
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, 0);
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);
                    return;
                }

                if (modifyAttack && meleeState != null && meleeState.IsValid)
                {
                    Mod.AILog.Info?.Write($"Evaluating utility against state: {meleeState.Label}");
                    // Set the DFA weapon's damage to our expected damage
                    float totalDamage = meleeState.TargetDamageClusters.Sum();
                    Mod.AILog.Info?.Write($" - totalDamage: {totalDamage}");

                    // Check to see if the attack will unsteady a target
                    float evasionBreakUtility = 0f;
                    if (targetMech != null && targetMech.EvasivePipsCurrent > 0 &&
                         (meleeState.UnsteadyTargetOnHit || AttackHelper.WillUnsteadyTarget(meleeState.TargetInstability, targetMech))
                       )
                    {
                        // Target will lose their evasion pips
                        evasionBreakUtility = targetMech.EvasivePipsCurrent * Mod.Config.Melee.AI.EvasionPipRemovedUtility;
                        Mod.AILog.Info?.Write($"  Adding {evasionBreakUtility} virtual damage to EV from " +
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
                    float distance = (attackPosition + unit.CurrentPosition).magnitude;
                    int newPips = unit.GetEvasivePipsResult(distance, isDFA, isCharge, true);
                    int normedNewPips = (unit.EvasivePipsCurrent + newPips) > unit.StatCollection.GetValue<int>("MaxEvasivePips") ?
                        unit.StatCollection.GetValue<int>("MaxEvasivePips") : (unit.EvasivePipsCurrent + newPips);
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

                    float virtualDamage = totalDamage + evasionBreakUtility + knockdownUtility - selfEvasionDamage - selfDamage;
                    Mod.AILog.Info?.Write($"  Virtual damage calculated as {virtualDamage} = " +
                        $"totalDamage: {totalDamage} + evasionBreakUtility: {evasionBreakUtility} + knockdownUtility: {knockdownUtility}" +
                        $" - selfDamage: {selfDamage} - selfEvasionDamage: {selfEvasionDamage}");

                    Mod.AILog.Info?.Write($"Setting weapon: {meleeWeapon.UIName} to virtual damage: {virtualDamage} for EV calculation");
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, virtualDamage);
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);

                    Mod.AILog.Info?.Write($"=== Done modifying attack!");
                }
                else
                {
                    Mod.AILog.Debug?.Write($"Attack is not melee {modifyAttack}, or melee state is invalid or null. I assume the normal AI will prevent action.");
                }
            }
            catch (Exception e)
            {
                Mod.AILog.Error?.Write(e, $"Failed to calculate melee damage for {unit.DistinctId()} using attackType {attackType} due to error!");
            }
        }

        static void Postfix(AIUtil __instance, AbstractActor unit, AttackType attackType, List<Weapon> weaponList,
            ICombatant target, Vector3 attackPosition, Vector3 targetPosition, bool useRevengeBonus,
            AbstractActor unitForBVContext, float __result)
        {
            Mod.AILog.Info?.Write($"=== Expected damage of: {__result} for attacker {unit.DistinctId()} using attackType: " +
                $"{attackType} versus target: {target.DistinctId()}");
        }
    }

    [HarmonyPatch(typeof(AIUtil), "GetAcceptableHeatLevelForMech")]
    static class AIUtil_GetAcceptableHeatLevelForMech
    {
        // float GetAcceptableHeatLevelForMech(Mech mech)
        static bool Prefix(Mech mech, ref float __result)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            float bhvarAcceptableHeatFraction = UnitHelper.GetBehaviorVariableValue(mech.BehaviorTree, BehaviorVariableName.Float_AcceptableHeatLevel).FloatVal;
            Mod.AILog.Info?.Write($"== Unit: {mech.DistinctId()} has heatRiskRatio: {bhvarAcceptableHeatFraction} and currentHeat: {mech.CurrentHeat}");

            int acceptableHeat = 0;
            float heatCheckMod = mech.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);

            // TODO: Check explosion chance; volatile is essentially double chance
            AmmunitionBox mostDamagingVolatile = HeatHelper.FindMostDamagingAmmoBox(mech, true);
            if (mostDamagingVolatile != null)
            {
                Mod.AILog.Info?.Write($"-- Checking volatile ammo ");
                // We have volatile ammo, success chances will be lower because of the greater chance of an ammo explosion
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion)
                {
                    if (kvp.Value == -1f)
                    {
                        // Guaranteed explosion, return one less than this value
                        acceptableHeat = kvp.Key - 1;
                        break;
                    }

                    float rawExplosionChance = Math.Max(0f, kvp.Value - heatCheckMod);
                    Mod.AILog.Info?.Write($" -- heat: {kvp.Key} has rawChance: {kvp.Value} - pilotMod: {heatCheckMod} => raw explosionChance: {rawExplosionChance}");
                    float successChance = 1.0f - rawExplosionChance;
                    float compoundChance = successChance * successChance;
                    float finalExplosionChance = 1.0f - compoundChance;
                    Mod.AILog.Info?.Write($" -- 1.0f - compoundChance: {compoundChance} => finalExplosionChance: {finalExplosionChance}");
                    if (finalExplosionChance <= bhvarAcceptableHeatFraction)
                        acceptableHeat = kvp.Key;
                    else
                        break;
                        
                }

                sw.Stop();
                Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from volatile ammo due to behVar: {bhvarAcceptableHeatFraction}. (think time: {sw.ElapsedMilliseconds})");
                __result = acceptableHeat;
                return false;
            }

            AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(mech, false);
            if (mostDamaging != null)
            {
                Mod.AILog.Info?.Write($"-- Checking regular ammo ");
                // We have regular ammo, so success chances are as on tin
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion)
                {
                    if (kvp.Value == -1f)
                    {
                        // Guaranteed explosion, return one less than this value
                        acceptableHeat = kvp.Key - 1;
                        break;
                    }

                    float explosionChance = Math.Max(0f, kvp.Value - heatCheckMod);
                    Mod.AILog.Info?.Write($" -- heat: {kvp.Key} has rawChance: {kvp.Value} - pilotMod: {heatCheckMod} => explosionChance: {explosionChance}");
                    if (explosionChance <= bhvarAcceptableHeatFraction)
                        acceptableHeat = kvp.Key;
                    else
                        break;

                }

                sw.Stop();
                Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from ammo due to behVar: {bhvarAcceptableHeatFraction}. (think time: {sw.ElapsedMilliseconds})");
                __result = acceptableHeat;
                return false;
            }

            // TODO: Compare max move to heat; fast mechs should avoid high heat to avoid losing evasion
            // TODO: Compare heat to firing modifier; limit heat based upon modifier

            // If we've gotten this far, check shutdown
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Shutdown)
            {
                if (kvp.Value == -1f)
                {
                    // Guaranteed shutdown, return one less than this value
                    acceptableHeat = kvp.Key - 1;
                    break;
                }

                float shutdownChance = Math.Max(0f, kvp.Value - heatCheckMod);
                if (shutdownChance <= bhvarAcceptableHeatFraction)
                    acceptableHeat = kvp.Key;
                else
                    break;

            }

            sw.Stop();
            __result = acceptableHeat;
            Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from shutdown due to behVar: {bhvarAcceptableHeatFraction}. (think time: {sw.ElapsedMilliseconds})");
            return false;
        }


    }

}

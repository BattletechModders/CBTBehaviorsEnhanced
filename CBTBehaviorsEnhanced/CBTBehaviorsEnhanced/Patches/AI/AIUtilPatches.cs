using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using IRBTModUtils;
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

            try
            {
                bool modifyAttack = false;
                MeleeState meleeState = null;
                Weapon meleeWeapon = null;

                if (attackType == AttackType.Melee && attackingMech.Pathing.GetMeleeDestsForTarget(targetActor).Count > 0)
                {
                    // Create melee options
                    ModState.MeleeStates = MeleeHelper.GetMeleeStates(attackingMech, attackPosition, targetActor);
                    ModState.MeleeStates.SelectedState = ModState.MeleeStates.GetHighestTargetDamageState();
                    
                    meleeState = ModState.MeleeStates.SelectedState;
                    meleeWeapon = attackingMech.MeleeWeapon;
                    modifyAttack = true;
                    Mod.Log.Info($"Will modify {attackingMech.DistinctId()}'s melee attack damage for utility");
                }

                if (attackType == AttackType.DeathFromAbove && attackingMech.JumpPathing.GetDFADestsForTarget(targetActor).Count > 0)
                {
                    // Create melee options
                    ModState.MeleeStates = MeleeHelper.GetMeleeStates(attackingMech, attackPosition, targetActor);
                    ModState.MeleeStates.SelectedState = ModState.MeleeStates.DFA;
                    
                    meleeState = ModState.MeleeStates.SelectedState;
                    meleeWeapon = attackingMech.DFAWeapon;
                    modifyAttack = true;
                    Mod.Log.Info($"Will modify {attackingMech.DistinctId()}'s DFA attack damage for utility");
                }

                if (modifyAttack && meleeState != null && meleeState.IsValid)
                {
                    // Set the DFA weapon's damage to our expected damage
                    float totalDamage = meleeState.TargetDamageClusters.Sum();
                    Mod.Log.Info($" - totalDamage: {totalDamage}");

                    // Check to see if the attack will unsteady a target
                    float evasionBreakUtility = 0f;
                    if (targetMech != null && targetMech.EvasivePipsCurrent > 0 &&
                         (meleeState.UnsteadyTargetOnHit || AttackHelper.WillUnsteadyTarget(meleeState.TargetInstability, targetMech))
                       )
                    {
                        // Target will lose their evasion pips
                        evasionBreakUtility = targetMech.EvasivePipsCurrent * Mod.Config.Melee.AI.EvasionPipRemovedUtility;
                        Mod.Log.Info($"  Adding {evasionBreakUtility} virtual damage to EV from " +
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
                            Mod.Log.Info($"  Adding {knockdownUtility} virtual damage to EV from " +
                                $"centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}");
                        }
                        else
                        {
                            // Attack won't kill, so only apply a fraction equal to the totalHeath 
                            float injuryFraction = (targetMech.pilot.TotalHealth - 1) - (targetMech.pilot.Injuries + 1);
                            knockdownUtility = (centerTorsoArmorAndStructure * Mod.Config.Melee.AI.PilotInjuryMultiUtility) / injuryFraction;
                            Mod.Log.Info($"  Adding {knockdownUtility} virtual damage to EV from " +
                                $"(centerTorsoArmorAndStructure: {centerTorsoArmorAndStructure} x injuryMultiUtility: {Mod.Config.Melee.AI.PilotInjuryMultiUtility}) " +
                                $"/ injuryFraction: {injuryFraction}");
                        }
                    }

                    float virtualDamage = totalDamage + evasionBreakUtility + knockdownUtility;
                    Mod.Log.Info($"Setting weapon: {meleeWeapon.UIName} to virtual damage: {virtualDamage} for EV calculation");
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, virtualDamage);
                    meleeWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, 0);

                }
                else
                {
                    Mod.Log.Info($"Attack is not melee {modifyAttack}, or melee state is invalid or null. I assume the normal AI will prevent action.");
                }
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to calculate melee damage due to error!", e);
            }


        }

    }
}

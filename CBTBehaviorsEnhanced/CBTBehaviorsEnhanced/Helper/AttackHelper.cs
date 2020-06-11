using BattleTech;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class AttackHelper
    {
        public static void CreateImaginaryAttack(Mech attacker, ICombatant target, int weaponHitInfoStackItemUID, float[] damageClusters, 
            DamageType damageType, MeleeAttackType attackType)
        {
            AttackDirector.AttackSequence attackSequence = target.Combat.AttackDirector.CreateAttackSequence(0, attacker, target, 
                attacker.CurrentPosition, attacker.CurrentRotation, 0, new List<Weapon>() { attacker.ImaginaryLaserWeapon }, 
                attackType, 0, false
                );

            AttackDirection[] attackDirections = new AttackDirection[damageClusters.Length];
            WeaponHitInfo hitInfo = new WeaponHitInfo(0, attackSequence.id, 0, 0, attacker.GUID, target.GUID, 1,
                null, null, null, null, null, null, null, attackDirections, null, null, null)
            {
                attackerId = attacker.GUID,
                targetId = target.GUID,
                numberOfShots = damageClusters.Length,
                stackItemUID = weaponHitInfoStackItemUID,
                locationRolls = new float[damageClusters.Length],
                hitLocations = new int[damageClusters.Length],
                hitPositions = new Vector3[damageClusters.Length],
                hitQualities = new AttackImpactQuality[damageClusters.Length]
            };
            
            AttackDirection attackDirection = attacker.Combat.HitLocation.GetAttackDirection(attacker, target);
            Mod.Log.Info($"  Attack direction is: {attackDirection}");

            int i = 0;
            foreach (int damage in damageClusters)
            {
                // Set hit qualities
                hitInfo.attackDirections[i] = attackDirection;
                hitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                hitInfo.hitPositions[i] = attacker.CurrentPosition;

                float randomRoll = (float)Mod.Random.NextDouble();
                if (target is Mech mech)
                {
                    ArmorLocation location =
                        SharedState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, mech, randomRoll, ArmorLocation.None, 0f);
                    hitInfo.hitLocations[i] = (int)location;
                    Mod.Log.Info($"  {damage} damage to location: {location}");
                }
                else if (target is Vehicle vehicle)
                {
                    VehicleChassisLocations location =
                        SharedState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, vehicle, randomRoll, VehicleChassisLocations.None, 0f);
                    hitInfo.hitLocations[i] = (int)location;
                    Mod.Log.Info($"  {damage} damage to location: {location}");
                }
                else if (target is Turret turret)
                {
                    BuildingLocation location = BuildingLocation.Structure;
                    hitInfo.hitLocations[i] = (int)BuildingLocation.Structure;
                    Mod.Log.Info($"  {damage} damage to location: {location}");
                }

                // Make the target take weapon damage
                target.TakeWeaponDamage(hitInfo, hitInfo.hitLocations[i], attacker.ImaginaryLaserWeapon, damage, 0, 0, damageType);

                i++;
            }

            // Cleanup after myself
            target.Combat.AttackDirector.RemoveAttackSequence(attackSequence);
        }
        public static float[] CreateDamageClustersWithExtraAttacks(AbstractActor attacker, float totalDamage,
            string extraHitsCountStat)
        {
            float[] damageClusters = new float[] { };

            // Check for extra strikes
            if (attacker.StatCollection.ContainsStatistic(extraHitsCountStat) &&
                attacker.StatCollection.GetValue<float>(extraHitsCountStat) > 0)
            {
                // Round down to allow fractional extra attacks
                int extraAttacks = (int)Math.Floor(attacker.StatCollection.GetValue<float>(extraHitsCountStat));
                if (extraAttacks > 0)
                {
                    // Check for damage split
                    if (Mod.Config.Melee.ExtraHitsAverageAllDamage)
                    {
                        // Divide the damage into N hits, using the extra attacks + 1 as divisor
                        float averaged = (float)Math.Floor(totalDamage / (extraAttacks + 1));
                        Mod.Log.Info($"Adding {extraAttacks + 1 } clusters of {averaged} damage averaged from {totalDamage} damage.");
                        damageClusters = Enumerable.Repeat(averaged, extraAttacks + 1).ToArray();
                    }
                    else
                    {
                        // Each extra attack adds a new strike
                        Mod.Log.Info($"Adding {extraAttacks + 1 } clusters of {totalDamage} damage.");
                        damageClusters = Enumerable.Repeat(totalDamage, extraAttacks + 1).ToArray();
                    }
                }
            }
            else
            {
                // Target damage applies as a single attack
                damageClusters = new float[] { totalDamage };
            }

            return damageClusters;
        }

        public static bool WillInjuriesKillTarget(AbstractActor target, int numInjuries)
        {
            Mech targetMech = target as Mech;
            if (targetMech == null || targetMech.pilot == null || 
                targetMech.pilot.StatCollection.GetValue<bool>(ModStats.HBS_Ignore_Pilot_Injuries)) return false;

            Mod.Log.Info($"  Target: {target.DistinctId()} has injuries: {targetMech.pilot.Injuries} + new: {numInjuries} " +
                $"vs. totalHeath: {targetMech.pilot.TotalHealth}");

            if (targetMech.pilot.Injuries + numInjuries >= targetMech.pilot.TotalHealth) return true;
            else return false;
        }

        public static bool WillKnockdownTarget(float instability, AbstractActor target, bool attackWillUnsteady = false)
        {
            Mech targetMech = target as Mech;
            if (targetMech == null) return false;

            if (targetMech.IsOrWillBeProne || targetMech.IsBecomingProne) return false;

            float expectedStability = ExpectedInstability(instability, targetMech);
            Mod.Log.Info($"  expectedStability: {expectedStability} vs. threshold: {targetMech.UnsteadyThreshold}");

            if (expectedStability >= targetMech.MaxStability && (targetMech.IsUnsteady || attackWillUnsteady))
                return true;
            else
                return false;
        }

        public static bool WillUnsteadyTarget(float instability, AbstractActor target)
        {
            Mech targetMech = target as Mech;
            if (targetMech == null) return false;

            if (targetMech.IsUnsteady) return false;

            float expectedStability = ExpectedInstability(instability, targetMech);
            Mod.Log.Info($"  expectedStability: {expectedStability} vs. threshold: {targetMech.UnsteadyThreshold}");

            if (expectedStability >= targetMech.UnsteadyThreshold)
                return true;
            else
                return false;
        }

        private static float ExpectedInstability(float instability, Mech targetMech)
        {
            float receivedInstabMulti = targetMech.StatCollection.GetValue<float>(ModStats.HBS_Received_Instability_Multi);
            Mod.Log.Info($"Target {targetMech.DistinctId()} has ReceivedInstabilityMultiplier: {receivedInstabMulti} " +
                $"and EntrenchedMulti: {targetMech.EntrenchedMultiplier} with unsteady threshold: {targetMech.UnsteadyThreshold}");

            float instabilityDelta = instability * receivedInstabMulti * targetMech.EntrenchedMultiplier;
            Mod.Log.Info($"  instability delta => {instabilityDelta} = attack: {instability} x receivedMulti: {receivedInstabMulti} x entrenchedMulti: {targetMech.EntrenchedMultiplier}");

            float totalInstability = targetMech.CurrentStability + instabilityDelta;
            
            return totalInstability;
        }
    }

}

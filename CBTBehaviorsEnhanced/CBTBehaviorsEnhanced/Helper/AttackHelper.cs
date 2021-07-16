using BattleTech;
using CustAmmoCategories;
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

        private static void ShowDamageFloatie(Mech mech, ArmorLocation location, float damage,
            string sourceGUID)
        {
            if (mech != null && mech.GameRep != null)
            {
                Vector3 vector = mech.GameRep.GetHitPosition((int)location) + UnityEngine.Random.insideUnitSphere * 5f;
                FloatieMessage.MessageNature nature = mech.GetCurrentArmor(location) > 0f ?
                    FloatieMessage.MessageNature.ArmorDamage : FloatieMessage.MessageNature.StructureDamage;

                FloatieMessage message = new FloatieMessage(sourceGUID, mech.GUID, $"{damage}", 
                    SharedState.Combat.Constants.CombatUIConstants.floatieSizeMedium, nature,
                    vector.x, vector.y, vector.z);

                SharedState.Combat.MessageCenter.PublishMessage(message);
            }
        }
        private static void ShowDamageFloatie(Turret turret, float damage, string sourceGUID)
        {
            if (turret != null && turret.GameRep != null)
            {
                Vector3 vector = turret.GameRep.GetHitPosition((int)BuildingLocation.Structure) + UnityEngine.Random.insideUnitSphere * 5f;
                FloatieMessage.MessageNature nature = turret.GetCurrentArmor(BuildingLocation.Structure) > 0f ?
                    FloatieMessage.MessageNature.ArmorDamage : FloatieMessage.MessageNature.StructureDamage;

                FloatieMessage message = new FloatieMessage(sourceGUID, turret.GUID, $"{damage}",
                    SharedState.Combat.Constants.CombatUIConstants.floatieSizeMedium, nature,
                    vector.x, vector.y, vector.z);

                SharedState.Combat.MessageCenter.PublishMessage(message);
            }
        }

        private static void ShowDamageFloatie(Vehicle vehicle, VehicleChassisLocations location, float damage, string sourceGUID)
        {
            if (vehicle != null && vehicle.GameRep != null)
            {
                Vector3 vector = vehicle.GameRep.GetHitPosition((int)location) + UnityEngine.Random.insideUnitSphere * 5f;
                FloatieMessage.MessageNature nature = vehicle.GetCurrentArmor(location) > 0f ?
                    FloatieMessage.MessageNature.ArmorDamage : FloatieMessage.MessageNature.StructureDamage;

                FloatieMessage message = new FloatieMessage(sourceGUID, vehicle.GUID, $"{damage}",
                    SharedState.Combat.Constants.CombatUIConstants.floatieSizeMedium, nature,
                    vector.x, vector.y, vector.z);

                SharedState.Combat.MessageCenter.PublishMessage(message);
            }
        }

        public static void CreateImaginaryAttack(Mech attacker, Weapon attackWeapon, ICombatant target, int weaponHitInfoStackItemUID, float[] damageClusters, 
            DamageType damageType, MeleeAttackType attackType)
        {
            Mod.Log.Info?.Write($"  Creating imaginary attack for attacker: {attacker.DistinctId()} at position: {attacker?.CurrentPosition} and rot: {attacker?.CurrentRotation}  " +
                $"vs. target: {target.DistinctId()} at position: {target?.CurrentPosition} and rot: {target?.CurrentRotation}  " +
                $"using weapon =>  isNull: {attackWeapon == null}  name: {attackWeapon?.Name}  damageType: {damageType}  attackType: {attackType}");
            
            if (attackWeapon.ammo() == null) Mod.Log.Error?.Write($"AMMO is null!");
            if (attackWeapon.mode() == null) Mod.Log.Error?.Write($"Mode is null!");
            if (attackWeapon.exDef() == null) Mod.Log.Error?.Write($"exDef is null!");

            AttackDirector.AttackSequence attackSequence = target.Combat.AttackDirector.CreateAttackSequence(0, attacker, target, 
                attacker.CurrentPosition, attacker.CurrentRotation, 0, new List<Weapon>() { attackWeapon }, 
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
            Mod.Log.Info?.Write($"  Attack direction is: {attackDirection}");

            int i = 0;
            foreach (int damage in damageClusters)
            {
                // Set hit qualities
                hitInfo.attackDirections[i] = attackDirection;
                hitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                hitInfo.hitPositions[i] = attacker.CurrentPosition;

                float adjustedDamage = damage;
                float randomRoll = (float)Mod.Random.NextDouble();
                if (target is Mech mech)
                {
                    ArmorLocation location =
                        SharedState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, mech, randomRoll, ArmorLocation.None, 0f);
                    hitInfo.hitLocations[i] = (int)location;

                    adjustedDamage = mech.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(mech, location, adjustedDamage, hitInfo.attackerId);
                }
                else if (target is Vehicle vehicle)
                {
                    VehicleChassisLocations location =
                        SharedState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, vehicle, randomRoll, VehicleChassisLocations.None, 0f);
                    hitInfo.hitLocations[i] = (int)location;

                    adjustedDamage = vehicle.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(vehicle, location, adjustedDamage, hitInfo.attackerId);
                }
                else if (target is Turret turret)
                {
                    BuildingLocation location = BuildingLocation.Structure;
                    hitInfo.hitLocations[i] = (int)BuildingLocation.Structure;

                    adjustedDamage = turret.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(turret, adjustedDamage, hitInfo.attackerId);
                }

                // Make the target take weapon damage
                target.TakeWeaponDamage(hitInfo, hitInfo.hitLocations[i], attackWeapon, adjustedDamage, 0, 0, damageType);

                i++;
            }

            // Cleanup after myself
            target.Combat.AttackDirector.RemoveAttackSequence(attackSequence);
        }
        public static float[] CreateDamageClustersWithExtraAttacks(AbstractActor attacker, float totalDamage,
            string extraHitsCountStat)
        {
            float[] damageClusters;

            // Check for extra strikes
            if (attacker.StatCollection.ContainsStatistic(extraHitsCountStat) &&
                attacker.StatCollection.GetValue<float>(extraHitsCountStat) > 0)
            {
                // Round down to allow fractional extra attacks
                int extraAttacks = (int)Math.Floor(attacker.StatCollection.GetValue<float>(extraHitsCountStat));
                if (extraAttacks >= 1)
                {
                    // Check for damage split
                    if (Mod.Config.Melee.ExtraHitsAverageAllDamage)
                    {
                        // Divide the damage into N hits, using the extra attacks + 1 as divisor
                        float averaged = (float)Math.Floor(totalDamage / (extraAttacks + 1));
                        Mod.Log.Info?.Write($"Adding {extraAttacks + 1 } clusters of {averaged} damage averaged from {totalDamage} damage.");
                        damageClusters = Enumerable.Repeat(averaged, extraAttacks + 1).ToArray();
                    }
                    else
                    {
                        // Each extra attack adds a new strike
                        Mod.Log.Info?.Write($"Adding {extraAttacks + 1 } clusters of {totalDamage} damage.");
                        damageClusters = Enumerable.Repeat(totalDamage, extraAttacks + 1).ToArray();
                    }
                }
                else
                {
                    // Target damage applies as a single attack
                    damageClusters = new float[] { totalDamage };
                }
            }
            else
            {
                // Target damage applies as a single attack
                damageClusters = new float[] { totalDamage };
            }

            return damageClusters;
        }

        // Yes, this is stupid sloppy.
        public static ArmorLocation GetSwarmLocationForMech()
        {
            int randomIdx = Mod.Random.Next(0, Mod.Config.Melee.Swarm.MechLocationsTotalWeight);
            int currentIdx = 0;
            ArmorLocation selectedLocation = ArmorLocation.CenterTorso;
            foreach (KeyValuePair<ArmorLocation, int> kvp in Mod.Config.Melee.Swarm.MechLocations)
            {
                if (randomIdx <= currentIdx + kvp.Value)
                {
                    Mod.Log.Debug?.Write($"randomIdx: {randomIdx} is <= currentIdx: {currentIdx} + weight: {kvp.Value}, using location: {kvp.Key}");
                    selectedLocation = kvp.Key;
                    break;
                }
                else
                    currentIdx += kvp.Value;                    
            }

            Mod.Log.Debug?.Write($"Returning randomly selected swarm location: {selectedLocation}");
            return selectedLocation;
        }

        public static VehicleChassisLocations GetSwarmLocationForVehicle()
        {
            int randomIdx = Mod.Random.Next(0, Mod.Config.Melee.Swarm.VehicleLocationsTotalWeight);
            int currentIdx = 0;
            VehicleChassisLocations selectedLocation = VehicleChassisLocations.Rear;
            foreach (KeyValuePair<VehicleChassisLocations, int> kvp in Mod.Config.Melee.Swarm.VehicleLocations)
            {
                if (randomIdx <= currentIdx + kvp.Value)
                {
                    Mod.Log.Debug?.Write($"randomIdx: {randomIdx} is <= currentIdx: {currentIdx} + weight: {kvp.Value}, using location: {kvp.Key}");
                    selectedLocation = kvp.Key;
                    break;
                }
                else
                    currentIdx += kvp.Value;
            }

            Mod.Log.Debug?.Write($"Returning randomly selected swarm location: {selectedLocation}");
            return selectedLocation;
        }

        public static bool WillInjuriesKillTarget(AbstractActor target, int numInjuries)
        {
            Mech targetMech = target as Mech;
            if (targetMech == null || targetMech.pilot == null || 
                targetMech.pilot.StatCollection.GetValue<bool>(ModStats.HBS_Ignore_Pilot_Injuries)) return false;

            Mod.Log.Info?.Write($"  Target: {target.DistinctId()} has injuries: {targetMech.pilot.Injuries} + new: {numInjuries} " +
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
            Mod.Log.Info?.Write($"  expectedStability: {expectedStability} vs. threshold: {targetMech.UnsteadyThreshold}");

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
            Mod.Log.Info?.Write($"  expectedStability: {expectedStability} vs. threshold: {targetMech.UnsteadyThreshold}");

            if (expectedStability >= targetMech.UnsteadyThreshold)
                return true;
            else
                return false;
        }

        private static float ExpectedInstability(float instability, Mech targetMech)
        {
            float receivedInstabMulti = targetMech.StatCollection.GetValue<float>(ModStats.HBS_Received_Instability_Multi);
            Mod.Log.Info?.Write($"Target {targetMech.DistinctId()} has ReceivedInstabilityMultiplier: {receivedInstabMulti} " +
                $"and EntrenchedMulti: {targetMech.EntrenchedMultiplier} with unsteady threshold: {targetMech.UnsteadyThreshold}");

            float instabilityDelta = instability * receivedInstabMulti * targetMech.EntrenchedMultiplier;
            Mod.Log.Info?.Write($"  instability delta => {instabilityDelta} = attack: {instability} x receivedMulti: {receivedInstabMulti} x entrenchedMulti: {targetMech.EntrenchedMultiplier}");

            float totalInstability = targetMech.CurrentStability + instabilityDelta;
            
            return totalInstability;
        }
    }

}

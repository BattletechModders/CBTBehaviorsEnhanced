using CustAmmoCategories;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public static void CreateImaginaryAttack(Mech attacker, Weapon attackWeapon, ICombatant target,
            int weaponHitInfoStackItemUID, float[] damageClusters,
            DamageType damageType, MeleeAttackType meleeAttackType)
        {
            Mod.Log.Warn?.Write($"DOING IMAGINARY ATTACK FOR: {attacker.DistinctId()} USING: {attackWeapon.Name}_{attackWeapon.uid}");

            Mod.Log.Info?.Write($"  Creating imaginary attack for attackType: {meleeAttackType}  damageType: {damageType}");
            Mod.Log.Info?.Write($"    attacker: {attacker.DistinctId()} => currentPos: {attacker?.CurrentPosition} currentRot: {attacker?.CurrentRotation}");
            Mod.Log.Info?.Write($"    target: {target.DistinctId()} => currentPos: {target?.CurrentPosition} currentRot: {target?.CurrentRotation}  targetPos: {target?.TargetPosition}");
            Mod.Log.Info?.Write($"    damageClusters.Length: {damageClusters.Length}");
            Mod.Log.Info?.Write($"    weapon =>  isNull: {attackWeapon == null}  name: {attackWeapon?.Name}  uid: {attackWeapon?.uid}  " +
                $"ammo.IsNull: {attackWeapon.ammo() == null}  mode.IsNull: {attackWeapon.mode() == null}  exDef.IsNull: {attackWeapon.exDef() == null}  " +
                $"indirectFireCapable: {attackWeapon.IndirectFireCapable}");

            // Prepare the weapon. Reset it (avoids the PreFire error), and set the ShotsWhenFired to the cluster size
            attackWeapon.ResetWeapon();
            attackWeapon.StatCollection.Set<int>("ShotsWhenFired", damageClusters.Length);
            List<Weapon> attackWeapons = new List<Weapon>() { attackWeapon };

            AttackDirector.AttackSequence attackSequence = target.Combat.AttackDirector.CreateAttackSequence(
                stackItemUID: 0, attacker: attacker, target: target,
                attackPosition: attacker.CurrentPosition, attackRotation: attacker.CurrentRotation,
                attackSequenceIdx: 0, selectedWeapons: attackWeapons,
                meleeAttackType: meleeAttackType, calledShotLocation: 0, isMoraleAttack: false
                );
            Mod.Log.Debug?.Write("  -- created attack sequence");

            WeaponHitInfo?[][] sequenceHitInfos = attackSequence.weaponHitInfo;
            Mod.Log.Debug?.Write("  -- fetched weaponHitInfo from attack Sequence");
            if (sequenceHitInfos != null)
            {
                Mod.Log.Debug?.Write($"  ---- sequence has: {sequenceHitInfos.Length} groups");
                Mod.Log.Debug?.Write($"  ---- group has : {sequenceHitInfos[0].Length} weapons");
            }
            WeaponHitInfo hitInfo = sequenceHitInfos[0][0].GetValueOrDefault();

            AttackDirection attackDirection = attacker.Combat.HitLocation.GetAttackDirection(attacker, target);
            Mod.Log.Info?.Write($"  Attack direction is: {attackDirection}");

            int i = 0;

            if (ModState.ForceDamageTable != MeleeStates.DamageTable.NONE)
            {
                // If we're using a kick/punch table, push the table into CU to use with unconventional hit locations (helis, vtols, etc)
                Mod.Log.Info?.Write($"  --- setting attack table: {ModState.ForceDamageTable}");
                Thread.CurrentThread.pushToStack<string>(ModConsts.SPECIAL_HIT_TABLE_NAME, $"CBTBE_MELEE_{ModState.ForceDamageTable}");            
            }

            foreach (int damage in damageClusters)
            {
                Mod.Log.Debug?.Write($"  ---- hitInfo.attackDirections => length: {hitInfo.attackDirections.Length}");
                Mod.Log.Debug?.Write($"  ---- hitInfo.hitQualities => length: {hitInfo.hitQualities.Length}");
                Mod.Log.Debug?.Write($"  ---- hitInfo.hitPositions => length: {hitInfo.hitPositions.Length}");
                Mod.Log.Debug?.Write($"  ---- hitInfo.hitLocations => length: {hitInfo.hitLocations.Length}");

                // Set hit qualities
                hitInfo.attackDirections[i] = attackDirection;
                hitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                hitInfo.hitPositions[i] = attacker.CurrentPosition;

                float adjustedDamage = damage;
                float randomRoll = (float)Mod.Random.NextDouble();
                if (target is Mech mech)
                {
                    ArmorLocation location =
                        mech.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, mech, randomRoll, ArmorLocation.None, 0f);
                    Mod.Log.Debug?.Write($"  ---- generated attack location: {location}");
                    hitInfo.hitLocations[i] = (int)location;

                    adjustedDamage = mech.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(mech, location, adjustedDamage, hitInfo.attackerId);
                }
                else if (target is Vehicle vehicle)
                {
                    VehicleChassisLocations location =
                        vehicle.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, vehicle, randomRoll, VehicleChassisLocations.None, 0f);
                    Mod.Log.Debug?.Write($"  ---- generated attack location: {location}");
                    hitInfo.hitLocations[i] = (int)location;

                    adjustedDamage = vehicle.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(vehicle, location, adjustedDamage, hitInfo.attackerId);
                }
                else if (target is Turret turret)
                {
                    BuildingLocation location = BuildingLocation.Structure;
                    Mod.Log.Debug?.Write($"  ---- generated attack location: {location}");
                    hitInfo.hitLocations[i] = (int)BuildingLocation.Structure;
                    
                    adjustedDamage = turret.GetAdjustedDamageForMelee(damage, attackWeapon.WeaponCategoryValue);
                    Mod.Log.Info?.Write($"  {adjustedDamage} damage to location: {location}");
                    ShowDamageFloatie(turret, adjustedDamage, hitInfo.attackerId);
                }

                // Make the target take weapon damage
                target.TakeWeaponDamage(hitInfo, hitInfo.hitLocations[i], attackWeapon, adjustedDamage, 0, 0, damageType);

                i++;
            }

            if (ModState.ForceDamageTable != MeleeStates.DamageTable.NONE)
            {
                Mod.Log.Info?.Write($"  --- reverting attack table");
                Thread.CurrentThread.popFromStack<string>(ModConsts.SPECIAL_HIT_TABLE_NAME);
            }
            Mod.Log.Debug?.Write("  -- done with damage cluster iteration");

            // Cleanup after myself
            target.Combat.AttackDirector.RemoveAttackSequence(attackSequence);
            Mod.Log.Debug?.Write("  -- removed attack sequence");
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
                        Mod.Log.Info?.Write($"Adding {extraAttacks + 1} clusters of {averaged} damage averaged from {totalDamage} damage.");
                        damageClusters = Enumerable.Repeat(averaged, extraAttacks + 1).ToArray();
                    }
                    else
                    {
                        // Each extra attack adds a new strike
                        Mod.Log.Info?.Write($"Adding {extraAttacks + 1} clusters of {totalDamage} damage.");
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

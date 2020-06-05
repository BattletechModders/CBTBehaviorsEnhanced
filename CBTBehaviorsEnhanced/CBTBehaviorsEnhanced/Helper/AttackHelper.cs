using BattleTech;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class AttackHelper
    {
        public static void CreateImaginaryAttack(Mech attacker, ICombatant target, int weaponHitInfoStackItemUID, float[] damageClusters)
        {
            AttackDirector.AttackSequence attackSequence = target.Combat.AttackDirector.CreateAttackSequence(0, attacker, target, 
                attacker.CurrentPosition, attacker.CurrentRotation, 0, new List<Weapon>() { attacker.ImaginaryLaserWeapon }, 
                MeleeAttackType.NotSet, 0, false
                );

            AttackDirection[] attackDirections = new AttackDirection[] { };
            WeaponHitInfo hitInfo = new WeaponHitInfo(0, attackSequence.id, 0, 0, attacker.GUID, target.GUID, 1,
                null, null, null, null, null, null, null, attackDirections, null, null, null)
            {
                attackerId = attacker.GUID,
                targetId = target.GUID,
                numberOfShots = attacker.ImaginaryLaserWeapon.ShotsWhenFired,
                stackItemUID = weaponHitInfoStackItemUID,
                locationRolls = new float[damageClusters.Length],
                hitLocations = new int[damageClusters.Length],
                hitPositions = new Vector3[damageClusters.Length],
                hitQualities = new AttackImpactQuality[damageClusters.Length]
            };
            
            AttackDirection attackDirection = attacker.Combat.HitLocation.GetAttackDirection(attacker, target);

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
                        ModState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, mech, randomRoll, ArmorLocation.None, 0f);
                    hitInfo.hitLocations[i] = (int)location;
                    Mod.Log.Info($"  {damage} damage to location: {location}");
                }
                else if (target is Vehicle vehicle)
                {
                    VehicleChassisLocations location = 
                        ModState.Combat.HitLocation.GetHitLocation(attacker.CurrentPosition, vehicle, randomRoll, VehicleChassisLocations.None, 0f);
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
                target.TakeWeaponDamage(hitInfo, hitInfo.hitLocations[i], attacker.ImaginaryLaserWeapon, damage, 0, 0, DamageType.Melee);

                i++;
            }
        }

    }
}

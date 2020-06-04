using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;
using us.frostraptor.modUtils.CustomDialog;

namespace CBTBehaviorsEnhanced.Piloting {

    [HarmonyPatch(typeof(MechFallSequence), "OnAdded")]
    public class MechFallSequence_OnAdded {
        public static void Postfix(MechFallSequence __instance) {
            Mod.Log.Trace("MFS:OnAdded - entered.");
            QuipHelper.PublishQuip(__instance.OwningMech, Mod.LocalizedText.Qips.Knockdown);            
        }
    }

    [HarmonyPatch(typeof(Mech), "CompleteKnockdown")]
    public class Mech_CompleteKnockdown {
        public static void Prefix(Mech __instance) {
            Mod.Log.Trace("M:CK - entered.");
        }
    }

    // In TT mechs take damage from falling. In BTG only he pilot takes damage. Create a new attack sequence and apply
    //   the TT rules for falling damage
    [HarmonyPatch(typeof(MechFallSequence), "OnComplete")]
    public class MechFallSequence_OnComplete {
        public static void Prefix(MechFallSequence __instance) {
            Mod.Log.Trace("MFS:OnComplete - entered.");
            int damagePointsTT = (int)Math.Ceiling(__instance.OwningMech.tonnage / 10f);
            Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} will suffer {damagePointsTT} TT damage points.");

            // Check for any pilot skill damage reduction
            float damageReduction = 1.0f - __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.DFAReductionMulti);
            float reducedDamage = (float)Math.Max(0f, Math.Floor(damageReduction * damagePointsTT));
            Mod.Log.Debug($" Reducing TT fall damage from: {damagePointsTT} by {damageReduction:P1} to {reducedDamage}");

            List<int> locationDamage = new List<int>();
            while (damagePointsTT >= 5) {
                locationDamage.Add(5 * Mod.Config.Piloting.FallingDamagePerTenTons);
                damagePointsTT -= 5;
            }
            if (damagePointsTT > 0) {
                locationDamage.Add(damagePointsTT * Mod.Config.Piloting.FallingDamagePerTenTons);
            }

            Mod.Log.Info($"Applying TT damage: {damagePointsTT} => {damagePointsTT * Mod.Config.Piloting.FallingDamagePerTenTons} damage falling damage to actor: {CombatantUtils.Label(__instance.OwningMech)}");

            AttackDirector.AttackSequence attackSequence = __instance.OwningMech.Combat.AttackDirector.CreateAttackSequence(0, __instance.OwningMech, __instance.OwningMech,
                __instance.OwningMech.CurrentPosition, __instance.OwningMech.CurrentRotation, 0, new List<Weapon>() { __instance.OwningMech.ImaginaryLaserWeapon }, MeleeAttackType.NotSet, 0, false
                );
            WeaponHitInfo hitInfo = new WeaponHitInfo(0, attackSequence.id, 0, 0, __instance.OwningMech.GUID, __instance.OwningMech.GUID, 1,
                null, null, null, null, null, null, null, new AttackDirection[] { AttackDirection.FromFront }, null, null, null)
            {
                attackerId = __instance.OwningMech.GUID,
                targetId = __instance.OwningMech.GUID,
                numberOfShots = __instance.OwningMech.ImaginaryLaserWeapon.ShotsWhenFired,
                stackItemUID = __instance.SequenceGUID,
                locationRolls = new float[locationDamage.Count],
                hitLocations = new int[locationDamage.Count],
                hitPositions = new Vector3[locationDamage.Count],
                hitQualities = new AttackImpactQuality[locationDamage.Count]
            };
            AttackDirection attackDirection = __instance.OwningMech.Combat.HitLocation.GetAttackDirection(__instance.OwningMech.CurrentPosition, __instance.OwningMech);

            int i = 0;
            foreach (int damage in locationDamage) {
                ArmorLocation location = FallingDamageLocations[__instance.OwningMech.Combat.NetworkRandom.Int(0, FallingDamageLocations.Length)];
                Mod.Log.Info($"  {damage} damage to location: {location}");

                hitInfo.attackDirections[i] = attackDirection;
                hitInfo.hitQualities[i] = AttackImpactQuality.Solid;
                hitInfo.hitPositions[i] = __instance.OwningMech.CurrentPosition;

                __instance.OwningMech.TakeWeaponDamage(hitInfo, (int)location, __instance.OwningMech.ImaginaryLaserWeapon, damage, 0, 0, DamageType.KnockdownSelf);

                i++;
            }
        }

        private static readonly ArmorLocation[] FallingDamageLocations = new[] {
            ArmorLocation.Head,
            ArmorLocation.CenterTorso,
            ArmorLocation.CenterTorsoRear,
            ArmorLocation.LeftTorso,
            ArmorLocation.LeftTorsoRear,
            ArmorLocation.RightTorso,
            ArmorLocation.RightTorsoRear,
            ArmorLocation.LeftArm,
            ArmorLocation.RightArm,
            ArmorLocation.RightLeg,
            ArmorLocation.LeftLeg
        };
    }
}

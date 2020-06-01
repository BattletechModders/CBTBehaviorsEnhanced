
using BattleTech;
using Harmony;
using System;

namespace CBTBehaviorsEnhanced {

    // TODO: Should use the delta across the entire attack sequence, not just one weapon
    public static class PilotingPatches {

        [HarmonyPatch(typeof(Mech), "ResolveWeaponDamage", new Type[] { typeof(WeaponHitInfo), typeof(Weapon), typeof(MeleeAttackType) })]
        public static class Mech_ResolveWeaponDamage {
            public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, Weapon weapon, MeleeAttackType meleeAttackType) {
                Mod.Log.Trace("M:RWD entered.");

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(hitInfo.attackSequenceId);
                AbstractActor target = __instance.Combat.FindActorByGUID(hitInfo.targetId);
                if (target is Mech targetMech) {

                    // Feature: Piloting Skill Check from instability
                    // TODO: Let instability represent this?
                    CombatResolutionConstantsDef crcd = target.Combat.Constants.ResolutionConstants;
                    float stabilityDamage = hitInfo.ConsolidateInstability(hitInfo.targetId, weapon.Instability(),
                        crcd.GlancingBlowDamageMultiplier, crcd.NormalBlowDamageMultiplier, crcd.SolidBlowDamageMultiplier);
                    stabilityDamage *= __instance.StatCollection.GetValue<float>("ReceivedInstabilityMultiplier");
                    stabilityDamage *= __instance.EntrenchedMultiplier;
                    MechHelper.PilotCheckOnInstabilityDamage(targetMech, stabilityDamage);
                }
            }
        }

    }
}

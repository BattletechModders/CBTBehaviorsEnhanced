
using BattleTech;
using Harmony;
using System;

namespace CBTBehaviorsEnhanced {

    public static class PilotingPatches {

        [HarmonyPatch(typeof(Mech), "ResolveWeaponDamage", new Type[] { typeof(WeaponHitInfo), typeof(Weapon), typeof(MeleeAttackType) })]
        public static class Mech_ResolveWeaponDamage {
            public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, Weapon weapon, MeleeAttackType meleeAttackType) {
                Mod.Log.Trace("M:RWD entered.");

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(hitInfo.attackSequenceId);
                AbstractActor actor = __instance.Combat.FindActorByGUID(hitInfo.targetId);
                if (actor is Mech target) {

                    CombatResolutionConstantsDef crcd = __instance.Combat.Constants.ResolutionConstants;
                    float stabilityDamage = hitInfo.ConsolidateInstability(hitInfo.targetId, weapon.Instability(),
                        crcd.GlancingBlowDamageMultiplier, crcd.NormalBlowDamageMultiplier, crcd.SolidBlowDamageMultiplier);

                    stabilityDamage *= __instance.StatCollection.GetValue<float>("ReceivedInstabilityMultiplier");
                    stabilityDamage *= __instance.EntrenchedMultiplier;

                    Mod.Log.Debug($" == Checking Piloting Stability");
                    Mod.Log.Debug($"   target:{CombatantHelper.LogLabel(target)} isMech:{(actor is Mech)} IsDead:{target.IsDead} IsUnsteady:{target.IsUnsteady} IsOrWillBeProne:{target.IsOrWillBeProne}");
                    Mod.Log.Debug($"   weapon stability damage:{stabilityDamage}");
                    
                    if (stabilityDamage > 0 && !target.IsDead && target.IsUnsteady && !target.IsOrWillBeProne) {
                        float skillBonus = (float)target.SkillPiloting / __instance.Combat.Constants.PilotingConstants.PilotingDivisor;

                        float skillRoll = __instance.Combat.NetworkRandom.Float();
                        float skillTotal = skillRoll + skillBonus;

                        Mod.Log.Debug($" Skill check -> bonus: {skillBonus}  roll: {skillRoll}  rollTotal: {skillTotal}  target:{Mod.Config.PilotStabilityCheck}");
                        
                        if (skillTotal < Mod.Config.PilotStabilityCheck) {
                            Mod.Log.Debug(string.Format(" Skill Check Failed! Flagging for Knockdown"));
                            bool showMessage = !target.IsFlaggedForKnockdown;

                            target.FlagForKnockdown();
                            if (Mod.Config.ShowAllStabilityRolls || showMessage)
                            {
                                target.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(target, $"Stability Check: Failed!", FloatieMessage.MessageNature.Debuff, true)));
                            }
                        } else {
                            Mod.Log.Debug(string.Format(" Skill Check Succeeded!"));
                            if (Mod.Config.ShowAllStabilityRolls)
                            {
                                target.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(target, $"Stability Check: Passed!", FloatieMessage.MessageNature.Buff, true)));
                            }
                        }
                    } else {
                        Mod.Log.Debug($"  target has no stability damage, is not unsteady, or is dead or prone - skipping");
                    }

                }
            }
        }

    }
}

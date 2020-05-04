﻿
using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced {

    public static class MovementPatches {

        [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
        public static class ToHit_GetAllModifiers {
            private static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
                Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot) {
                Mod.Log.Trace("TH:GAM entered");

                if (attacker.HasMovedThisRound && attacker.JumpedLastRound && 
                    // Special trigger for dz's abilities
                    !(ModConfig.dZ_Abilities && attacker.SkillTactics != 10)) {
                    __result = __result + (float)Mod.Config.ToHitSelfJumped;
                }
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
        public static class ToHit_GetAllModifiersDescription {
            private static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
                Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot) {
                Mod.Log.Trace("TH:GAMD entered");

                if (attacker.HasMovedThisRound && attacker.JumpedLastRound) {
                    __result = string.Format("{0}JUMPED {1:+#;-#}; ", __result, Mod.Config.ToHitSelfJumped);
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
        public static class CombatHUDWeaponSlot_SetHitChance {

            private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD) {

                if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) { return; }

                Mod.Log.Trace("CHUDWS:SHC entered");

                AbstractActor actor = __instance.DisplayedWeapon.parent;
                var _this = Traverse.Create(__instance);

                if (actor.HasMovedThisRound && actor.JumpedLastRound) {
                    Traverse addToolTipDetailT = Traverse.Create(__instance).Method("AddToolTipDetail", "JUMPED SELF", Mod.Config.ToHitSelfJumped);
                    Mod.Log.Trace($"Invoking addToolTipDetail for: JUMPED SELF = {Mod.Config.ToHitSelfJumped}");
                    addToolTipDetailT.GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(ActorMovementSequence), "OnComplete")]
        public static class ActorMovementSequence_OnComplete {
            private static void Prefix(ActorMovementSequence __instance) {
                Mod.Log.Trace("AMS:OC entered");
                // Interleaved - check for visibility to any enemies 
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved) {
                    if (__instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0) {
                        Mod.Log.Info("AMS:OC TD is not interleaved but enemies are detected - disabling autobrace. ");
                        __instance.owningActor.AutoBrace = false;
                    } else {
                        Mod.Log.Info("AMS:OC TD is not interleaved and no enemies - autobracing ");
                        __instance.owningActor.AutoBrace = true;
                    }
                }

                // Movement - check for damage after a sprint, and if so force a piloting check
                if (__instance.OwningMech != null && __instance.isSprinting && __instance.OwningMech.ActuatorDamageMalus() != 0) {
                    Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Move.SkillMulti);
                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterRunChance, __instance.OwningMech, sourceSkillMulti, ModConfig.FT_Fall_After_Run);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after sprinting with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Fall_After_Run);
                    }
                }
            }
        }

        // Prevents a mech from being able to move into combat or use abilities from non-interleaved mode
        [HarmonyPatch(typeof(ActorMovementSequence), "ConsumesFiring", MethodType.Getter)]
        public static class ActorMovementSequence_ConsumesFiring_Getter {
            private static void Postfix(ActorMovementSequence __instance, ref bool __result) {
                Mod.Log.Trace("AMS:CF:GET entered");
                if (!__instance.OwningActor.Combat.TurnDirector.IsInterleaved) 
                {
                    // We want to auto-brace, and auto-brace requires that consumesFiring = false. So when no enemies are around, don't consume firing so 
                    //   that we can auto-brace
                    if (__instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0)
                    {
                        __result = true;
                    }
                    else
                    {
                        __result = false;
                    }
                }
            }
        }


        // Prevents a mech from being able to jump into combat from non-interleaved mode
        [HarmonyPatch(typeof(MechJumpSequence), "ConsumesFiring", MethodType.Getter)]
        public static class MechJumpSequence_ConsumesFiring_Getter {
            private static void Postfix(MechJumpSequence __instance, ref bool __result) {
                Mod.Log.Trace("AMS:CF:GET entered");
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved) {
                    // We want to auto-brace, and auto-brace requires that consumesFiring = false. So when no enemies are around, don't consume firing so 
                    //   that we can auto-brace
                    if (__instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0)
                    {
                        __result = true;
                    }
                    else
                    {
                        __result = false;
                    }
                    
                }
            }
        }


        [HarmonyPatch(typeof(MechJumpSequence), "OnComplete")]
        public static class MechJumpSequence_OnComplete {
            private static void Prefix(MechJumpSequence __instance) {
                Mod.Log.Trace("MJS:OC entered");

                // Check for visibility to any enemies
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved)
                {
                    if (__instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0)
                    {
                        Mod.Log.Debug("MJS:OC TD is not interleaved but enemies are detected - disabling autobrace. ");
                        __instance.owningActor.AutoBrace = false;
                    }
                    else
                    {
                        Mod.Log.Debug("MJS:OC is not interleaved and no enemies - autobracing ");
                        __instance.owningActor.AutoBrace = true;
                    }
                }

                Mod.Log.Trace($"JUMP -- ABILITY_CONSUMES_FIRING: {__instance.AbilityConsumesFiring} / CONSUMES_FIRING: {__instance.ConsumesFiring}");

                // Movement - check for damage after a jump, and if so force a piloting check
                if (__instance.OwningMech != null && __instance.OwningMech.ActuatorDamageMalus() != 0) {
                    Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Move.SkillMulti);
                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterRunChance, __instance.OwningMech, sourceSkillMulti, ModConfig.FT_Fall_After_Jump);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after jumping with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Fall_After_Jump);
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(Mech), "ApplyBraced")]
        //public static class Mech_ApplyBraced
        //{
        //    public static void Prefix(Mech __instance)
        //    {
        //        Mod.Log.Trace($" BRACING UNIT: {CombatantUtils.Label(__instance)}");
        //    }
        //}

        //[HarmonyPatch(typeof(AbstractActor), "DoneWithActor")]
        //public static class AbstractActor_DoneWithActor
        //{
        //    public static void Prefix(Mech __instance)
        //    {
        //        Mod.Log.Trace($" UNIT: {CombatantUtils.Label(__instance)} hasFiredThisRound: {__instance.HasFiredThisRound}  hasSprintedThisRound: {__instance.HasSprintedThisRound}  isAttacking: {__instance.IsAttacking}");
        //    }
        //}

        // Prevents losing evasion when attacked
        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence", null)]
        public static class AbstractActor_ResolveAttackSequence_Patch {
            
            private static bool Prefix(AbstractActor __instance) {
                Mod.Log.Trace("AA:RAS:PRE entered");
                return !ModConfig.EnablePermanentEvasion;
            }

            private static void Postfix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID, AttackDirection attackDirection) {
                Mod.Log.Trace("AA:RAS:POST entered");
                if (!ModConfig.EnablePermanentEvasion) { return; }

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(sequenceID);
                if (attackSequence != null) {
                    if (!attackSequence.GetAttackDidDamage(__instance.GUID)) {
                        return;
                    }
                    List<Effect> list = __instance.Combat.EffectManager
                        .GetAllEffectsTargeting(__instance)
                        .FindAll((Effect x) => x.EffectData.targetingData.effectTriggerType == EffectTriggerType.OnDamaged);
                    
                    for (int i = 0; i < list.Count; i++) {
                        list[i].OnEffectTakeDamage(attackSequence.attacker, __instance);
                    }
                    
                    if (attackSequence.isMelee) {
                        int value = attackSequence.attacker.StatCollection.GetValue<int>(ModStats.MeleeHitPushBackPhases);
                        if (value > 0) {
                            for (int j = 0; j < value; j++) {
                                __instance.ForceUnitOnePhaseDown(sourceID, stackItemID, false);
                            }
                        }
                    }
                }
            }
        }
    }
}

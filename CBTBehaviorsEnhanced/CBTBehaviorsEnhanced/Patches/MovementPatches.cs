
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

        [HarmonyPatch(typeof(OrderSequence), "OnUpdate")]
        public static class OrderSequence_OnUpdate
        {
            public static void Prefix(OrderSequence __instance)
            {
                Mod.Log.Debug($"OS:OU - entered for actor: {CombatantUtils.Label(__instance?.owningActor)}");
                if (__instance == null) return;

                Traverse sequenceIsCompleteT = Traverse.Create(__instance).Property("sequenceIsComplete");
                bool sequenceIsComplete = sequenceIsCompleteT.GetValue<bool>();
                bool baseIsComplete = __instance.childSequences.Count == 0 || (__instance.childSequences.Count == 1 && __instance.cameraSequence != null && __instance.cameraSequence.IsFinished);
                Mod.Log.Debug($"  ordersAreComplete: {__instance.OrdersAreComplete}  self.isComplete: {__instance.IsComplete}  base.isComplete: {baseIsComplete}  " +
                    $"#childSequences: {__instance?.childSequences?.Count}  cameraSequence: {__instance?.cameraSequence?.IsFinished}  sequenceIsComplete: {sequenceIsComplete}");
                if (__instance.ChildSequenceCount > 0)
                {
                    foreach (IStackSequence seq in __instance.childSequences)
                    {
                        Mod.Log.Debug($" -- child sequence  type: {seq.GetType()}  msgIdx: {seq.MessageIndex}");
                    }
                }

                if (__instance.OrdersAreComplete && baseIsComplete && !sequenceIsComplete)
                {
                    Mod.Log.Debug($"OrderSequence consumesActivation: {__instance.ConsumesActivation}");
                    if (__instance.ConsumesActivation)
                    {
                        DoneWithActorSequence dwaSeq = __instance as DoneWithActorSequence;
                        Mech mech = __instance.owningActor as Mech;
                        Mod.Log.Debug($" OwningActor: {CombatantUtils.Label(__instance.owningActor)} is " +
                            $"mech: {mech != null}  isShutdown: {mech?.IsShutDown}  " +
                            $"doneWithActorSequence isNotNull: {dwaSeq != null}  " +
                            $"isInterleaved: {__instance?.owningActor?.Combat?.TurnDirector?.IsInterleaved}  " +
                            $"detectedEnemyUnits: {__instance?.owningActor?.Combat?.LocalPlayerTeam?.GetDetectedEnemyUnits()?.Count}");

                        if (mech != null && !mech.IsShutDown && dwaSeq != null && 
                            !mech.Combat.TurnDirector.IsInterleaved &&
                            mech.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count == 0)
                        {
                            Mod.Log.Debug($"Creating heat sequence for mech.");
                            // By default OrderSequence:OnUpdate doesn't apply a MechHeatSequence if you are in non-interleaved mode. Why? I don't know. Force it to add one here.
                            MechHeatSequence heatSequence = mech.GenerateEndOfTurnHeat(__instance);
                            if (heatSequence != null)
                            {
                                Mod.Log.Debug($"  Done, adding sequence: {heatSequence.SequenceGUID} to instance.");
                                __instance.AddChildSequence(heatSequence, __instance.MessageIndex);
                            }
                            else
                            {
                                Mod.Log.Debug($" HEAT SEQUENCE IS NULL - PROBABLY AN ERROR!");
                            }
                        }
                        else 
                        {
                            Mod.Log.Debug("HEAT SEQUENCE CONDITIONAL FAILED");
                        }
                    }
                }
                
            }
        }

        [HarmonyPatch(typeof(ActorMovementSequence), "OnComplete")]
        public static class ActorMovementSequence_OnComplete {
            private static void Prefix(ActorMovementSequence __instance) {
                Mod.Log.Debug($"AMS:OC entered for actor: {CombatantUtils.Label(__instance?.OwningActor)}");

                // Interleaved - check for visibility to any enemies 
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved) {
                    __instance.owningActor.AutoBrace = true;                    
                }

                // Movement - check for damage after a sprint, and if so force a piloting check
                if (__instance.OwningMech != null && __instance.isSprinting && __instance.OwningMech.ActuatorDamageMalus() != 0) {
                    Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Move.SkillMulti);
                    float damagePenalty = __instance.OwningMech.ActuatorDamageMalus() * Mod.Config.Move.SkillMulti;
                    float checkMod = sourceSkillMulti + damagePenalty;
                    Mod.Log.Debug($"  moveSkillMulti:{sourceSkillMulti} - damagePenalty: {damagePenalty} = checkMod: {checkMod}");

                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterRunChance, __instance.OwningMech, checkMod, ModText.FT_Fall_After_Run);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after sprinting with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Fall_After_Run);
                    }
                }
            }

            static void Postfix(ActorMovementSequence __instance)
            {
                Mod.Log.Debug($"AMS:OC:post - actor: {CombatantUtils.Label(__instance.OwningActor)} " +
                    $"autoBrace: {__instance.OwningActor.AutoBrace}  hasFired: {__instance.OwningActor.HasFiredThisRound}  consumesFiring: {__instance.ConsumesFiring}");
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
                    __result = false;
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
                    __result = false;
                }
            }
        }


        [HarmonyPatch(typeof(MechJumpSequence), "OnComplete")]
        public static class MechJumpSequence_OnComplete {
            private static void Prefix(MechJumpSequence __instance) {
                Mod.Log.Debug($"MJS:OC entered for actor: {CombatantUtils.Label(__instance.OwningMech)}");

                // Check for visibility to any enemies
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved)
                {                
                    Mod.Log.Debug("MJS:OC is not interleaved and no enemies - autobracing ");
                    __instance.owningActor.AutoBrace = true;
                }

                Mod.Log.Trace($"JUMP -- ABILITY_CONSUMES_FIRING: {__instance.AbilityConsumesFiring} / CONSUMES_FIRING: {__instance.ConsumesFiring}");

                // Movement - check for damage after a jump, and if so force a piloting check
                if (__instance.OwningMech != null && __instance.OwningMech.ActuatorDamageMalus() != 0) {
                    Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Move.SkillMulti);
                    float damagePenalty = __instance.OwningMech.ActuatorDamageMalus() * Mod.Config.Move.SkillMulti;
                    float checkMod = sourceSkillMulti + damagePenalty;
                    Mod.Log.Debug($"  moveSkillMulti:{sourceSkillMulti} - damagePenalty: {damagePenalty} = checkMod: {checkMod}");

                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterRunChance, __instance.OwningMech, checkMod, ModText.FT_Fall_After_Jump);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after jumping with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Fall_After_Jump);
                    }
                }
            }
        }

        // Prevents losing evasion when attacked
        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence", null)]
        public static class AbstractActor_ResolveAttackSequence_Patch {
            
            private static bool Prefix(AbstractActor __instance) {
                Mod.Log.Trace("AA:RAS:PRE entered");
                return !Mod.Config.Features.PermanentEvasion;
            }

            private static void Postfix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID, AttackDirection attackDirection) {
                Mod.Log.Trace("AA:RAS:POST entered");
                if (!Mod.Config.Features.PermanentEvasion) { return; }

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

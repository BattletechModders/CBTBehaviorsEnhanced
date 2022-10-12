
using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System.Collections.Generic;
using CBTBehaviorsEnhanced.Helper;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced
{
    public static class MovementPatches
    {

        [HarmonyPatch(typeof(OrderSequence), "OnUpdate")]
        public static class OrderSequence_OnUpdate
        {
            public static void Prefix(OrderSequence __instance, ref bool __state)
            {
                __state = false;

                Mech mech = __instance.owningActor as Mech;
                if (__instance == null || __instance.owningActor == null || mech == null) return; // Nothing to do

                // sequenceIsComplete should be false here, but true in the postfix
                Traverse sequenceIsCompleteT = Traverse.Create(__instance).Property("sequenceIsComplete");
                __state = sequenceIsCompleteT.GetValue<bool>();
            }

            public static void Postfix(OrderSequence __instance, bool __state)
            {

                Mech mech = __instance.owningActor as Mech;
                if (__instance == null || __instance.owningActor == null || mech == null) return; // Nothing to do
                Mod.ActivationLog.Debug?.Write($"OS:OU - entered for Mech: {CombatantUtils.Label(mech)} with autoBrace: {mech.AutoBrace}");

                // If state is true, orders were complete before we headed into the sequence, so skip
                if (__state) return;

                // If the seqeuence doesn't consume activation, it's not one we target. Ignore it.
                if (!__instance.ConsumesActivation)
                {
                    Mod.ActivationLog.Debug?.Write($" -- !consumesActivation: {__instance.ConsumesActivation}, skipping");
                    return;
                }

                if (mech.IsShutDown)
                {
                    Mod.ActivationLog.Debug?.Write(" -- Mech is shutdown, assuming a MechStartupSequence will handle this - skipping.");
                    return;
                }

                bool isInterleaved = SharedState.Combat?.TurnDirector?.IsInterleaved == true;
                if (isInterleaved)
                {
                    Mod.ActivationLog.Debug?.Write(" -- Combat is interleaved, should be handled by OnUpdate() or MechStartupSequence - skipping.");
                    return;
                }

                DoneWithActorSequence dwaSeq = __instance as DoneWithActorSequence;
                if (dwaSeq != null)
                {
                    Mod.ActivationLog.Debug?.Write($" -- sequence is DoneWithActorSequence: {dwaSeq != null}, skipping.");
                    return; // Either a complete ending sequence, or the specific sequence doesn't consume activation so return
                }

                // Finally, check to see if the sequence isn't complete yet
                Traverse sequenceIsCompleteT = Traverse.Create(__instance).Property("sequenceIsComplete");
                bool sequenceIsComplete = sequenceIsCompleteT.GetValue<bool>();
                if (!sequenceIsComplete)
                {
                    Mod.ActivationLog.Debug?.Write($" -- !sequenceIsComplete: {sequenceIsComplete}, skipping");
                    return;
                }

                // At this point, ___state should be false and sequenceIsComplete is true. This represents OnUpdate flipping the value during it's processing.
                Mod.ActivationLog.Info?.Write($" -- AT ACTIVATION END, checking for heat sequence creation. ");
                Mod.ActivationLog.Info?.Write($"  -- isInterleavePending => {SharedState.Combat?.TurnDirector?.IsInterleavePending}  " +
                    $"highestEnemyContactLevel => {SharedState.Combat?.LocalPlayerTeam?.VisibilityCache.HighestEnemyContactLevel}");

                // By default OrderSequence:OnUpdate doesn't apply a MechHeatSequence if you are in non-interleaved mode. Why? I don't know. Force it to add one here.
                MechHeatSequence heatSequence = mech.GenerateEndOfTurnHeat(__instance);
                if (heatSequence != null)
                {
                    Mod.ActivationLog.Info?.Write($" -- Creating heat sequence for non-interleaved mode");
                    __instance.AddChildSequence(heatSequence, __instance.MessageIndex);
                }
                else
                {
                    Mod.ActivationLog.Warn?.Write($"FAILED TO CREATE HEAT SEQUENCE FOR MECH: {mech.DistinctId()} - UNIT WILL CONTINUE TO GAIN HEAT!");
                }

            }
        }

        [HarmonyPatch(typeof(ActorMovementSequence), "OnComplete")]
        public static class ActorMovementSequence_OnComplete
        {
            private static void Prefix(ActorMovementSequence __instance)
            {
                Mod.ActivationLog.Info?.Write($"AMS:OC:PRE entered for actor: {CombatantUtils.Label(__instance?.OwningActor)}");

                // Interleaved - check for visibility to any enemies 
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved)
                {
                    __instance.owningActor.AutoBrace = true;
                }

                // Movement - check for damage after a sprint, and if so force a piloting check
                if (__instance.OwningMech != null && __instance.isSprinting && __instance.OwningMech.ActuatorDamageMalus() != 0)
                {
                    // Vehicles, Naval unit & BA do not fall over
                    if (__instance.OwningMech.IsVehicle() || __instance.OwningMech.IsNaval() ||
                        __instance.OwningMech.IsTrooper()) return;
                    Mod.Log.Info?.Write($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float checkMod = __instance.OwningMech.PilotCheckMod(Mod.Config.SkillChecks.ModPerPointOfPiloting);
                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterRunChance, __instance.OwningMech, checkMod, ModText.FT_Fall_After_Run);
                    if (!sourcePassed)
                    {
                        Mod.Log.Info?.Write($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after sprinting with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Fall_After_Run);
                    }
                }
            }

            static void Postfix(ActorMovementSequence __instance)
            {
                Mod.ActivationLog.Info?.Write($"AMS:OC:POST - actor: {CombatantUtils.Label(__instance.OwningActor)} " +
                    $"autoBrace: {__instance.OwningActor.AutoBrace}  hasFired: {__instance.OwningActor.HasFiredThisRound}  consumesFiring: {__instance.ConsumesFiring}");
            }
        }

        // Prevents a mech from being able to move into combat or use abilities from non-interleaved mode
        [HarmonyPatch(typeof(ActorMovementSequence), "ConsumesFiring", MethodType.Getter)]
        public static class ActorMovementSequence_ConsumesFiring_Getter
        {
            private static void Postfix(ActorMovementSequence __instance, ref bool __result)
            {
                Mod.Log.Trace?.Write("AMS:CF:GET entered");
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
        public static class MechJumpSequence_ConsumesFiring_Getter
        {
            private static void Postfix(MechJumpSequence __instance, ref bool __result)
            {
                Mod.Log.Trace?.Write("AMS:CF:GET entered");
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved)
                {
                    // We want to auto-brace, and auto-brace requires that consumesFiring = false. So when no enemies are around, don't consume firing so 
                    //   that we can auto-brace
                    __result = false;
                }
            }
        }


        [HarmonyPatch(typeof(MechJumpSequence), "OnComplete")]
        public static class MechJumpSequence_OnComplete
        {
            private static void Prefix(MechJumpSequence __instance)
            {
                Mod.ActivationLog.Debug?.Write($"MJS:OC entered for actor: {CombatantUtils.Label(__instance.OwningMech)}");

                // Check for visibility to any enemies
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved)
                {
                    Mod.ActivationLog.Info?.Write("MJS:OC is not interleaved and no enemies - autobracing ");
                    __instance.owningActor.AutoBrace = true;
                }

                Mod.Log.Trace?.Write($"JUMP -- ABILITY_CONSUMES_FIRING: {__instance.AbilityConsumesFiring} / CONSUMES_FIRING: {__instance.ConsumesFiring}");

                if (__instance.OwningMech == null) return; // Nothing more to do

                // Movement - check for damage after a jump, and if so force a piloting check
                if (__instance.OwningMech.ActuatorDamageMalus() != 0 || Mod.Config.Developer.ForceFallAfterJump)
                {
                    // Vehicles, Naval unit & BA do not fall over
                    if (__instance.OwningMech.IsVehicle() || __instance.OwningMech.IsNaval() ||
                        __instance.OwningMech.IsTrooper()) return;
                    
                    Mod.Log.Info?.Write($"Actor: {CombatantUtils.Label(__instance.OwningMech)} has actuator damage, forcing piloting check.");
                    float checkMod = __instance.OwningMech.PilotCheckMod(Mod.Config.SkillChecks.ModPerPointOfPiloting);

                    bool sourcePassed = Mod.Config.Developer.ForceFallAfterJump ? false : 
                        CheckHelper.DidCheckPassThreshold(Mod.Config.Move.FallAfterJumpChance, __instance.OwningMech, checkMod, ModText.FT_Fall_After_Jump);
                    if (!sourcePassed)
                    {
                        Mod.Log.Info?.Write($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check after jumping with actuator damage, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModText.FT_Fall_After_Jump);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(AbstractActorMovementInvocation), "Invoke")]
        public static class AbstractActorMovementInvocation_Invoke
        {
            private static void Postfix(AbstractActorMovementInvocation __instance)
            {
                AbstractActor actor = SharedState.Combat.FindActorByGUID(__instance.ActorGUID);
                if (actor != null)
                {
                    Mod.ActivationLog.Debug?.Write($"AAMI:I entered for actor: {CombatantUtils.Label(actor)}");

                    // Check for visibility to any enemies
                    if (!actor.Combat.TurnDirector.IsInterleaved)
                    {
                        Mod.ActivationLog.Info?.Write("MJS:OC is not interleaved and no enemies - autobracing ");
                        actor.AutoBrace = true;
                    }
                }

            }
        }

        // Prevents losing evasion when attacked
        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence", null)]
        public static class AbstractActor_ResolveAttackSequence_Patch
        {

            private static bool Prefix(AbstractActor __instance)
            {
                Mod.Log.Trace?.Write("AA:RAS:PRE entered");
                return !Mod.Config.Features.PermanentEvasion;
            }

            private static void Postfix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID, AttackDirection attackDirection)
            {
                Mod.Log.Trace?.Write("AA:RAS:POST entered");
                if (!Mod.Config.Features.PermanentEvasion) { return; }

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(sequenceID);
                if (attackSequence != null)
                {
                    if (!attackSequence.GetAttackDidDamage(__instance.GUID))
                    {
                        return;
                    }
                    List<Effect> list = __instance.Combat.EffectManager
                        .GetAllEffectsTargeting(__instance)
                        .FindAll((Effect x) => x.EffectData.targetingData.effectTriggerType == EffectTriggerType.OnDamaged);

                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].OnEffectTakeDamage(attackSequence.attacker, __instance);
                    }

                    if (attackSequence.isMelee)
                    {
                        int value = attackSequence.attacker.StatCollection.GetValue<int>(ModStats.MeleeHitPushBackPhases);
                        if (value > 0)
                        {
                            for (int j = 0; j < value; j++)
                            {
                                __instance.ForceUnitOnePhaseDown(sourceID, stackItemID, false);
                            }
                        }
                    }
                }
            }
        }
    }
}

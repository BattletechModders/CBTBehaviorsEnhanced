
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

                if (attacker.HasMovedThisRound && attacker.JumpedLastRound) {
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

        [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetMechwarriorButtons")]
        public static class CombatHUDMechwarriorTray_ResetMechwarriorButtons {
            static void Prefix(CombatHUDMechwarriorTray __instance, AbstractActor actor) {
                Mod.Log.Trace($"CHUDMT:RMB:post entered.");
                if (actor != null && actor.Combat.TurnDirector.IsInterleavePending) {
                    Traverse turnDirectorT = Traverse.Create(actor.Combat.TurnDirector).Property("_isInterleaved");
                    turnDirectorT.SetValue(true);
                }
            }

            static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor) {
                Mod.Log.Trace($"CHUDMT:RMB:post entered.");
                if (actor != null && actor.Combat.TurnDirector.IsInterleavePending) {
                    Traverse turnDirectorT = Traverse.Create(actor.Combat.TurnDirector).Property("_isInterleaved");
                    turnDirectorT.SetValue(false);
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

        [HarmonyPatch(typeof(MechJumpSequence), "OnComplete")]
        public static class MechJumpSequence_OnComplete {
            private static void Prefix(MechJumpSequence __instance) {
                Mod.Log.Trace("MJS:OC entered");
                // Check for visibility to any enemies
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved &&
                    __instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0) {
                    //Mod.Log.Info("MJS:OC TD is not interleaved but enemies are detected - disabling autobrace. ");
                    __instance.owningActor.AutoBrace = false;
                }

                // Movement - check for damage after a sprint, and if so force a piloting check
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

        [HarmonyPatch(typeof(Team), "GetNextAvailableUnit")]
        public static class Team_GetNextAvailableUnit {
            private static void Postfix(Team __instance, ref AbstractActor __result) {
                Mod.Log.Info($"T:GNAU invoked with null AA? {__result == null}");

                if (__instance.IsLocalPlayer && __instance.Combat.TurnDirector.IsInterleavePending) {
                    Mod.Log.Info("  IsInterleavePending - returning no available unit and deferring all units.");
                    __instance.DoneWithAllAvailableActors();

                    __result = null;

                    ReserveActorInvocation message = new ReserveActorInvocation(__instance.Combat.LocalPlayerTeam, ReserveActorAction.DONE, __instance.Combat.TurnDirector.CurrentRound);
                    __instance.Combat.MessageCenter.PublishMessage(message);
                }
            }
        }

        [HarmonyPatch(typeof(Team),"DoneWithAllAvailableActors")]
        public static class Team_DoneWithAllAvailableActors {
            private static void Prefix(Team __instance, List<IStackSequence> __result) {
                Mod.Log.Info($"T:DWAAA invoked");
                if (!__instance.IsLocalPlayer) { return; }

                if (__instance.Combat.TurnDirector.IsInterleavePending) {
                    if (__result == null) {
                        Mod.Log.Info("Result was null, adding a new list.");
                        __result = new List<IStackSequence>();
                    }

                    int numUnitsEndingActivation = 0;
                    foreach (AbstractActor unit in __instance.units) {
                        Mod.Log.Info($"Processing unit: {unit.DisplayName}_{unit.GetPilot().Name}");
                        if (!unit.IsCompletingActivation && !unit.IsDead && !unit.IsFlaggedForDeath) {
                            Mod.Log.Info($"  Ending activation ");
                            IStackSequence item = unit.DoneWithActor();
                            numUnitsEndingActivation++;
                            __result.Add(item);
                        }
                    }

                    Traverse numUnitsEndingActivationT = Traverse.Create(__instance).Field("numUnitsEndingActivation");
                    int currentValue = numUnitsEndingActivationT.GetValue<int>();
                    numUnitsEndingActivationT.SetValue(currentValue + numUnitsEndingActivation);
                }

            }
        }

        [HarmonyPatch(typeof(TurnDirector), "OnTurnActorActivateComplete")]
        public static class TurnDirector_OnTurnActorActivateComplete {
            private static bool Prefix(TurnDirector __instance) {
                Mod.Log.Info($"TD:OTAAC invoked");

                if (__instance.IsMissionOver) {
                    return false;
                }

                Mod.Log.Info($"TD isInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.Combat.TurnDirector.IsInterleavePending}" +
                    $"  isNonInterleavePending: {__instance.Combat.TurnDirector.IsNonInterleavePending}");

                int numUnusedUnitsForCurrentPhase = __instance.TurnActors[__instance.ActiveTurnActorIndex].GetNumUnusedUnitsForCurrentPhase();
                Mod.Log.Info($"There are {numUnusedUnitsForCurrentPhase} unusedUnits in the current phase)");

                if (!__instance.IsInterleavePending && !__instance.IsInterleaved && numUnusedUnitsForCurrentPhase > 0) {
                    Mod.Log.Info("Sending TurnActorActivateMessage");
                    Traverse staamT = Traverse.Create(__instance).Method("SendTurnActorActivateMessage", new object[] { __instance.ActiveTurnActorIndex });
                    staamT.GetValue();
                } else {
                    Mod.Log.Info("Incrementing ActiveTurnActor");
                    Traverse iataT = Traverse.Create(__instance).Method("IncrementActiveTurnActor");
                    iataT.GetValue();
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence", null)]
        public static class AbstractActor_ResolveAttackSequence_Patch {
            
            private static bool Prefix(AbstractActor __instance) {
                Mod.Log.Trace("AA:RAS:PRE entered");
                return false;
            }

            private static void Postfix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID, AttackDirection attackDirection) {
                Mod.Log.Trace("AA:RAS:POST entered");

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


using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviors {

    public static class MovementPatches {

        //[HarmonyPatch(typeof(EncounterLayerData))]
        //[HarmonyPatch("ContractInitialize")]
        //public static class EncounterLayerData_ContractInitialize {
        //    static void Prefix(EncounterLayerData __instance) {
        //        Mod.Log.Trace("ELD:CI entered");
        //        try {
        //            __instance.turnDirectorBehavior = TurnDirectorBehaviorType.AlwaysInterleaved;
        //        } catch (Exception e) {
        //            Mod.Log.Info($"Failed to set behavior to interleaved due to:{e.Message}");
        //        }
        //    }
        //}

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

            private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target) {
                Mod.Log.Trace("CHUDWS:SHC entered");

                AbstractActor actor = __instance.DisplayedWeapon.parent;
                var _this = Traverse.Create(__instance);

                if (actor.HasMovedThisRound && actor.JumpedLastRound) {
                    Traverse addToolTipDetailT = Traverse.Create(__instance).Method("AddToolTipDetail", "JUMPED SELF", Mod.Config.ToHitSelfJumped);
                    Mod.Log.Debug($"Invoking addToolTipDetail for: JUMPED SELF = {Mod.Config.ToHitSelfJumped}");
                    addToolTipDetailT.GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
        public static class AbstractActor_InitEffectStats {
            static void Postfix(AbstractActor __instance) {
                Mod.Log.Info($"AA:IES entered- setting CanShootAfterSprinting for actor:{__instance.DisplayName}");
                __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
            }
        }

        //[HarmonyPatch(typeof(AbstractActor), "DoneWithActor")]
        //public static class AbstractActor_DoneWithActor {
        //    static void Postfix(AbstractActor __instance) {
        //        Mod.Log.Info($"AA:DWA - entered.");
        //    }
        //}

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

                //Mod.Log.Info($"  TD isInterleaved: {actor.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {actor.Combat.TurnDirector.IsInterleavePending}" +
                //    $"  isNonInterleavePending: {actor.Combat.TurnDirector.IsNonInterleavePending}");

                //if (actor == null) {
                //    Mod.Log.Info($"CHUDMT:RMB - entered with null actor");
                //} else {
                //    Mod.Log.Info($"CHUDMT:RMB - entered for actor:{actor.DisplayName}");
                //}
            }
        }

        [HarmonyPatch(typeof(ActorMovementSequence), "OnComplete")]
        public static class ActorMovementSequence_OnComplete {
            private static void Prefix(ActorMovementSequence __instance) {
                Mod.Log.Trace("AMS:OC entered");
                // Check for visibility to any enemies 

                //Mod.Log.Info($" Actor {__instance.owningActor.DisplayName} has autoBrace: {__instance.owningActor.AutoBrace}  canShootAfterSprinting:{__instance.owningActor.CanShootAfterSprinting}");
                //Mod.Log.Info($" Sequence - consumesActivation: {__instance.ConsumesActivation}  forceActivationEnd: {__instance.ForceActivationEnd}");
                //Mod.Log.Info($"TD isInterleaved: {__instance.owningActor.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.owningActor.Combat.TurnDirector.IsInterleavePending}" +
                //    $"  isNonInterleavePending: {__instance.owningActor.Combat.TurnDirector.IsNonInterleavePending}");

                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved) {
                    if (__instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0) {
                        Mod.Log.Info("AMS:OC TD is not interleaved but enemies are detected - disabling autobrace. ");
                        __instance.owningActor.AutoBrace = false;
                    } else {
                        Mod.Log.Info("AMS:OC TD is not interleaved and no enemies - autobracing ");
                        __instance.owningActor.AutoBrace = true;
                    }
                }

            }
        }

        //[HarmonyPatch(typeof(SelectionStateSprint))]
        //[HarmonyPatch("ConsumesFiring", MethodType.Getter)]
        //public static class SelectionState_ConsumesFiring_Getter {
        //    static void Postfix(SelectionStateSprint __instance, bool __result) {
        //        //Mod.Log.Info($"SS:CF returning result: {__result}");
        //    }
        //}


        [HarmonyPatch(typeof(MechJumpSequence), "OnComplete")]
        public static class MechJumpSequence_OnComplete {
            private static void Prefix(MechJumpSequence __instance) {
                Mod.Log.Trace("MJS:OC entered");
                // Check for visibility to any enemies

                //Mod.Log.Info($" Actor {__instance.owningActor.DisplayName} has autoBrace: {__instance.owningActor.AutoBrace}");
                //Mod.Log.Info($"TD isInterleaved: {__instance.owningActor.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.owningActor.Combat.TurnDirector.IsInterleavePending}" +
                //    $"  isNonInterleavePending: {__instance.owningActor.Combat.TurnDirector.IsNonInterleavePending}");
                
                if (!__instance.owningActor.Combat.TurnDirector.IsInterleaved &&
                    __instance.owningActor.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0) {
                    //Mod.Log.Info("MJS:OC TD is not interleaved but enemies are detected - disabling autobrace. ");
                    __instance.owningActor.AutoBrace = false;
                }
 
            }
        }

        //[HarmonyPatch(typeof(AbstractActor), "OnActivationEnd")]
        //public static class AbstractActor_OnActivationEnd {
        //    private static void Postfix(AbstractActor __instance) {
        //        Mod.Log.Info("AA:OAE invoked");
        //        if (__instance.Combat.TurnDirector.IsInterleavePending && __instance.Combat.LocalPlayerTeam.GetDetectedEnemyUnits().Count > 0) {
        //            Mod.Log.Info("AMS:OAE TD interleave is pending and enemies are detected - notifying of contact ");
        //            //__instance.Combat.TurnDirector.OnPhaseBeginComplete();
        //        }

        //    }
        //}

        //[HarmonyPatch(typeof(Team))]
        //[HarmonyPatch("AllUnitsDoneMoving", MethodType.Getter)]
        //public static class Team_AllUnitsDoneMoving {
        //    private static void Prefix(Team __instance, ref bool __result) {
        //        Mod.Log.Info($"T:AUDM invoked");
        //        if (__instance.Combat.TurnDirector.IsInterleavePending && __instance.GetDetectedEnemyUnits().Count > 0) {
        //            Mod.Log.Info("T:AUDM TD interleave is pending and enemies are detected - returning true");
        //            __result = true;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(AbstractActor), "IsAvailableOnPhase")]
        //public static class AbstractActor_IsAvailableOnPhase {
        //    private static void Postfix(AbstractActor __instance, ref bool __result) {
        //        Mod.Log.Info($"AA:IAOP invoked");
        //        if (__instance.Combat.TurnDirector.IsInterleavePending && __instance.GetDetectedEnemyUnits().Count > 0 && __instance.HasBegunActivation == false) {
        //            Mod.Log.Info("AA:IAOP TD interleave is pending and enemies are detected - returning true");
        //            __result = false;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(DoneWithActorSequence), "CompleteOrders")]
        //public static class DoneWithActorSequence_CompleteOrders {
        //    private static void Prefix(DoneWithActorSequence __instance) {
        //        Mod.Log.Info($"DOAS:CO invoked");
        //        //__instance.owningActor.Combat.TurnDirector.OnPhaseBeginComplete();
        //    }
        //}

        [HarmonyPatch(typeof(Team), "GetNextAvailableUnit")]
        public static class Team_GetNextAvailableUnit {
            private static void Postfix(Team __instance, ref AbstractActor __result) {
                Mod.Log.Info($"T:GNAU invoked with null AA? {__result == null}");

                if (__instance.IsLocalPlayer && __instance.Combat.TurnDirector.IsInterleavePending) {
                    Mod.Log.Info("  IsInterleavePending - returning no available unit and deferring all units.");
                    //foreach (AbstractActor unit in __instance.units) {
                    //    if (!unit.HasActivatedThisRound) {
                    //        Mod.Log.Info($"  Deferring unit: {unit.DisplayName} with canBeDeferred: {unit.CanDeferUnit}");
                    //    }
                    //}
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

                //int evasivePipsCurrent = __instance.EvasivePipsCurrent;
                //__instance.ConsumeEvasivePip(true);
                //int evasivePipsCurrent2 = __instance.EvasivePipsCurrent;
                //if (evasivePipsCurrent2 < evasivePipsCurrent && !__instance.IsDead && !__instance.IsFlaggedForDeath) {
                //    __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "-1 EVASION", FloatieMessage.MessageNature.Debuff));
                //}

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(sequenceID);
                if (attackSequence != null) {
                    if (!attackSequence.GetAttackDidDamage(__instance.GUID)) {
                        return;
                    }
                    List<Effect> list = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance).FindAll((Effect x) => x.EffectData.targetingData.effectTriggerType == EffectTriggerType.OnDamaged);
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

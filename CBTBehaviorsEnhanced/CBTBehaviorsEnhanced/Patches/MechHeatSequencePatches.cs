using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using Localize;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(MechHeatSequence), "setState")]
    public static class MechHeatSequence_SetState {
        // Because this is private in MechHeatSequence, we can't directly reference via Harmony. Make our own instead.
        public enum HeatSequenceState {
            None,
            Delaying,
            Rising,
            Falling,
            Finished
        }

        // How does this enum work? Fuck if I know. It even surprised paradeike, but apparently it just 'works'. 
        public static bool Prefix(MechHeatSequence __instance, HeatSequenceState newState) {

            if (newState != HeatSequenceState.Finished) { return true; }

            Traverse stateT = Traverse.Create(__instance).Field("state");
            HeatSequenceState currentState = (HeatSequenceState)stateT.GetValue<int>();
            if (currentState == newState) { return true; }

            Mod.Log.Debug($"MHS - executing updated logic for state: {newState} on actor:{CombatantUtils.Label(__instance.OwningMech)}.");
            stateT.SetValue((int)newState);

            Traverse timeInCurrentStateT = Traverse.Create(__instance).Field("timeInCurrentState");
            timeInCurrentStateT.SetValue(0f);

            /* Finished Can be invoked from a rising state (heat being added):
                Attack Sequence, artillery sequence, actor burning effect, (heatSinkStep=false, applyStartupHeatSinks=false)
                On End of turn sequence - (heatSinkStep=true, applyStartupHeatSinks=false)
                On Mech Startup sequence - (heatSinkStep=true, applyStartupHeatSinks=true)
             */

            if (!__instance.PerformHeatSinkStep) {
                Mod.Log.Debug($"Reconciling heat for actor: {CombatantUtils.Label(__instance.OwningMech)}");
                Mod.Log.Debug($"  Before - currentHeat: {__instance.OwningMech.CurrentHeat}  tempHeat: {__instance.OwningMech.TempHeat}  " +
                    $"isPastMaxHeat: {__instance.OwningMech.IsPastMaxHeat}  hasAppliedHeatSinks: {__instance.OwningMech.HasAppliedHeatSinks}");
                // Checks for heat damage, clamps heat to max and min
                __instance.OwningMech.ReconcileHeat(__instance.RootSequenceGUID, __instance.InstigatorID);
                Mod.Log.Debug($"  After - currentHeat: {__instance.OwningMech.CurrentHeat}  tempHeat: {__instance.OwningMech.TempHeat}  " +
                    $"isPastMaxHeat: {__instance.OwningMech.IsPastMaxHeat}  hasAppliedHeatSinks: {__instance.OwningMech.HasAppliedHeatSinks}");
            }

            if (__instance.PerformHeatSinkStep && !__instance.ApplyStartupHeatSinks)
            {
                // We are at the end of the turn - force an overheat
                Mod.Log.Info($"-- AT END OF TURN FOR {CombatantUtils.Label(__instance.OwningMech)}... CHECKING EFFECTS");

                MultiSequence sequence = new MultiSequence(__instance.OwningMech.Combat);

                float heatCheck = __instance.OwningMech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
                float pilotCheck = __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.SkillMulti);
                Mod.Log.Debug($" Actor: {CombatantUtils.Label(__instance.OwningMech)} has gutsMulti: {heatCheck}  pilotingMulti: {pilotCheck}");

                bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(__instance.OwningMech, __instance.OwningMech.CurrentHeat, __instance.RootSequenceGUID, __instance.SequenceGUID, heatCheck);
                bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(__instance.OwningMech, __instance.OwningMech.CurrentHeat, __instance.RootSequenceGUID, heatCheck);
                bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(__instance.OwningMech, __instance.OwningMech.CurrentHeat, __instance.RootSequenceGUID, heatCheck);
                bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(__instance.OwningMech, __instance.OwningMech.CurrentHeat, __instance.RootSequenceGUID, heatCheck);

                bool failedShutdownCheck = false;
                if (!__instance.OwningMech.IsShutDown)
                {
                    // Resolve Shutdown + Fall
                    failedShutdownCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModText.FT_Check_Shutdown);
                    Mod.Log.Debug($"  failedShutdownCheck: {failedShutdownCheck}");
                    if (failedShutdownCheck)
                    {
                        Mod.Log.Info($"-- Shutdown check failed for unit {CombatantUtils.Label(__instance.OwningMech)}, forcing unit to shutdown");

                        string debuffText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Shutdown_Failed_Overide]).ToString();
                        sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, debuffText,
                            FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                        MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance.OwningMech)
                        {
                            RootSequenceGUID = __instance.SequenceGUID
                        };
                        sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                        if (__instance.OwningMech.IsOrWillBeProne)
                        {
                            bool failedFallingCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.ShutdownFallThreshold, __instance.OwningMech, pilotCheck, ModText.FT_Check_Fall);
                            Mod.Log.Debug($"  failedFallingCheck: {failedFallingCheck}");
                            if (failedFallingCheck)
                            {
                                Mod.Log.Info("   Pilot check from shutdown failed! Forcing a fall!");
                                string fallDebuffText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Shutdown_Fall]).ToString();
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDebuffText,
                                    FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                                MechFallSequence mfs = new MechFallSequence(__instance.OwningMech, "Overheat", new Vector2(0f, -1f))
                                {
                                    RootSequenceGUID = __instance.SequenceGUID
                                };
                                sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                            }
                            else
                            {
                                Mod.Log.Info($"Pilot check to avoid falling passed.");
                            }
                        }
                        else
                        {
                            Mod.Log.Debug("Unit is already prone, skipping.");
                        }
                    }
                }
                else
                {
                    Mod.Log.Debug("Unit is already shutdown, skipping.");
                }

                if (failedInjuryCheck || failedSystemFailureCheck || failedAmmoCheck || failedVolatileAmmoCheck || failedShutdownCheck)
                {
                    __instance.OwningMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                }

                return false;
            }

            if (__instance.OwningMech.GameRep != null) {
                if (__instance.OwningMech.team.LocalPlayerControlsTeam) {
                    if (__instance.OwningMech.CurrentHeat > __instance.OwningMech.OverheatLevel) {
                        string text = string.Format("MechHeatSequence_{0}_{1}", __instance.RootSequenceGUID, __instance.SequenceGUID);
                        AudioEventManager.CreateVOQueue(text, -1f, null, null);
                        AudioEventManager.QueueVOEvent(text, VOEvents.Mech_Overheat_Warning, __instance.OwningMech);
                        AudioEventManager.StartVOQueue(1f);
                    }

                    if ((float)__instance.OwningMech.CurrentHeat > (float)__instance.OwningMech.MaxHeat - (float)(__instance.OwningMech.MaxHeat - __instance.OwningMech.OverheatLevel) * 0.333f) {
                        WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_overheat_alarm_3, WwiseManager.GlobalAudioObject, null, null);
                    } else if ((float)__instance.OwningMech.CurrentHeat > (float)__instance.OwningMech.MaxHeat - (float)(__instance.OwningMech.MaxHeat - __instance.OwningMech.OverheatLevel) * 0.666f) {
                        WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_overheat_alarm_2, WwiseManager.GlobalAudioObject, null, null);
                    } else if (__instance.OwningMech.CurrentHeat > __instance.OwningMech.OverheatLevel) {
                        WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_overheat_alarm_1, WwiseManager.GlobalAudioObject, null, null);
                    }
                }

                if (__instance.OwningMech.CurrentHeat > Mod.Config.Heat.ShowLowOverheatAnim) {
                    __instance.OwningMech.GameRep.StopManualPersistentVFX(__instance.OwningMech.Combat.Constants.VFXNames.heat_midHeat_persistent);
                    __instance.OwningMech.GameRep.PlayVFX(8, __instance.OwningMech.Combat.Constants.VFXNames.heat_highHeat_persistent, true, Vector3.zero, false, -1f);
                    return false;
                }

                if ((float)__instance.OwningMech.CurrentHeat > Mod.Config.Heat.ShowExtremeOverheatAnim) {
                    __instance.OwningMech.GameRep.StopManualPersistentVFX(__instance.OwningMech.Combat.Constants.VFXNames.heat_highHeat_persistent);
                    __instance.OwningMech.GameRep.PlayVFX(8, __instance.OwningMech.Combat.Constants.VFXNames.heat_midHeat_persistent, true, Vector3.zero, false, -1f);
                    return false;
                }

                __instance.OwningMech.GameRep.StopManualPersistentVFX(__instance.OwningMech.Combat.Constants.VFXNames.heat_highHeat_persistent);
                __instance.OwningMech.GameRep.StopManualPersistentVFX(__instance.OwningMech.Combat.Constants.VFXNames.heat_midHeat_persistent);
            }

            return false;
        }

       
    }
}

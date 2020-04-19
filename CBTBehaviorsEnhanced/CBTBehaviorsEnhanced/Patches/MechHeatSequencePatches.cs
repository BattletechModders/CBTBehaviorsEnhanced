using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CustomComponents;
using Harmony;
using Localize;
using System.Collections.Generic;
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

        public static bool Prefix(MechHeatSequence __instance, HeatSequenceState newState) {

            if (newState != HeatSequenceState.Finished) { return true; }

            Traverse stateT = Traverse.Create(__instance).Field("state");
            HeatSequenceState currentState = (HeatSequenceState)stateT.GetValue<int>();
            if (currentState == newState) { return true; }

            Mod.Log.Info($"MHS - executing updated logic for state: {newState} on actor:{__instance.OwningMech.DisplayName}_{__instance.OwningMech.GetPilot().Name}.");
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

            //if (__instance.OwningMech.IsPastMaxHeat && !__instance.OwningMech.IsShutDown) {
            //    __instance.OwningMech.GenerateOverheatedSequence(__instance);
            //    return;
            //}

            if (__instance.PerformHeatSinkStep && !__instance.ApplyStartupHeatSinks) {
                // We are at the end of the turn - force an overheat
                Mod.Log.Info($"-- AT END OF TURN FOR {CombatantUtils.Label(__instance.OwningMech)}... CHECKING EFFECTS");

                MultiSequence sequence = new MultiSequence(__instance.OwningMech.Combat);

                // Possible sequences
                //  Shutdown
                //  Fall from shutdown
                //  Ammo Explosion
                //  System damage
                //  Pilot injury
                //  Pilot death

                float heatCheck = __instance.OwningMech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
                float pilotCheck = __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.SkillMulti);
                Mod.Log.Debug($" Actor: {CombatantUtils.Label(__instance.OwningMech)} has gutsMulti: {heatCheck}  pilotingMulti: {pilotCheck}");

                // Resolve Pilot Injury
                bool failedInjuryCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.PilotInjury, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Injury);
                Mod.Log.Debug($"  failedInjuryCheck: {failedInjuryCheck}");
                if (failedInjuryCheck) {
                    Mod.Log.Info($"-- Pilot Heat Injury check failed for {CombatantUtils.Label(__instance.OwningMech)}, forcing injury from heat");
                    __instance.OwningMech.pilot.InjurePilot(__instance.SequenceGUID.ToString(), __instance.RootSequenceGUID, 1, DamageType.OverheatSelf, null, __instance.OwningMech);
                    if (!__instance.OwningMech.pilot.IsIncapacitated) {
                        AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, __instance.OwningMech);
                        AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, __instance.OwningMech, null, null, true);
                        if (__instance.OwningMech.team.LocalPlayerControlsTeam) {
                            AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                        }
                    }
                }

                // Resolve System Damage
                bool failedSystemFailureCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.SystemFailures, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModConfig.FT_Check_System_Failure);
                Mod.Log.Debug($"  failedSystemFailureCheck: {failedSystemFailureCheck}");
                if (failedSystemFailureCheck) {
                    Mod.Log.Info($"-- System Failure check failed, forcing system damage on unit: {CombatantUtils.Label(__instance.OwningMech)}");
                    List<MechComponent> functionalComponents = new List<MechComponent>();
                    foreach (MechComponent mc in __instance.OwningMech.allComponents) {
                        bool canTarget = mc.IsFunctional;
                        if (mc.mechComponentRef.Is<Flags>(out Flags flagsCC)) {
                            if (flagsCC.IsSet(ModStats.ME_IgnoreDamage)) {
                                canTarget = false;
                                Mod.Log.Trace($"    Component: {mc.Name} / {mc.UIName} is marked ignores_damage.");
                            }
                        }
                        if (canTarget) { functionalComponents.Add(mc); }
                    }
                    MechComponent componentToDamage = functionalComponents.GetRandomElement();
                    Mod.Log.Info($"   Destroying component: {componentToDamage.UIName} from heat damage.");

                    WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                        new AttackDirection[] { AttackDirection.None }, null, null, null);
                    componentToDamage.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                }

                // Resolve Ammo Explosion - regular ammo
                bool failedAmmoCheck = false;
                AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(__instance.OwningMech, false);
                if (mostDamaging != null) {
                    failedAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Explosion);
                    Mod.Log.Debug($"  failedAmmoCheck: {failedAmmoCheck}");
                    if (failedAmmoCheck) {
                        Mod.Log.Info($"-- Ammo Explosion check failed, forcing ammo explosion on unit: {CombatantUtils.Label(__instance.OwningMech)}");

                        if (mostDamaging != null) {
                            Mod.Log.Info($"   Exploding ammo: {mostDamaging.UIName}");
                            WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                                new AttackDirection[] { AttackDirection.None }, null, null, null);
                            mostDamaging.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                        } else {
                            Mod.Log.Debug(" Unit has no ammo boxes, skipping.");
                        }
                    }
                }

                // Resolve Ammo Explosion - inferno ammo
                bool failedVolatileAmmoCheck = false;
                AmmunitionBox mostDamagingVolatile = HeatHelper.FindMostDamagingAmmoBox(__instance.OwningMech, true);
                if (mostDamagingVolatile != null) {
                    failedVolatileAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Explosion);
                    Mod.Log.Debug($"  failedVolatileAmmoCheck: {failedVolatileAmmoCheck}");
                    if (failedVolatileAmmoCheck) {
                        Mod.Log.Info($"-- Volatile Ammo Explosion check failed on {CombatantUtils.Label(__instance.OwningMech)}, forcing volatile ammo explosion");

                        if (mostDamaging != null) {
                            Mod.Log.Info($" Exploding inferno ammo: {mostDamagingVolatile.UIName}");
                            WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                                new AttackDirection[] { AttackDirection.None }, null, null, null);
                            mostDamagingVolatile.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                        } else {
                            Mod.Log.Debug(" Unit has no inferno ammo boxes, skipping.");
                        }
                    }
                }

                bool failedShutdownCheck = false;
                if (!__instance.OwningMech.IsShutDown) {
                    // Resolve Shutdown + Fall
                    failedShutdownCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, __instance.OwningMech.CurrentHeat, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Shutdown);
                    Mod.Log.Debug($"  failedShutdownCheck: {failedShutdownCheck}");
                    if (failedShutdownCheck) {
                        Mod.Log.Debug($"-- Shutdown check failed for unit {CombatantUtils.Label(__instance.OwningMech)}, forcing unit to shutdown");

                        string debuffText = new Text(Mod.Config.LocalizedFloaties[ModConfig.FT_Shutdown_Failed_Overide]).ToString();
                        sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, debuffText,
                            FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                        MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance.OwningMech) {
                            RootSequenceGUID = __instance.SequenceGUID
                        };
                        sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                        if (__instance.OwningMech.IsOrWillBeProne) {
                            bool failedFallingCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.ShutdownFallThreshold, __instance.OwningMech, pilotCheck, ModConfig.FT_Check_Fall);
                            Mod.Log.Debug($"  failedFallingCheck: {failedFallingCheck}");
                            if (failedFallingCheck) {
                                Mod.Log.Info("   Pilot check from shutdown failed! Forcing a fall!");

                                string fallDebuffText = new Text(Mod.Config.LocalizedFloaties[ModConfig.FT_Shutdown_Fall]).ToString();
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDebuffText,
                                    FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                                MechFallSequence mfs = new MechFallSequence(__instance.OwningMech, "Overheat", new Vector2(0f, -1f)) {
                                    RootSequenceGUID = __instance.SequenceGUID
                                };
                                sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                            } else {
                                Mod.Log.Debug($"Pilot check to avoid falling passed.");
                            }
                        } else {
                            Mod.Log.Debug("Unit is already prone, skipping.");
                        }
                    }
                } else {
                    Mod.Log.Debug("Unit is already shutdown, skipping.");
                }

                if (failedInjuryCheck || failedSystemFailureCheck || failedAmmoCheck || failedShutdownCheck) {
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

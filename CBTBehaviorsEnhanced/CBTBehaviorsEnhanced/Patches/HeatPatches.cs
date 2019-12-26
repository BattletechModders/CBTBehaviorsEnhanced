
using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Heat;
using Harmony;
using HBS;
using Localize;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced {

    public static class HeatPatches {

        [HarmonyPatch(typeof(Mech), "Init")]
        [HarmonyAfter("MechEngineer.Features.Engine")]
        public static class Mech_Init {
            public static void Postfix(Mech __instance) {
                Mod.Log.Trace("M:I entered.");
                MechHelper.InitModStats(__instance);
            }
        }

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
                    // Checks for heat damage, clamps heat to max and min
                    __instance.OwningMech.ReconcileHeat(__instance.RootSequenceGUID, __instance.InstigatorID);
                }

                //if (__instance.OwningMech.IsPastMaxHeat && !__instance.OwningMech.IsShutDown) {
                //    __instance.OwningMech.GenerateOverheatedSequence(__instance);
                //    return;
                //}

                if (__instance.PerformHeatSinkStep && !__instance.ApplyStartupHeatSinks) {
                    // We are at the end of the turn - force an overheat
                    Mod.Log.Info("AT END OF TURN - CHECKING EFFECTS");

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

                    bool failedAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Explosion);
                    Mod.Log.Debug($"  failedAmmoCheck: {failedAmmoCheck}");
                    bool failedInfernoAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Explosion);
                    Mod.Log.Debug($"  failedInfernoAmmoCheck: {failedInfernoAmmoCheck}");
                    bool failedShutdownCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Shutdown);
                    Mod.Log.Debug($"  failedShutdownCheck: {failedShutdownCheck}");
                    bool failedSystemFailureCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.SystemFailures, __instance.OwningMech, heatCheck, ModConfig.FT_Check_System_Failure);
                    Mod.Log.Debug($"  failedSystemFailureCheck: {failedSystemFailureCheck}");
                    bool failedInjuryCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.PilotInjury, __instance.OwningMech, heatCheck, ModConfig.FT_Check_Injury);
                    Mod.Log.Debug($"  failedInjuryCheck: {failedInjuryCheck}");

                    // Resolve Pilot Injury
                    if (failedInjuryCheck) {
                        Mod.Log.Debug("-- Pilot Injury check failed, forcing injury from heat");
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
                    if (failedSystemFailureCheck) {
                        Mod.Log.Debug("-- System Failure check failed, forcing system damage");
                        List<MechComponent> functionalComponents = __instance.OwningMech.allComponents.Where(mc => mc.IsFunctional).ToList();
                        MechComponent componentToDamage = functionalComponents.GetRandomElement();
                        Mod.Log.Debug($" Destroying component: {componentToDamage.UIName} from heat damage.");

                        WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                            new AttackDirection[] { AttackDirection.None }, null, null, null);
                        componentToDamage.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                    }

                    // Resolve Ammo Explosion - regular ammo
                    if (failedAmmoCheck) {
                        Mod.Log.Debug("-- Ammo Explosion check failed, forcing ammo explosion");

                        AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(__instance.OwningMech, false);
                        if (mostDamaging != null) {
                            Mod.Log.Debug($" Exploding ammo: {mostDamaging.UIName}");
                            WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null, 
                                new AttackDirection[] { AttackDirection.None }, null, null, null);
                            mostDamaging.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                        } else {
                            Mod.Log.Debug(" Unit has no ammo boxes, skipping.");
                        }
                    }

                    // Resolve Ammo Explosion - inferno ammo
                    if (failedInfernoAmmoCheck) {
                        Mod.Log.Debug("-- Inferno Ammo Explosion check failed, forcing inferno ammo explosion");

                        AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(__instance.OwningMech, true);
                        if (mostDamaging != null) {
                            Mod.Log.Debug($" Exploding inferno ammo: {mostDamaging.UIName}");
                            WeaponHitInfo fakeHit = new WeaponHitInfo(__instance.RootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                                new AttackDirection[] { AttackDirection.None }, null, null, null);
                            mostDamaging.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                        } else {
                            Mod.Log.Debug(" Unit has no inferno ammo boxes, skipping.");
                        }
                    }

                    // Resolve Shutdown + Fall
                    if (failedShutdownCheck) {
                        Mod.Log.Debug("-- Shutdown check failed, forcing unit to shutdown");

                        string debuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Failed_Overide]).ToString();
                        sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, debuffText,
                            FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                        MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance.OwningMech) {
                            RootSequenceGUID = __instance.SequenceGUID
                        };
                        sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                        bool failedFallingCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.ShutdownFallThreshold, __instance.OwningMech, pilotCheck, ModConfig.FT_Check_Fall);
                        Mod.Log.Debug($"  failedFallingCheck: {failedFallingCheck}");
                        if (failedFallingCheck) {
                            Mod.Log.Info("Pilot check from shutdown failed! Forcing a fall!");

                            string fallDebuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Fall]).ToString();
                            sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDebuffText,
                                FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                            MechFallSequence mfs = new MechFallSequence(__instance.OwningMech, "Overheat", new Vector2(0f, -1f)) {
                                RootSequenceGUID = __instance.SequenceGUID
                            };
                            sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                        } else {
                            Mod.Log.Info($"Pilot check to avoid falling passed.");
                        }
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

        // Deliberately empty to prevent all damage
        [HarmonyPatch(typeof(Mech), "CheckForHeatDamage")]
        public static class Mech_CheckForHeatDamage {
            static bool Prefix(Mech __instance, int stackID, string attackerID) {
                return false;
            }
        }

        [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
        public static class Mech_OnActivationEnd {
            private static void Prefix(Mech __instance, string sourceID, int stackItemID) {

                Mod.Log.Debug($"Actor: {__instance.DisplayName}_{__instance.GetPilot().Name} has currentHeat: {__instance.CurrentHeat}" +
                    $" tempHeat: {__instance.TempHeat}  maxHeat: {__instance.MaxHeat}  heatsinkCapacity: {__instance.AdjustedHeatsinkCapacity}");
            }
        }

        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("MaxMeleeEngageRangeDistance", MethodType.Getter)]
        public static class Mech_MaxMeleeEngageRangeDistance_Get {
            public static void Postfix(Mech __instance, ref float __result) {
                Mod.Log.Trace("M:MMERD:GET entered.");
                // TODO: Should this be Run or Walk speed?
                __result = MechHelper.CalcRunSpeed(__instance);
            }
        }

        /*
         * MechEngineer.Features.ShutdownInjuryProtection
         * MechEngineer.Features.Engine
         */

        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("MaxWalkDistance", MethodType.Getter)]
        public static class Mech_MaxWalkDistance_Get {
            public static void Postfix(Mech __instance, ref float __result) {
                Mod.Log.Trace("M:MWD:GET entered.");
                __result = MechHelper.CalcWalkSpeed(__instance);
            }
        }

        // TODO: Enforce on vehicles?
        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("MaxSprintDistance", MethodType.Getter)]
        public static class Mech_MaxSprintDistance_Get {
            public static void Postfix(Mech __instance, ref float __result) {
                Mod.Log.Trace("M:MSD:GET entered.");
                __result = MechHelper.CalcRunSpeed(__instance);
            }
        }

        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("MaxBackwardDistance", MethodType.Getter)]
        public static class Mech_MaxBackwardDistance_Get {
            public static void Postfix(Mech __instance, ref float __result) {
                Mod.Log.Trace("M:MBD:GET entered.");
                __result = MechHelper.CalcWalkSpeed(__instance);
            }
        }

        // TODO: Memoize this; its invoked multiple times
        [HarmonyPatch(typeof(ToHit), "GetHeatModifier")]
        public static class ToHit_GetHeatModifier {
            public static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker) {
                Mod.Log.Trace("TH:GHM entered.");
                if (attacker is Mech mech && mech.IsOverheated) {

                    float penalty = 0f;
                    foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                        if (mech.CurrentHeat >= kvp.Key) {
                            penalty = kvp.Value;
                            //Mod.Log.Debug($"  attackPenalty:{penalty} from heat: {mech.CurrentHeat} >= {kvp.Key}");
                        }
                    }

                    __result = penalty;
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechTray), "Init")]
        public static class CombatHUDMechTray_Init {

            // FIXME: Make state var; cleanup on CG destroyed
            public static CombatHUDSidePanelHeatHoverElement HoverElement = null;
            public static CombatHUD HUD = null;

            public static void Postfix(CombatHUDMechTray __instance, CombatHUD ___HUD) {
                Mod.Log.Trace("CHUDMT::Init - entered.");

                if (__instance.gameObject.GetComponentInChildren<CombatHUDHeatDisplay>() == null) {
                    Mod.Log.Info("COULD NOT FIND HEAT DISPLAY");
                } else {
                    Mod.Log.Info("FOUND HEAT DISPLAY");

                    CombatHUDHeatDisplay heatDisplay = __instance.gameObject.GetComponentInChildren<CombatHUDHeatDisplay>();

                    HoverElement = heatDisplay.gameObject.AddComponent<CombatHUDSidePanelHeatHoverElement>();
                    HoverElement.name = "CBTBE_Hover_Element";
                    HoverElement.Init(___HUD);
                }
                HUD = ___HUD;
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechTray), "Update")]
        public static class CombatHUDMechTray_Update {
            public static void Postfix(CombatHUDMechTray __instance) {
                Mod.Log.Trace("CHUDMT::Update - entered.");

                if (__instance.DisplayedActor is Mech displayedMech && CombatHUDMechTray_Init.HoverElement != null) {
                    //Mod.Log.Info("-- UPDATING TOOLTIP DATA.");
                    CombatHUDMechTray_Init.HoverElement.UpdateText(displayedMech);
                }
            }
        }

        // TODO: FIXME
        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowShutDownIndicator", null)]
        public static class CombatHUDStatusPanel_ShowShutDownIndicator {
            public static bool Prefix(CombatHUDStatusPanel __instance) {
                Mod.Log.Trace("CHUBSP:SSDI:PRE entered.");
                return false;
            }

            public static void Postfix(CombatHUDStatusPanel __instance, Mech mech) {
                Mod.Log.Trace("CHUBSP:SSDI:POST entered.");

                // TODO: FIXME
                var type = __instance.GetType();
                MethodInfo methodInfo = type.GetMethod("ShowDebuff", (BindingFlags.NonPublic | BindingFlags.Instance), null, 
                    new Type[] { typeof(SVGAsset), typeof(Text), typeof(Text), typeof(Vector3), typeof(bool) }, new ParameterModifier[5]);

                int turnsOverheated = mech.StatCollection.ContainsStatistic(ModStats.TurnsOverheated) ? mech.StatCollection.GetValue<int>("TurnsOverheated") : 0;

                if (mech.IsShutDown) {
                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(mech)} is shutdown.");
                    methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                        new Text("SHUT DOWN", new object[0]), new Text("This target is easier to hit, and Called Shots can be made against this target.", new object[0]),
                        __instance.defaultIconScale, false });
                } else if (mech.IsOverheated) {
                    float shutdownChance = 0;
                    float ammoExplosionChance = 0;
                    // FIXME: Remove this old code
                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(mech)} is overheated, shutdownChance:{shutdownChance}% ammoExplosionChance:{ammoExplosionChance}%");

                    string descr = string.Format("This unit may trigger a Shutdown at the end of the turn unless heat falls below critical levels." +
                        "\nShutdown Chance: {0:P2}\nAmmo Explosion Chance: {1:P2}", 
                        shutdownChance, ammoExplosionChance);
                    methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                        new Text("OVERHEATING", new object[0]), new Text(descr, new object[0]), __instance.defaultIconScale, false });
                }
            }
        }



    }
}

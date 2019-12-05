
using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Heat;
using CBTBehaviorsEnhanced.Helper;
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
using static CBTBehaviors.HeatHelper;

namespace CBTBehaviors {

    public static class HeatPatches {

        [HarmonyPatch(typeof(Mech), "Init")]
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
                    Mod.Log.Info("AT END OF TURN - UNIT IS HOT, Checking shutdown");

                    float shutdownTarget = 0f;
                    foreach (var item in Mod.Config.Heat.Shutdown.OrderBy(i => i.Key)) {
                        if (__instance.OwningMech.CurrentHeat >= item.Key) {
                            shutdownTarget = item.Value;
                            Mod.Log.Debug($"  Setting shutdown target to {item.Value} as currentHeat: {__instance.OwningMech.CurrentHeat} > bounds: {item.Key}");
                        }
                    }

                    Mod.Log.Debug($"  Shutdown target roll set to: {shutdownTarget}");
                    if (shutdownTarget == 0f) {
                        Mod.Log.Debug($"  Target heat below shutdown targets, skipping.");
                    } else {
                        float pilotMod = HeatHelper.GetPilotMod(__instance.OwningMech);

                        // Calculate the shutdown chance: a random roll, plus the piloting skill
                        float shutdownRoll = __instance.OwningMech.Combat.NetworkRandom.Float();
                        float shutdownCheckResult = shutdownRoll + pilotMod;
                        Mod.Log.Debug($"  pilotMod: {pilotMod} + roll: {shutdownRoll} = shutdownCheckResult: {shutdownCheckResult}");

                        // Calculate a fall chance; a random roll, plus the piloting skill
                        float fallRoll = __instance.OwningMech.Combat.NetworkRandom.Float();
                        float fallCheckResult = fallRoll + pilotMod;
                        Mod.Log.Debug($"  pilotMod: {pilotMod} + roll: {fallRoll} = fallCheckResult: {fallCheckResult}");
                        
                        MultiSequence sequence = new MultiSequence(__instance.OwningMech.Combat);
                        // Possible sequences
                        //  Shutdown
                        //  Fall from shutdown
                        //  Ammo Explosion
                        //  System damage
                        //  Pilot injury
                        //  Pilot death
                        if (shutdownTarget == -1f) {
                            // Unit must shutdown!
                            Mod.Log.Debug($"  currentHeat: {__instance.OwningMech.CurrentHeat} is beyond forced shutdown mark: {shutdownTarget} -" +
                                $" Must be forced into a shutdown!.");
                            //sequence.SetCamera(CameraControl.Instance.ShowDeathCam(__instance.OwningMech, false, -1f), 0);

                            string debuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Forced]).ToString();
                            sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, debuffText,
                                FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                            MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance.OwningMech);
                            mechShutdownSequence.RootSequenceGUID = __instance.SequenceGUID;
                            sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                            if (fallCheckResult < Mod.Config.Heat.ShutdownFallThreshold) {
                                Mod.Log.Info("Pilot check from shutdown failed! Forcing a fall!");

                                string fallDebuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Fall]).ToString();
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDebuffText,
                                    FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                                string fallDetailsText = $"{fallDebuffText} - {fallCheckResult:P1} < {Mod.Config.Heat.ShutdownFallThreshold:P1}";
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDetailsText,
                                    FloatieMessage.MessageNature.Neutral, true), sequence.ChildSequenceCount - 1);

                                MechFallSequence mfs = new MechFallSequence(__instance.OwningMech, "Overheat", new Vector2(0f, -1f));
                                mfs.RootSequenceGUID = __instance.SequenceGUID;
                                sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                            } else {
                                Mod.Log.Info($"Pilot check to avoid falling passed.");
                            }
                        } else {
                            if (shutdownCheckResult >= shutdownTarget) {
                                Mod.Log.Debug("  Shutdown override skill check passed.");

                                string buffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Override]).ToString();
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, buffText,
                                    FloatieMessage.MessageNature.Buff, true), sequence.ChildSequenceCount - 1);

                                string detailsText = $"{buffText} - {shutdownCheckResult:P1} > {shutdownTarget:P1}";
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, detailsText,
                                    FloatieMessage.MessageNature.Neutral, true), sequence.ChildSequenceCount - 1);

                            } else {
                                Mod.Log.Debug("  Shutdown override skill check failed, shutting down");
                                //sequence.SetCamera(CameraControl.Instance.ShowDeathCam(__instance.OwningMech, false, -1f), 0);

                                string debuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Failed_Overide]).ToString();
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, debuffText,
                                    FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                                string detailsText = $"{debuffText} - {shutdownCheckResult:P1} < {shutdownTarget:P1}";
                                sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, detailsText,
                                    FloatieMessage.MessageNature.Neutral, true), sequence.ChildSequenceCount - 1);

                                MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance.OwningMech);
                                mechShutdownSequence.RootSequenceGUID = __instance.SequenceGUID;
                                sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                                if (fallCheckResult < Mod.Config.Heat.ShutdownFallThreshold) {
                                    Mod.Log.Info("Pilot check from shutdown failed! Forcing a fall!");

                                    string fallDebuffText = new Text(Mod.Config.Floaties[ModConfig.FT_Shutdown_Fall]).ToString();
                                    sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDebuffText,
                                        FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);

                                    string fallDetailsText = $"{fallDebuffText} - {fallCheckResult:P1} < {Mod.Config.Heat.ShutdownFallThreshold:P1}";
                                    sequence.AddChildSequence(new ShowActorInfoSequence(__instance.OwningMech, fallDetailsText,
                                        FloatieMessage.MessageNature.Neutral, true), sequence.ChildSequenceCount - 1);

                                    MechFallSequence mfs = new MechFallSequence(__instance.OwningMech, "Overheat", new Vector2(0f, -1f));
                                    mfs.RootSequenceGUID = __instance.SequenceGUID;
                                    sequence.AddChildSequence(mfs, sequence.ChildSequenceCount - 1);
                                } else {
                                    Mod.Log.Info($"Pilot check to avoid falling passed.");
                                }
                            }
                        }

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

                //MultiSequence sequence = new MultiSequence(__instance.Combat);
                //sequence.SetCamera(CameraControl.Instance.ShowDeathCam(__instance, false, -1f), 0);

                //if (__instance.IsOverheated) {
                //    CBTPilotingRules rules = new CBTPilotingRules(__instance.Combat);
                //    float gutsTestChance = rules.GetGutsModifier(__instance);
                //    float skillRoll = __instance.Combat.NetworkRandom.Float();
                //    float ammoRoll = __instance.Combat.NetworkRandom.Float();

                //    int turnsOverheated = __instance.StatCollection.ContainsStatistic(ModStats.TurnsOverheated) ? __instance.StatCollection.GetValue<int>("TurnsOverheated") : 0;
                //    float shutdownPercentage = HeatHelper.GetShutdownPercentageForTurn(turnsOverheated);
                //    float ammoExplosionPercentage = HeatHelper.GetAmmoExplosionPercentageForTurn(turnsOverheated);

                //    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(__instance)} is overheated for {turnsOverheated} turns. Checking shutdown override.");
                //    Mod.Log.Debug($"  Guts -> skill: {__instance.SkillGuts}  divisor: {Mod.Config.GutsDivisor}  bonus: {gutsTestChance}");
                //    Mod.Log.Debug($"  Skill roll: {skillRoll} plus guts roll: {skillRoll + gutsTestChance}  target: {shutdownPercentage}");
                //    Mod.Log.Debug($"  Ammo roll: {ammoRoll} plus guts roll: {ammoRoll + gutsTestChance}  target: {ammoExplosionPercentage}");

                //    if (Mod.Config.UseGuts) {
                //        ammoRoll = ammoRoll + gutsTestChance;
                //        skillRoll = skillRoll + gutsTestChance;
                //    }

                //    if (HeatHelper.CanAmmoExplode(__instance)) {
                //        if (ammoRoll < ammoExplosionPercentage) {
                //            __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "Ammo Overheated!", FloatieMessage.MessageNature.CriticalHit));

                //            var ammoBox = __instance.ammoBoxes.Where(box => box.CurrentAmmo > 0)
                //                .OrderByDescending(box => box.CurrentAmmo / box.AmmoCapacity)
                //                .FirstOrDefault();
                //            if (ammoBox != null) {
                //                WeaponHitInfo fakeHit = new WeaponHitInfo(stackItemID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null, new AttackDirection[] { AttackDirection.None }, null, null, null);
                //                ammoBox.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                //            }

                //            return;
                //        }

                //        sequence.AddChildSequence(new ShowActorInfoSequence(__instance, "Ammo Explosion Avoided!", FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);
                //    }

                //    sequence.AddChildSequence(new DelaySequence(__instance.Combat, 2f), sequence.ChildSequenceCount - 1);
                //    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                //} else {
                //    int turnsOverheated = __instance.StatCollection.GetValue<int>("TurnsOverheated");
                //    if (turnsOverheated > 0) {
                //        __instance.StatCollection.Set<int>("TurnsOverheated", 0);
                //    }
                //}
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

        [HarmonyPatch(typeof(ToHit), "GetHeatModifier")]
        public static class ToHit_GetHeatModifier {
            public static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker) {
                Mod.Log.Trace("TH:GHM entered.");
                if (attacker is Mech mech && mech.StatCollection.ContainsStatistic(ModStats.TurnsOverheated)) {

                    int turnsOverheated = mech.StatCollection.GetValue<int>(ModStats.TurnsOverheated);
                    if (turnsOverheated > 0) {
                        float modifier = HeatHelper.GetHeatToHitModifierForTurn(turnsOverheated);
                        __result = modifier;
                        Mod.Log.Debug($"Mech {CombatantHelper.LogLabel(mech)} has overheat ToHit modifier:{modifier}");
                    } else {
                        __result = 0f;
                    }
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
                    HoverElement.name = "GBK_HOVER";
                    HoverElement.Init(___HUD);
                    Mod.Log.Info($"CREATED HEAT TOOLTIP WITH CHUDHD: {__instance.GetInstanceID()}");
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
                    float shutdownChance = HeatHelper.GetShutdownPercentageForTurn(turnsOverheated);
                    float ammoExplosionChance = HeatHelper.GetAmmoExplosionPercentageForTurn(turnsOverheated);
                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(mech)} is overheated, shutdownChance:{shutdownChance}% ammoExplosionChance:{ammoExplosionChance}%");

                    string descr = string.Format("This unit may trigger a Shutdown at the end of the turn unless heat falls below critical levels.\nShutdown Chance: {0:P2}\nAmmo Explosion Chance: {1:P2}", 
                        shutdownChance, ammoExplosionChance);
                    methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                        new Text("OVERHEATING", new object[0]), new Text(descr, new object[0]), __instance.defaultIconScale, false });
                }
            }
        }



    }
}

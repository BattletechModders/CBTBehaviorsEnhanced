using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Heat;
using Harmony;
using HBS;
using Localize;
using SVGImporter;
using System;
using System.Reflection;
using UnityEngine;
using us.frostraptor.modUtils;
using static CBTBehaviorsEnhanced.HeatHelper;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(CombatHUDHeatMeter), "GetMaxOuterHeatLevel")]
    public static class CombatHUDHeatMeter_GetMaxOuterHeatLevel {

        // Resize the heat bar to its original value
        public static void Postfix(CombatHUDHeatMeter __instance, Mech mech, ref int __result) {
            Mod.Log.Trace("CHUDHD:GMOHL - entered.");
            //Mod.Log.Debug($"MAXOUTERHEATLEVEL FOR: {CombatantUtils.Label(mech)} = {__result}");
            __result = (int)(Mod.Config.Heat.WarnAtHeat * 1.75f);
        }
    }


    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "DangerLevel", MethodType.Getter)]
    public static class CombatHUDHeatDisplay_DangerLevel_Getter {
        public static void Postfix(CombatHUDHeatDisplay __instance, ref float __result) {
            Mod.Log.Trace("CHUDHD:DL - entered.");
            // Mod.Log.Debug($" DangerLevel: {__result} for mech: {CombatantUtils.Label(displayedMech)} == Overheat level: {displayedMech.OverheatLevel} / MaxHeat: {displayedMech.MaxHeat}");
            __result = ((float)Mod.Config.Heat.WarnAtHeat / (float)Mod.Config.Heat.MaxHeat);
            // Mod.Log.Debug($"   Updated result to: {__result}");
        }
    }

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "RefreshInfo")]
    [HarmonyPatch()]
    public static class CombatHUDHeatDisplay_RefreshInfo {
        public static void Postfix(CombatHUDHeatDisplay __instance) {
            Mod.Log.Trace("CHUDHD:RI - entered.");
            // Disable the overheating icon... because it sucks.
            __instance.OverHeatedIcon.SetActive(false);
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
                Mod.Log.Warn("COULD NOT FIND HEAT DISPLAY");
            } else {
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
                CombatHUDMechTray_Init.HoverElement.UpdateText(displayedMech);
            }
        }
    }

    // TODO: FIXME - should trigger on guesstimated heat as well.
    [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowShutDownIndicator", null)]
    public static class CombatHUDStatusPanel_ShowShutDownIndicator {
        public static bool Prefix(CombatHUDStatusPanel __instance) {
            Mod.Log.Trace("CHUBSP:SSDI:PRE entered.");
            return false;
        }

        public static void Postfix(CombatHUDStatusPanel __instance, Mech mech) {
            Mod.Log.Trace("CHUBSP:SSDI:POST entered.");

            var type = __instance.GetType();
            MethodInfo methodInfo = type.GetMethod("ShowDebuff", (BindingFlags.NonPublic | BindingFlags.Instance), null,
                new Type[] { typeof(SVGAsset), typeof(Text), typeof(Text), typeof(Vector3), typeof(bool) }, new ParameterModifier[5]);

            Traverse HUDT = Traverse.Create(__instance).Property("HUD");
            CombatHUD HUD = HUDT.GetValue<CombatHUD>();
            
            CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(mech, HUD.SelectionHandler.ProjectedHeatForState);
            Mod.Log.Debug($" In ShutdownIndicator, projectedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");

            if (mech.IsShutDown) {
                Mod.Log.Info($" MECH {CombatantUtils.Label(mech)} IS SHUTDOWN, DISPLAYING SHUTDOWN WARNING");
                methodInfo.Invoke(__instance, new object[] { 
                    LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TITLE]),
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TEXT]),
                    __instance.defaultIconScale, false });
            } else if (calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat) {
                Mod.Log.Info($"DISPLAYING OVERHEAT WARNING FOR MECH {CombatantUtils.Label(mech)} - heat {calculatedHeat.ThresholdHeat} >= warningHeat: {Mod.Config.Heat.WarnAtHeat}");
                methodInfo.Invoke(__instance, new object[] {
                    LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_OVERHEAT_TITLE]),
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_OVERHEAT_TEXT]),
                    __instance.defaultIconScale, false });
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "Update")]
    public static class CombatHUDAttackModeSelector_Update {
        public static void Postfix(CombatHUDAttackModeSelector __instance) {
            Mod.Log.Trace("CHUDAMS:U - entered.");

            Traverse HUDT = Traverse.Create(__instance).Property("HUD");
            CombatHUD HUD = HUDT.GetValue<CombatHUD>();
            if (HUD != null && HUD.SelectedActor != null && HUD.SelectedActor is Mech) {

                Traverse showHeatWarningsT = Traverse.Create(__instance).Field("showHeatWarnings");
                bool showHeatWarnings = showHeatWarningsT.GetValue<bool>();
                if (showHeatWarnings) {

                    CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(HUD.SelectedActor as Mech, HUD.SelectionHandler.ProjectedHeatForState);
                    //Mod.Log.Debug($" In CombatHUDAttackModeSelector, projectedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");
                    bool isOverheated = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat;
                    bool isShutdown = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.MaxHeat;

                    Traverse updateOverheatWarningsT = Traverse.Create(__instance).Method("UpdateOverheatWarnings", new object[] { isOverheated, isShutdown });
                    updateOverheatWarningsT.GetValue();
                }

            }

        }
    }
}

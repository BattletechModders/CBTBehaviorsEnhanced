using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Heat;
using Harmony;
using HBS;
using IRBTModUtils.Extension;
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
            Mod.HeatLog.Trace?.Write("CHUDHD:GMOHL - entered.");
            //Mod.HeatLog.Debug?.Write($"MAXOUTERHEATLEVEL FOR: {CombatantUtils.Label(mech)} = {__result}");
            __result = (int)(Mod.Config.Heat.WarnAtHeat * 1.75f);
        }
    }


    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "Init")]
    [HarmonyPatch(new Type[] { typeof(float) } )]
    public static class CombatHUDHeatDisplay_Init
    {
        public static void Postfix(CombatHUDHeatDisplay __instance)
        {
            Mod.HeatLog.Trace?.Write("CHUDHD:I - entered.");
            if (__instance.DisplayedActor != null && __instance.DisplayedActor is Mech displayedMech)
            {
                Traverse origWidthT = Traverse.Create(__instance).Property("origWidth");
                float origWidth = origWidthT.GetValue<float>();

                Traverse rectTransformT = Traverse.Create(__instance).Property("rectTransform");
                RectTransform rectTransform = rectTransformT.GetValue<RectTransform>();

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origWidth);
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "DangerLevel", MethodType.Getter)]
    public static class CombatHUDHeatDisplay_DangerLevel_Getter {
        public static void Postfix(CombatHUDHeatDisplay __instance, ref float __result) {
            Mod.HeatLog.Trace?.Write("CHUDHD:DL - entered.");
            // Mod.HeatLog.Debug?.Write($" DangerLevel: {__result} for mech: {CombatantUtils.Label(displayedMech)} == Overheat level: {displayedMech.OverheatLevel} / MaxHeat: {displayedMech.MaxHeat}");
            __result = ((float)Mod.Config.Heat.WarnAtHeat / (float)Mod.Config.Heat.MaxHeat);
            // Mod.HeatLog.Debug?.Write($"   Updated result to: {__result}");
        }
    }

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "RefreshInfo")]
    [HarmonyPatch()]
    public static class CombatHUDHeatDisplay_RefreshInfo {
        public static void Postfix(CombatHUDHeatDisplay __instance) {
            Mod.HeatLog.Trace?.Write("CHUDHD:RI - entered.");
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
            Mod.HeatLog.Trace?.Write("CHUDMT::Init - entered.");

            if (__instance.gameObject.GetComponentInChildren<CombatHUDHeatDisplay>() == null) {
                Mod.HeatLog.Warn?.Write("COULD NOT FIND HEAT DISPLAY");
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
            Mod.HeatLog.Trace?.Write("CHUDMT::Update - entered.");

            if (__instance.DisplayedActor is Mech displayedMech && CombatHUDMechTray_Init.HoverElement != null) {
                CombatHUDMechTray_Init.HoverElement.UpdateText(displayedMech);
            }
        }
    }

    // TODO: FIXME - should trigger on guesstimated heat as well.
    [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowShutDownIndicator", null)]
    public static class CombatHUDStatusPanel_ShowShutDownIndicator {
        public static bool Prefix(CombatHUDStatusPanel __instance) {
            Mod.HeatLog.Trace?.Write("CHUBSP:SSDI:PRE entered.");
            return false;
        }

        public static void Postfix(CombatHUDStatusPanel __instance, Mech mech) {
            Mod.HeatLog.Trace?.Write("CHUBSP:SSDI:POST entered.");

            var type = __instance.GetType();
            MethodInfo methodInfo = type.GetMethod("ShowDebuff", (BindingFlags.NonPublic | BindingFlags.Instance), null,
                new Type[] { typeof(SVGAsset), typeof(Text), typeof(Text), typeof(Vector3), typeof(bool) }, new ParameterModifier[5]);

            Traverse HUDT = Traverse.Create(__instance).Property("HUD");
            CombatHUD HUD = HUDT.GetValue<CombatHUD>();
            
            CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(mech, HUD.SelectionHandler.ProjectedHeatForState);
            Mod.HeatLog.Debug?.Write($"In ShutdownIndicator, projectedHeat {HUD.SelectionHandler.ProjectedHeatForState} => calculatedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");
            Mod.HeatLog.Debug?.Write($"  current: {calculatedHeat.CurrentHeat} projected: {calculatedHeat.ProjectedHeat} temp: {calculatedHeat.TempHeat}  " +
                $"sinkable: {calculatedHeat.SinkableHeat}  sinkCapacity: {calculatedHeat.OverallSinkCapacity}  future: {calculatedHeat.FutureHeat}  threshold: {calculatedHeat.ThresholdHeat}");
            Mod.HeatLog.Debug?.Write($"  CACTerrainHeat{ calculatedHeat.CACTerrainHeat}  CurrentPathNodes: {calculatedHeat.CurrentPathNodes}  isProjectedHeat: {calculatedHeat.IsProjectedHeat}");

            if (mech.IsShutDown) {
                Mod.HeatLog.Info?.Write($" Mech {CombatantUtils.Label(mech)} is shutdown, displaying the shutdown warning");
                methodInfo.Invoke(__instance, new object[] { 
                    LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TITLE]),
                    new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TEXT]),
                    __instance.defaultIconScale, false });
            } else if (calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat) {
                Mod.HeatLog.Info?.Write($"Mech {mech.DistinctId()} has thresholdHeat {calculatedHeat.ThresholdHeat} >= warningHeat: {Mod.Config.Heat.WarnAtHeat}. Displaying heat warning.");
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
            Mod.HeatLog.Trace?.Write("CHUDAMS:U - entered.");

            Traverse HUDT = Traverse.Create(__instance).Property("HUD");
            CombatHUD HUD = HUDT.GetValue<CombatHUD>();
            if (HUD != null && HUD.SelectedActor != null && HUD.SelectedActor is Mech) {

                Traverse showHeatWarningsT = Traverse.Create(__instance).Field("showHeatWarnings");
                bool showHeatWarnings = showHeatWarningsT.GetValue<bool>();
                if (showHeatWarnings) {

                    CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(HUD.SelectedActor as Mech, HUD.SelectionHandler.ProjectedHeatForState);
                    //Mod.HeatLog.Debug?.Write($" In CombatHUDAttackModeSelector, projectedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");
                    bool isOverheated = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat;
                    bool isShutdown = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.MaxHeat;

                    Traverse updateOverheatWarningsT = Traverse.Create(__instance).Method("UpdateOverheatWarnings", new object[] { isOverheated, isShutdown });
                    updateOverheatWarningsT.GetValue();
                }

            }

        }
    }
}

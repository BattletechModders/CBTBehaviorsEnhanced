using BattleTech.UI;
using CBTBehaviorsEnhanced.Heat;
using HBS;
using IRBTModUtils.Extension;
using Localize;
using SVGImporter;
using System;
using System.Reflection;
using UnityEngine;
using us.frostraptor.modUtils;
using static CBTBehaviorsEnhanced.HeatHelper;

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(CombatHUDHeatMeter), "GetMaxOuterHeatLevel")]
    public static class CombatHUDHeatMeter_GetMaxOuterHeatLevel
    {

        // Resize the heat bar to its original value
        public static void Postfix(CombatHUDHeatMeter __instance, Mech mech, ref int __result)
        {
            Mod.UILog.Trace?.Write("CHUDHD:GMOHL - entered.");
            //Mod.UILog.Debug?.Write($"MAXOUTERHEATLEVEL FOR: {CombatantUtils.Label(mech)} = {__result}");
            __result = (int)(Mod.Config.Heat.WarnAtHeat * 1.75f);
        }
    }


    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "Init")]
    [HarmonyPatch(new Type[] { typeof(float) })]
    public static class CombatHUDHeatDisplay_Init
    {
        public static void Postfix(CombatHUDHeatDisplay __instance)
        {
            Mod.UILog.Trace?.Write("CHUDHD:I - entered.");
            if (__instance.DisplayedActor != null && __instance.DisplayedActor is Mech displayedMech)
            {
                float origWidth = __instance.origWidth;
                RectTransform rectTransform = __instance.rectTransform;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origWidth);
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "DangerLevel", MethodType.Getter)]
    public static class CombatHUDHeatDisplay_DangerLevel_Getter
    {
        public static void Postfix(CombatHUDHeatDisplay __instance, ref float __result)
        {
            Mod.UILog.Trace?.Write("CHUDHD:DL - entered.");
            // Mod.UILog.Debug?.Write($" DangerLevel: {__result} for mech: {CombatantUtils.Label(displayedMech)} == Overheat level: {displayedMech.OverheatLevel} / MaxHeat: {displayedMech.MaxHeat}");
            __result = ((float)Mod.Config.Heat.WarnAtHeat / (float)Mod.Config.Heat.MaxHeat);
            // Mod.UILog.Debug?.Write($"   Updated result to: {__result}");
        }
    }

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "RefreshInfo")]
    [HarmonyPatch()]
    public static class CombatHUDHeatDisplay_RefreshInfo
    {
        public static void Postfix(CombatHUDHeatDisplay __instance)
        {
            Mod.UILog.Trace?.Write("CHUDHD:RI - entered.");
            // Disable the overheating icon... because it sucks.
            __instance.OverHeatedIcon.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(CombatHUDMechTray), "Init")]
    public static class CombatHUDMechTray_Init
    {

        // FIXME: Make state var; cleanup on CG destroyed
        public static CombatHUDSidePanelHeatHoverElement HoverElement = null;
        public static CombatHUD HUD = null;

        public static void Postfix(CombatHUDMechTray __instance)
        {
            Mod.UILog.Trace?.Write("CHUDMT::Init - entered.");

            if (__instance.gameObject.GetComponentInChildren<CombatHUDHeatDisplay>() == null)
            {
                Mod.UILog.Warn?.Write("COULD NOT FIND HEAT DISPLAY");
            }
            else
            {
                CombatHUDHeatDisplay heatDisplay = __instance.gameObject.GetComponentInChildren<CombatHUDHeatDisplay>();

                HoverElement = heatDisplay.gameObject.AddComponent<CombatHUDSidePanelHeatHoverElement>();
                HoverElement.name = "CBTBE_Hover_Element";
                HoverElement.Init(__instance.HUD);
            }
            HUD = __instance.HUD;
        }
    }

    [HarmonyPatch(typeof(CombatHUDMechTray), "Update")]
    public static class CombatHUDMechTray_Update
    {
        public static void Postfix(CombatHUDMechTray __instance)
        {
            Mod.UILog.Trace?.Write("CHUDMT::Update - entered.");

            if (__instance.DisplayedActor is Mech displayedMech && CombatHUDMechTray_Init.HoverElement != null)
            {
                CombatHUDMechTray_Init.HoverElement.UpdateText(displayedMech);
            }
        }
    }

    // TODO: FIXME - should trigger on guesstimated heat as well.
    [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowShutDownIndicator", null)]
    static class CombatHUDStatusPanel_ShowShutDownIndicator
    {
        static void Prefix(ref bool __runOriginal, CombatHUDStatusPanel __instance)
        {
            Mod.UILog.Trace?.Write("CHUBSP:SSDI:PRE entered.");
            __runOriginal = false;
        }

        static void Postfix(CombatHUDStatusPanel __instance, Mech mech)
        {
            Mod.UILog.Trace?.Write("CHUBSP:SSDI:POST entered.");

            CombatHUD HUD = __instance.HUD;

            if (HUD == null || HUD.SelectionHandler == null) return;
            
            try
            {
                CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(mech, HUD.SelectionHandler.ProjectedHeatForState);
                Mod.UILog.Debug?.Write($"In ShutdownIndicator, projectedHeat {HUD.SelectionHandler.ProjectedHeatForState} => calculatedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");
                Mod.UILog.Debug?.Write($"  current: {calculatedHeat.CurrentHeat} projected: {calculatedHeat.ProjectedHeat} temp: {calculatedHeat.TempHeat}  " +
                    $"sinkable: {calculatedHeat.SinkableHeat}  sinkCapacity: {calculatedHeat.OverallSinkCapacity}  future: {calculatedHeat.FutureHeat}  threshold: {calculatedHeat.ThresholdHeat}");
                Mod.UILog.Debug?.Write($"  CACTerrainHeat{calculatedHeat.CACTerrainHeat}  CurrentPathNodes: {calculatedHeat.CurrentPathNodes}  isProjectedHeat: {calculatedHeat.IsProjectedHeat}");

                if (mech.IsShutDown)
                {
                    Mod.UILog.Info?.Write($" Mech {CombatantUtils.Label(mech)} is shutdown, displaying the shutdown warning");
                    __instance.ShowDebuff(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                        new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TITLE]),
                        new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_SHUTDOWN_TEXT]),
                        __instance.defaultIconScale, false);
                }
                else if (calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat)
                {
                    Mod.UILog.Info?.Write($"Mech {mech.DistinctId()} has thresholdHeat {calculatedHeat.ThresholdHeat} >= warningHeat: {Mod.Config.Heat.WarnAtHeat}. Displaying heat warning.");
                    __instance.ShowDebuff(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                        new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_OVERHEAT_TITLE]),
                        new Text(Mod.LocalizedText.Tooltips[ModText.CHUDSP_TT_WARN_OVERHEAT_TEXT]),
                        __instance.defaultIconScale, false);
                }
            }
            catch (Exception e)
            {
                Mod.UILog.Warn?.Write(e, "Failed to update CHUDStatusPanel due to error!");
            }


        }
    }

    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "Update")]
    public static class CombatHUDAttackModeSelector_Update
    {
        public static void Postfix(CombatHUDAttackModeSelector __instance)
        {
            Mod.UILog.Trace?.Write("CHUDAMS:U - entered.");

            CombatHUD HUD = __instance.HUD;
            if (HUD != null && HUD.SelectedActor != null && HUD.SelectedActor is Mech)
            {

                bool showHeatWarnings = __instance.showHeatWarnings;
                if (showHeatWarnings)
                {
                    CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(HUD.SelectedActor as Mech, HUD.SelectionHandler.ProjectedHeatForState);
                    //Mod.UILog.Debug?.Write($" In CombatHUDAttackModeSelector, projectedHeat: {calculatedHeat.ThresholdHeat} vs {Mod.Config.Heat.WarnAtHeat}");
                    bool isOverheated = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.WarnAtHeat;
                    bool isShutdown = calculatedHeat.ThresholdHeat >= Mod.Config.Heat.MaxHeat;

                    __instance.UpdateOverheatWarnings(isOverheated, isShutdown);
                }

            }

        }
    }
}

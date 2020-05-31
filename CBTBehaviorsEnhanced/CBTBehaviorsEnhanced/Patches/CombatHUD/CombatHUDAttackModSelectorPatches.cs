using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "Init")]
    [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
    static class CombatHUDAttackModeSelector_Init
    {

        static CombatHUDFireButton CloneCHUDFireButton(GameObject parent, CombatHUDFireButton cloneSource, string goName, string label, CombatGameState Combat, CombatHUD HUD)
        {
            CombatHUDFireButton newButton = GameObject.Instantiate<CombatHUDFireButton>(cloneSource);
            newButton.Init(Combat, HUD);
            newButton.gameObject.transform.parent = parent.transform;
            Mod.Log.Trace($"Created {goName} and attached to parent.");

            newButton.gameObject.name = goName;
            newButton.gameObject.transform.SetAsFirstSibling();
            newButton.gameObject.transform.localScale = Vector3.one;

            RectTransform rectTransform = newButton.gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(240f, 70f); // Default is 160, 80

            Image image = newButton.gameObject.GetComponent<Image>();
            image.color = new Color(255f, 255f, 255f, 204f);

            LayoutElement layoutElement = newButton.gameObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = 240f;
            layoutElement.preferredWidth = 240f;
            layoutElement.minHeight = 70f;
            layoutElement.preferredHeight = 70f;
            layoutElement.ignoreLayout = false;

            GameObject punchOverheatWarn = newButton.gameObject.transform.Find("overheatWarn").gameObject;
            punchOverheatWarn.SetActive(false);
            GameObject punchShutdownWarn = newButton.gameObject.transform.Find("shutdownWarn").gameObject;
            punchShutdownWarn.SetActive(false);
            GameObject sideWedges = newButton.gameObject.transform.Find("confirmFrame_sideWedges (1)").gameObject;
            sideWedges.SetActive(false);
       
            Mod.Log.Trace($"Redid layout for {goName}.");
            return newButton;
        }

        static void Postfix(CombatHUDAttackModeSelector __instance, CombatGameState Combat, CombatHUD HUD)
        {
            try
            {
                Mod.Log.Trace($"CREATING TEST COMPONENTS: instance is null? {__instance == null}  instanceGO is null? {__instance?.gameObject == null}");

                // Find icPanel_Layout as the parent
                Transform icPanelLayoutTransform = __instance.FireButton.gameObject.transform.parent;
                GameObject icPanelLayoutGO = icPanelLayoutTransform.gameObject;
                if (icPanelLayoutGO == null) Mod.Log.Warn("FAILED TO FIND IC_PANEL_LAYOUT!");

                VerticalLayoutGroup vlg = icPanelLayoutGO.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.spacing = 8f;

                GameObject container = new GameObject();
                container.transform.parent = icPanelLayoutTransform;
                container.transform.SetSiblingIndex(1); // Move us above the description container
                container.layer = 5; // everyting else is at this level
                container.name = ModConsts.Container_GO_ID;
                if (container == null) Mod.Log.Warn("FAILED TO ADD CONTAINER!");

                RectTransform containerRectTransform = container.AddComponent<RectTransform>();
                containerRectTransform.localScale = Vector3.one;

                HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.Log.Warn("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childAlignment = TextAnchor.LowerLeft;
                hlg.spacing = 16f;
                hlg.gameObject.SetActive(true);

                LayoutElement le = container.AddComponent<LayoutElement>();
                if (le == null) Mod.Log.Warn("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 75f;
                le.preferredWidth = 500f;

                Mod.Log.Trace($"CREATING BUTTONS");
                ModState.PunchFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PunchFB_GO_ID, "PUNCH", Combat, HUD);
                ModState.KickFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.KickFB_GO_ID, "KICK", Combat, HUD);
                ModState.ChargeFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.ChargeFB_GO_ID, "CHARGE", Combat, HUD);
            }
            catch (Exception e)
            {
                Mod.Log.Error($"Failed to create melee buttons!", e);
            }

        }
    }

    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "ShowFireButton")]
    static class CombatHUDAttackModeSelector_ShowFireButton
    {
        public static void Prefix(CombatHUDAttackModeSelector __instance, CombatHUDFireButton.FireMode mode, string additionalDetails, bool showHeatWarnings)
        {
            Mod.Log.Debug($"ShowFireButton called with mode: {mode}");
            if (mode == CombatHUDFireButton.FireMode.Engage || mode == CombatHUDFireButton.FireMode.Reserve)
            {
                Mod.Log.Info($"Enabling all CHUD_Fire_Buttons");
                MeleeState meleeState = MeleeHelper.GetMeleeState(
                    ModState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    ModState.CombatHUD.SelectionHandler.ActiveState.PreviewPos,
                    ModState.CombatHUD.SelectionHandler.ActiveState.TargetedCombatant
                    );
                if (ModState.ChargeFB != null && meleeState.CanPunch) ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;

                if (ModState.KickFB != null && meleeState.CanKick) ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                if (ModState.PunchFB != null && meleeState.CanCharge) ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
            }
            else
            {
                Mod.Log.Info($"Disabling all CHUD_Fire_Buttons");
                if (ModState.ChargeFB != null) ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.KickFB != null) ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.PunchFB != null) ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "CurrentFireMode", MethodType.Setter)]
    static class CombatHUDFireButton_CurrentFireMode_Setter
    {
        static void Postfix(CombatHUDFireButton __instance)
        {

            if (__instance.gameObject != null)
            {
                if (__instance.gameObject.name == ModConsts.ChargeFB_GO_ID)
                {
                    Mod.Log.Info($"UPDATING CURRENT FIRE MODE: CHARGE");
                    __instance.SetState(ButtonState.Enabled, false);
                    __instance.FireText.SetText("CHARGE", new object[] { }); 
                }
                else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
                {
                    Mod.Log.Info($"UPDATING CURRENT FIRE MODE: KICK");
                    __instance.FireText.SetText("KICK", new object[] { });
                    __instance.SetState(ButtonState.Enabled, false);
                }
                else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
                {
                    Mod.Log.Info($"UPDATING CURRENT FIRE MODE: PUNCH");
                    __instance.FireText.SetText("PUNCH", new object[] { });
                    __instance.SetState(ButtonState.Enabled, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "OnClick")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDFireButton_OnClick
    {
        static void Prefix(CombatHUDFireButton __instance)
        {
            Mod.Log.Info($"CHUDFB - OnClick FIRED!");
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "Update")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDFireButton_Update
    {
        static void Prefix(CombatHUDFireButton __instance)
        {

            if (__instance == null || __instance.gameObject == null || !__instance.gameObject.name.StartsWith("cbtbe")) return; // nothing to do

            //Mod.Log.Info($"CHUDFB - Update FIRED FOR: {__instance.gameObject.name} AND FIREMODE: {__instance.CurrentFireMode}");

            if (__instance.CurrentFireMode == CombatHUDFireButton.FireMode.Engage || __instance.CurrentFireMode == CombatHUDFireButton.FireMode.Reserve)
            {
                //__instance.SetState(ButtonState.Enabled, false);
                //if (__instance.gameObject.name == ModConsts.ChargeFB_GO_ID)
                //{
                //    Mod.Log.Info($"UPDATING TYPE: CHARGE");

                //}
                //else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
                //{
                //    Mod.Log.Info($"UPDATING TYPE: KICK");

                //}
                //else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
                //{
                //    Mod.Log.Info($"UPDATING TYPE: PUNCH");

                //}
            }
        }
    }

}

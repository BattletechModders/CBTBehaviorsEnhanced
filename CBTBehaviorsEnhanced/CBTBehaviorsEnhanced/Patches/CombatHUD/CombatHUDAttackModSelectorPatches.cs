using BattleTech;
using BattleTech.UI;
using BattleTech.WeaponFilters;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "Init")]
    [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
    static class CombatHUDAttackModeSelector_Init
    {

        static CombatHUDFireButton CloneCHUDFireButton(GameObject parent, CombatHUDFireButton cloneSource, string goName, CombatGameState Combat, CombatHUD HUD)
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

                ModState.MeleeAttackContainer = new GameObject();
                ModState.MeleeAttackContainer.transform.parent = icPanelLayoutTransform;
                ModState.MeleeAttackContainer.transform.SetSiblingIndex(1); // Move us above the description container
                ModState.MeleeAttackContainer.layer = 5; // everyting else is at this level
                ModState.MeleeAttackContainer.name = ModConsts.Container_GO_ID;
                if (ModState.MeleeAttackContainer == null) Mod.Log.Warn("FAILED TO ADD CONTAINER!");

                RectTransform containerRectTransform = ModState.MeleeAttackContainer.AddComponent<RectTransform>();
                containerRectTransform.localScale = Vector3.one;

                HorizontalLayoutGroup hlg = ModState.MeleeAttackContainer.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.Log.Warn("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childAlignment = TextAnchor.LowerLeft;
                hlg.spacing = 16f;
                hlg.gameObject.SetActive(true);

                LayoutElement le = ModState.MeleeAttackContainer.AddComponent<LayoutElement>();
                if (le == null) Mod.Log.Warn("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 75f;
                le.preferredWidth = 500f;

                // Reverse order, as the first one created is the right-most
                Mod.Log.Trace($"CREATING BUTTONS");
                ModState.PunchFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PunchFB_GO_ID, Combat, HUD);
                ModState.PhysicalWeaponFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PhysicalWeaponFB_GO_ID, Combat, HUD);
                ModState.KickFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.KickFB_GO_ID, Combat, HUD);
                ModState.ChargeFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.ChargeFB_GO_ID, Combat, HUD);
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
            if (mode == CombatHUDFireButton.FireMode.Engage && ModState.CombatHUD.SelectionHandler.ActiveState != null)
            {
                Mod.Log.Info($"Enabling all CHUD_Fire_Buttons");
                ModState.MeleeAttackContainer.SetActive(true);

                ModState.MeleeStates = MeleeHelper.GetMeleeStates(
                    ModState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    ModState.CombatHUD.SelectionHandler.ActiveState.PreviewPos,
                    ModState.CombatHUD.SelectionHandler.ActiveState.TargetedCombatant
                    );

                if (ModState.ChargeFB != null && ModState.MeleeStates.Charge.IsValid) 
                    ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                if (ModState.KickFB != null && ModState.MeleeStates.Kick.IsValid) 
                    ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                if (ModState.PhysicalWeaponFB != null && ModState.MeleeStates.PhysicalWeapon.IsValid) 
                    ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                if (ModState.PunchFB != null && ModState.MeleeStates.Punch.IsValid) 
                    ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;

                // TODO: Autoselect best option
            }
            else
            {
                Mod.Log.Info($"Disabling all CHUD_Fire_Buttons");
                ModState.MeleeAttackContainer.SetActive(false);
                if (ModState.ChargeFB != null) ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.KickFB != null) ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.PhysicalWeaponFB != null) ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.PunchFB != null) ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;

                // Resetting state so it's not accidently reused
                ModState.MeleeStates = null;
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
                    string localText = new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_FB_CHARGE]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_FB_KICK]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.PhysicalWeaponFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_FB_PHYSICAL_WEAPON]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_FB_PUNCH]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "OnClick")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDFireButton_OnClick
    {
        static bool Prefix(CombatHUDFireButton __instance)
        {
            Mod.Log.Info($"CHUDFB - OnClick FIRED!");
            bool shouldReturn = true;
            CombatHUDAttackModeSelector selector = ModState.CombatHUD.AttackModeSelector;
            if (__instance.gameObject.name == ModConsts.ChargeFB_GO_ID)
            {
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.Charge;
                shouldReturn = false;
            }
            else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
            {
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.Kick;
                shouldReturn = false;
            }
            else if (__instance.gameObject.name == ModConsts.PhysicalWeaponFB_GO_ID)
            {
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.PhysicalWeapon;
                shouldReturn = false;
            }
            else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
            {
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.Punch;
                shouldReturn = false;
            }

            if (ModState.MeleeStates.SelectedState != null)
            {
                Mod.Log.Info("Enabling description container");
                selector.DescriptionContainer.SetActive(true);
                selector.DescriptionContainer.gameObject.SetActive(true);

                HashSet<string> descriptonNotes = ModState.MeleeStates.SelectedState.DescriptionNotes;
                string description = String.Join(", ", descriptonNotes);
                Mod.Log.Info($"Aggregate description is: {description}");

                Mod.Log.Info("Setting text strings for selected state.");
                selector.DescriptionText.SetText(description);
                selector.DescriptionText.ForceMeshUpdate(true);
            }

            return shouldReturn;
        }
    }

}

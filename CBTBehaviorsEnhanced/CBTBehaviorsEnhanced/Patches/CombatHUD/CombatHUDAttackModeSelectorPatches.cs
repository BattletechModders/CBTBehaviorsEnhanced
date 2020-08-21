using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Helper;
using CustomAmmoCategoriesLog;
using Harmony;
using IRBTModUtils;
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
            Mod.Log.Trace?.Write($"Created {goName} and attached to parent.");

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
       
            Mod.Log.Trace?.Write($"Redid layout for {goName}.");
            return newButton;
        }

        static void Postfix(CombatHUDAttackModeSelector __instance, CombatGameState Combat, CombatHUD HUD)
        {
            try
            {
                Mod.Log.Trace?.Write($"CREATING TEST COMPONENTS: instance is null? {__instance == null}  instanceGO is null? {__instance?.gameObject == null}");

                // Find icPanel_Layout as the parent
                Transform icPanelLayoutTransform = __instance.FireButton.gameObject.transform.parent;
                GameObject icPanelLayoutGO = icPanelLayoutTransform.gameObject;
                if (icPanelLayoutGO == null) Mod.Log.Warn?.Write("FAILED TO FIND IC_PANEL_LAYOUT!");

                VerticalLayoutGroup vlg = icPanelLayoutGO.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.spacing = 8f;

                ModState.MeleeAttackContainer = new GameObject();
                ModState.MeleeAttackContainer.transform.parent = icPanelLayoutTransform;
                ModState.MeleeAttackContainer.transform.SetSiblingIndex(1); // Move us above the description container
                ModState.MeleeAttackContainer.layer = 5; // everyting else is at this level
                ModState.MeleeAttackContainer.name = ModConsts.Container_GO_ID;
                if (ModState.MeleeAttackContainer == null) Mod.Log.Warn?.Write("FAILED TO ADD CONTAINER!");

                RectTransform containerRectTransform = ModState.MeleeAttackContainer.AddComponent<RectTransform>();
                containerRectTransform.localScale = Vector3.one;

                HorizontalLayoutGroup hlg = ModState.MeleeAttackContainer.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.Log.Warn?.Write("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childAlignment = TextAnchor.LowerLeft;
                hlg.spacing = 16f;
                hlg.gameObject.SetActive(true);

                LayoutElement le = ModState.MeleeAttackContainer.AddComponent<LayoutElement>();
                if (le == null) Mod.Log.Warn?.Write("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 75f;
                le.preferredWidth = 500f;

                // Reverse order, as the first one created is the right-most
                Mod.Log.Trace?.Write($"CREATING BUTTONS");
                ModState.PunchFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PunchFB_GO_ID, Combat, HUD);
                ModState.PhysicalWeaponFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PhysicalWeaponFB_GO_ID, Combat, HUD);
                ModState.KickFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.KickFB_GO_ID, Combat, HUD);
                ModState.ChargeFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.ChargeFB_GO_ID, Combat, HUD);
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to create melee buttons!");
            }

        }
    }

    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "ShowFireButton")]
    static class CombatHUDAttackModeSelector_ShowFireButton
    {
        public static void Prefix(CombatHUDAttackModeSelector __instance, CombatHUDFireButton.FireMode mode, ref string additionalDetails, bool showHeatWarnings)
        {
            if (SharedState.CombatHUD.SelectionHandler.ActiveState == null)
            {
                Mod.Log.Trace?.Write($"Disabling all CHUD_Fire_Buttons");
                ModState.MeleeAttackContainer.SetActive(false);
                return;
            }

            Mod.Log.Trace?.Write($"ShowFireButton called with mode: {mode}");

            // Intentionally regen the meleeStates everytime the button changes, to make sure different positions calculate properly
            if (mode == CombatHUDFireButton.FireMode.Engage || mode == CombatHUDFireButton.FireMode.DFA)
            {
                if (ModState.MeleeStates == null  || SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos != ModState.MeleePreviewPos)
                {
                    ModState.MeleeStates = MeleeHelper.GetMeleeStates(
                        SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.TargetedCombatant
                        );
                    ModState.MeleePreviewPos = SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos;
                    Mod.Log.Debug?.Write($"Updated melee state for position: {ModState.MeleePreviewPos}");
                }
            }
            else
            {
                ModState.MeleeStates = null;
            }
            
            if (mode == CombatHUDFireButton.FireMode.Engage)
            {
                Mod.Log.Trace?.Write($"Enabling all CHUD_Fire_Buttons");
                ModState.MeleeAttackContainer.SetActive(true);

                if (ModState.ChargeFB != null)
                {
                    if (ModState.MeleeStates.Charge.IsValid)
                        ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                    else
                        ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                }

                if (ModState.KickFB != null)
                {
                    if (ModState.MeleeStates.Kick.IsValid)
                        ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                    else
                        ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                }

                if (ModState.PhysicalWeaponFB != null)
                {
                    if (ModState.MeleeStates.PhysicalWeapon.IsValid)
                        ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                    else
                        ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                }

                if (ModState.PunchFB != null)
                {
                    if (ModState.MeleeStates.Punch.IsValid)
                        ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
                    else
                        ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                }

                // TODO: Autoselect best option
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.GetHighestTargetDamageState();
                if (ModState.MeleeStates.SelectedState != null)
                {
                    Mod.Log.Info?.Write($"Autoselecting state of type: '{ModState.MeleeStates.SelectedState?.Label}' as most damaging.");
                }
                else
                {
                    Mod.Log.Info?.Write("No highest damaging state - no melee options!");
                }

                // Final check - if everything is disabled, disable the button
                if (ModState.MeleeStates.SelectedState == null && 
                    (!ModState.MeleeStates.Charge.IsValid && !ModState.MeleeStates.Kick.IsValid && 
                    !ModState.MeleeStates.PhysicalWeapon.IsValid && !ModState.MeleeStates.Punch.IsValid))
                {
                    Mod.Log.Debug?.Write("NO VALID MELEE ATTACKS, DISABLING!");
                    __instance.FireButton.SetState(ButtonState.Disabled);
                }
            }
            else
            {
                Mod.Log.Trace?.Write($"Disabling all CHUD_Fire_Buttons");
                ModState.MeleeAttackContainer.SetActive(false);
                if (ModState.ChargeFB != null) ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.KickFB != null) ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.PhysicalWeaponFB != null) ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                if (ModState.PunchFB != null) ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
            }

            // Handle the DFA button here
            if (mode == CombatHUDFireButton.FireMode.DFA)
            {
                HashSet<string> descriptonNotes = ModState.MeleeStates.DFA.DescriptionNotes;
                additionalDetails = String.Join(", ", descriptonNotes);
                Mod.Log.Info?.Write($"Aggregate description is: {additionalDetails}");

                // Select state here as a click will validate 
                ModState.MeleeStates.SelectedState = ModState.MeleeStates.DFA;
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "CurrentFireMode", MethodType.Setter)]
    static class CombatHUDFireButton_CurrentFireMode_Setter
    {
        static void Postfix(CombatHUDFireButton __instance, CombatHUDFireButton.FireMode value)
        {

            if (__instance.gameObject != null)
            {
                if (__instance.gameObject.name == ModConsts.ChargeFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Kick]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.PhysicalWeaponFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Physical_Weapon]).ToString();
                    __instance.FireText.SetText(localText, new object[] { });
                }
                else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
                {
                    string localText = new Localize.Text(Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Punch]).ToString();
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

            if (__instance == null || __instance.gameObject == null || ModState.MeleeStates == null) return true;

            Mod.Log.Trace?.Write($"CHUDFB - OnClick FIRED for FireMode: {__instance.CurrentFireMode}!");

            bool shouldReturn = true;
            CombatHUDAttackModeSelector selector = SharedState.CombatHUD.AttackModeSelector;
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
                Mod.Log.Debug?.Write("Enabling description container for melee attack");
                selector.DescriptionContainer.SetActive(true);
                selector.DescriptionContainer.gameObject.SetActive(true);

                HashSet<string> descriptonNotes = ModState.MeleeStates.SelectedState.DescriptionNotes;
                string description = String.Join(", ", descriptonNotes);
                Mod.Log.Debug?.Write($"Aggregate description is: {description}");

                selector.DescriptionText.SetText(description);
                selector.DescriptionText.ForceMeshUpdate(true);

                // TODO: Update weapon damage instead?

                // Update the weapon strings
                SharedState.CombatHUD.WeaponPanel.RefreshDisplayedWeapons();
            }

            return shouldReturn;
        }
    }

}

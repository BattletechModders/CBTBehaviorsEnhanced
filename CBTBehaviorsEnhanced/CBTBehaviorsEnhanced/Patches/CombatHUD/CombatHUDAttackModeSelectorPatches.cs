using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Helper;
using CBTBehaviorsEnhanced.MeleeStates;
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
            Mod.UILog.Trace?.Write($"Created {goName} and attached to parent.");

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
       
            Mod.UILog.Trace?.Write($"Redid layout for {goName}.");
            return newButton;
        }

        static void Postfix(CombatHUDAttackModeSelector __instance, CombatGameState Combat, CombatHUD HUD)
        {
            try
            {
                Mod.UILog.Trace?.Write($"CREATING TEST COMPONENTS: instance is null? {__instance == null}  instanceGO is null? {__instance?.gameObject == null}");

                // Find icPanel_Layout as the parent
                Transform icPanelLayoutTransform = __instance.FireButton.gameObject.transform.parent;
                GameObject icPanelLayoutGO = icPanelLayoutTransform.gameObject;
                if (icPanelLayoutGO == null) Mod.UILog.Warn?.Write("FAILED TO FIND IC_PANEL_LAYOUT!");

                VerticalLayoutGroup vlg = icPanelLayoutGO.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.spacing = 8f;

                ModState.MeleeAttackContainer = new GameObject();
                ModState.MeleeAttackContainer.transform.parent = icPanelLayoutTransform;
                ModState.MeleeAttackContainer.transform.SetSiblingIndex(1); // Move us above the description container
                ModState.MeleeAttackContainer.layer = 5; // everyting else is at this level
                ModState.MeleeAttackContainer.name = ModConsts.Container_GO_ID;
                if (ModState.MeleeAttackContainer == null) Mod.UILog.Warn?.Write("FAILED TO ADD CONTAINER!");

                RectTransform containerRectTransform = ModState.MeleeAttackContainer.AddComponent<RectTransform>();
                containerRectTransform.localScale = Vector3.one;

                HorizontalLayoutGroup hlg = ModState.MeleeAttackContainer.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.UILog.Warn?.Write("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childAlignment = TextAnchor.LowerLeft;
                hlg.spacing = 16f;
                hlg.gameObject.SetActive(true);

                LayoutElement le = ModState.MeleeAttackContainer.AddComponent<LayoutElement>();
                if (le == null) Mod.UILog.Warn?.Write("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 75f;
                le.preferredWidth = 500f;

                // Reverse order, as the first one created is the right-most
                Mod.UILog.Trace?.Write($"CREATING BUTTONS");
                ModState.ChargeFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.ChargeFB_GO_ID, Combat, HUD);
                ModState.PhysicalWeaponFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PhysicalWeaponFB_GO_ID, Combat, HUD);
                ModState.PunchFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PunchFB_GO_ID, Combat, HUD);
                ModState.KickFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.KickFB_GO_ID, Combat, HUD);
            }
            catch (Exception e)
            {
                Mod.UILog.Error?.Write(e, $"Failed to create melee buttons!");
            }

        }
    }

    [HarmonyPatch(typeof(CombatHUDAttackModeSelector), "ShowFireButton")]
    static class CombatHUDAttackModeSelector_ShowFireButton
    {
        static void Prefix(CombatHUDAttackModeSelector __instance, CombatHUDFireButton.FireMode mode, 
            ref string additionalDetails, bool showHeatWarnings)
        {
            Mod.UILog.Trace?.Write($"ShowFireButton called with mode: {mode}");

            // Intentionally regen the meleeStates everytime the button changes, to make sure different positions calculate properly
            if (mode == CombatHUDFireButton.FireMode.Engage || mode == CombatHUDFireButton.FireMode.DFA)
            {
                if (SharedState.CombatHUD?.SelectionHandler?.ActiveState?.PreviewPos != ModState.MeleePreviewPos)
                {
                    ModState.MeleePreviewPos = SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos;

                    // Update melee states
                    ModState.AddorUpdateMeleeState(SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.TargetedCombatant);
                    Mod.UILog.Debug?.Write($"Updated melee state for position: {ModState.MeleePreviewPos}");

                    // Re-enable any buttons if they were disabled.
                    __instance.FireButton.SetState(ButtonState.Enabled);
                    __instance.DescriptionContainer.SetActive(true);

                }
            }
            else
            {
                ModState.InvalidateState(SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor);
            }
        }

        static void Postfix(CombatHUDAttackModeSelector __instance, CombatHUDFireButton.FireMode mode, 
            ref string additionalDetails, bool showHeatWarnings)
        {
            try
            {
                // Disable the melee container if there's no active state
                if (SharedState.CombatHUD?.SelectionHandler?.ActiveState == null ||
                    SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor == null ||
                    SharedState.CombatHUD?.SelectionHandler?.ActiveState?.PreviewPos == null)
                {
                    Mod.UILog.Trace?.Write($"Disabling all CHUD_Fire_Buttons");
                    ModState.MeleeAttackContainer.SetActive(false);
                    return;
                }

                Mod.UILog.Trace?.Write($"ShowFireButton called with mode: {mode}");

                if (mode == CombatHUDFireButton.FireMode.Engage)
                {
                    Mod.UILog.Trace?.Write($"Enabling all CHUD_Fire_Buttons");
                    ModState.MeleeAttackContainer.SetActive(true);

                    MeleeState meleeState = ModState.GetMeleeState(
                        SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);

                    // Toggle each button by available state
                    ToggleStateButtons(meleeState);

                    // Autoselect best option
                    MeleeAttack autoselectedAttack = meleeState.GetHighestDamageAttackForUI();
                    if (autoselectedAttack != null)
                        Mod.UILog.Info?.Write($"Autoselecting state of type: '{autoselectedAttack.Label}' as most damaging.");
                    else
                        Mod.UILog.Info?.Write("No highest damaging state - no melee options!");

                    // Final check - if everything is disabled, disable the button
                    bool hasValidAttack = meleeState.Charge.IsValid || meleeState.Kick.IsValid ||
                        meleeState.PhysicalWeapon.IsValid || meleeState.Punch.IsValid;
                    if (!hasValidAttack)
                    {
                        Mod.UILog.Info?.Write("NO VALID MELEE ATTACKS, DISABLING!");
                        __instance.FireButton.SetState(ButtonState.Disabled);
                        __instance.FireButton.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                        __instance.DescriptionContainer.SetActive(false);
                        SharedState.CombatHUD.SelectionHandler.ActiveState.BackOut();
                        __instance.ForceRefreshImmediate();
                    }
                    else
                    {
                        Mod.UILog.Info?.Write($" CHECKING FOR VALID ATTACKS: hasValidAttack=>{hasValidAttack}" +
                            $"  charge=>{meleeState.Charge.IsValid}" +
                            $"  kick=>{meleeState.Kick.IsValid}" +
                            $"  punch=>{meleeState.Punch.IsValid}" +
                            $"  weapon=>{meleeState.PhysicalWeapon.IsValid}" +
                            $"");
                    }
                }
                else
                {
                    Mod.UILog.Trace?.Write($"Disabling all CHUD_Fire_Buttons");
                    ModState.MeleeAttackContainer.SetActive(false);
                    if (ModState.ChargeFB != null) ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (ModState.KickFB != null) ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (ModState.PhysicalWeaponFB != null) ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (ModState.PunchFB != null) ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;

                    ModState.InvalidateState(SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor);
                }

                // Handle the DFA button here
                if (mode == CombatHUDFireButton.FireMode.DFA)
                {

                    MeleeState meleeState = ModState.GetMeleeState(
                        SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                        SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);

                    // Check for valid attack
                    if (!meleeState.DFA.IsValid)
                    {
                        Mod.UILog.Info?.Write($"DFA attack failed validation, disabling button.");
                        __instance.FireButton.SetState(ButtonState.Disabled);
                        __instance.FireButton.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                        __instance.DescriptionContainer.SetActive(false);
                        SharedState.CombatHUD.SelectionHandler.ActiveState.BackOut();
                        __instance.ForceRefreshImmediate();
                    }

                    HashSet<string> descriptonNotes = meleeState.DFA.DescriptionNotes;
                    additionalDetails = String.Join(", ", descriptonNotes);
                    Mod.UILog.Info?.Write($"Aggregate description is: {additionalDetails}");

                    // Select state here as a click will validate 
                    ModState.AddOrUpdateSelectedAttack(
                        SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                        meleeState.DFA
                        );
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Failed to update the CombatButton states - warn Frost!");
            }
            
        }

        private static void ToggleStateButtons(MeleeState meleeState)
        {
            if (ModState.ChargeFB != null)
            {
                if (meleeState == null || meleeState.Charge == null || !meleeState.Charge.IsValid)
                    ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                else
                    ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
            }

            if (ModState.KickFB != null)
            {
                if (meleeState == null || meleeState.Kick == null || !meleeState.Kick.IsValid)
                    ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                else
                    ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
            }

            if (ModState.PhysicalWeaponFB != null)
            {
                if (meleeState == null || meleeState.PhysicalWeapon == null || !meleeState.PhysicalWeapon.IsValid)
                    ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                else
                    ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
            }

            if (ModState.PunchFB != null)
            {
                if (meleeState == null || meleeState.Punch == null || !meleeState.Punch.IsValid)
                    ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                else
                    ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.Engage;
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

            if (__instance == null || __instance.gameObject == null) return true;
            
            Mod.UILog.Info?.Write($"CHUDFB - OnClick FIRED for FireMode: {__instance.CurrentFireMode}");

            bool shouldReturn = true;
            CombatHUDAttackModeSelector selector = SharedState.CombatHUD.AttackModeSelector;
            if (__instance.gameObject.name == ModConsts.ChargeFB_GO_ID)
            {
                MeleeState meleeState = ModState.GetMeleeState(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);
                ModState.AddOrUpdateSelectedAttack(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    meleeState.Charge
                    );
                Mod.UILog.Info?.Write("User selected Charge button");
                shouldReturn = false;                
            }
            else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
            {
                MeleeState meleeState = ModState.GetMeleeState(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);
                ModState.AddOrUpdateSelectedAttack(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    meleeState.Kick
                    );
                Mod.UILog.Info?.Write("User selected Kick button");
                shouldReturn = false;
            }
            else if (__instance.gameObject.name == ModConsts.PhysicalWeaponFB_GO_ID)
            {
                MeleeState meleeState = ModState.GetMeleeState(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);
                ModState.AddOrUpdateSelectedAttack(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    meleeState.PhysicalWeapon
                    );
                Mod.UILog.Info?.Write("User selected PhysWeap button");
                shouldReturn = false;
            }
            else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
            {
                MeleeState meleeState = ModState.GetMeleeState(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    SharedState.CombatHUD.SelectionHandler.ActiveState.PreviewPos);
                ModState.AddOrUpdateSelectedAttack(
                    SharedState.CombatHUD.SelectionHandler.ActiveState.SelectedActor,
                    meleeState.Punch
                    );
                Mod.UILog.Info?.Write("User selected Punch button");
                shouldReturn = false;
            }
            else 
            {

                MeleeAttack selectedAttack2 = ModState.GetSelectedAttack(SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor);
                if (selectedAttack2 != null)
                {
                    Mod.UILog.Info?.Write("OnClick from generic CHUDFB with selected type, short-cutting to action.");

                    // Disable the buttons to prevent accidental clicks?
                    if (selectedAttack2 is ChargeAttack)
                        ModState.ChargeFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (selectedAttack2 is KickAttack)
                        ModState.KickFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (selectedAttack2 is WeaponAttack)
                        ModState.PhysicalWeaponFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;
                    if (selectedAttack2 is PunchAttack)
                        ModState.PunchFB.CurrentFireMode = CombatHUDFireButton.FireMode.None;

                    return true;
                }
                
            }

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor);
            if (selectedAttack != null)
            {
                Mod.UILog.Debug?.Write("Enabling description container for melee attack");
                selector.DescriptionContainer.SetActive(true);
                selector.DescriptionContainer.gameObject.SetActive(true);

                HashSet<string> descriptonNotes = selectedAttack.DescriptionNotes;
                string description = String.Join(", ", descriptonNotes);
                Mod.UILog.Debug?.Write($"Aggregate description is: {description}");

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

using BattleTech;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
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
            CombatHUDFireButton fireButton = GameObject.Instantiate<CombatHUDFireButton>(cloneSource);
            fireButton.Init(Combat, HUD);
            fireButton.gameObject.transform.parent = parent.transform;
            Mod.Log.Info($"Created {goName} and attached to parent.");

            fireButton.gameObject.name = goName;
            fireButton.gameObject.transform.SetAsFirstSibling();
            fireButton.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);

            RectTransform rectTransform = fireButton.gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(240f, 70f); // Default is 160, 80

            Image image = fireButton.gameObject.GetComponent<Image>();
            image.color = new Color(255f, 255f, 255f, 204f);

            LayoutElement layoutElement = fireButton.gameObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = 240f;
            layoutElement.preferredWidth = 240f;
            layoutElement.minHeight = 70f;
            layoutElement.preferredHeight = 70f;
            layoutElement.ignoreLayout = false;

            GameObject punchOverheatWarn = fireButton.gameObject.transform.Find("overheatWarn").gameObject;
            punchOverheatWarn.SetActive(false);
            GameObject punchShutdownWarn = fireButton.gameObject.transform.Find("shutdownWarn").gameObject;
            punchShutdownWarn.SetActive(false);
            GameObject sideWedges = fireButton.gameObject.transform.Find("confirmFrame_sideWedges (1)").gameObject;
            sideWedges.SetActive(false);

            //LocalizableText buttonText = fireButton.gameObject.GetComponentInChildren<LocalizableText>();
            //buttonText.SetText(label);
            //buttonText.color = Color.black;

            //punchFB.gameObject.transform.parent = icPanelLayoutGO.transform;            
            Mod.Log.Info($"Redid layout for {goName}.");
            return fireButton;
        }

        static void Postfix(CombatHUDAttackModeSelector __instance, CombatGameState Combat, CombatHUD HUD)
        {
            try
            {
                Mod.Log.Info($"CREATING TEST COMPONENTS: instance is null? {__instance == null}  instanceGO is null? {__instance?.gameObject == null}");

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
                container.name = "cbtbe_melee_container";
                if (container == null) Mod.Log.Warn("FAILED TO ADD CONTAINER!");

                HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.Log.Warn("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childAlignment = TextAnchor.LowerLeft;
                hlg.spacing = 16f;

                LayoutElement le = container.AddComponent<LayoutElement>();
                if (le == null) Mod.Log.Warn("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 75f;
                le.preferredWidth = 500f;

                Mod.Log.Info($"CREATING PUNCH BUTTON");
                ModState.PunchFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.PunchFB_GO_ID, "PUNCH", Combat, HUD);
                ModState.KickFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.KickFB_GO_ID, "KICK", Combat, HUD);
                ModState.ChargeFB = CloneCHUDFireButton(hlg.gameObject, __instance.FireButton, ModConsts.ChargeFB_GO_ID, "CHARGE", Combat, HUD);

                hlg.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Mod.Log.Error($"Failed to create melee buttons!", e);
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
                    Mod.Log.Info($"UPDATING TYPE: PUNCH");
                    __instance.FireText.SetText("PUNCH", new object[] { });
                    __instance.SetState(ButtonState.Enabled, true);
                    Traverse highlightT = Traverse.Create(__instance).Method("Highlighted_OnEnter");
                    highlightT.GetValue();
                }
                else if (__instance.gameObject.name == ModConsts.KickFB_GO_ID)
                {
                    Mod.Log.Info($"UPDATING TYPE: KICK");
                    __instance.FireText.SetText("KICK", new object[] { });
                    __instance.SetState(ButtonState.Disabled, true);
                }
                else if (__instance.gameObject.name == ModConsts.PunchFB_GO_ID)
                {
                    Mod.Log.Info($"UPDATING TYPE: CHARGE");
                    __instance.FireText.SetText("CHARGE", new object[] { });
                    __instance.SetState(ButtonState.Enabled, true);
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
            //Mod.Log.Info($"CHUDFB - Update FIRED FOR: {__instance.gameObject.name}");
        }
    }


    [HarmonyPatch(typeof(CombatHUDFireButton), "Highlighted_OnEnter")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDFireButton_Highlighted_OnEnter
    {
        static void Prefix(CombatHUDFireButton __instance)
        {
            Mod.Log.Info($"CHUDFB - Highlighted-Entered FIRED FOR: {__instance.gameObject.name}");
        }
    }

    [HarmonyPatch(typeof(CombatHUDFireButton), "Highlighted_OnExit")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDFireButton_Highlighted_OnExit
    {
        static void Prefix(CombatHUDFireButton __instance)
        {
            Mod.Log.Info($"CHUDFB - Highlighted-Exit FIRED FOR: {__instance.gameObject.name}");
        }
    }
}

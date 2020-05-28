using BattleTech;
using BattleTech.UI;
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
        static void Postfix(CombatHUDAttackModeSelector __instance, CombatGameState Combat, CombatHUD HUD)
        {
            try
            {
                Mod.Log.Info($"CREATING TEST COMPONENTS: instance is null? {__instance == null}  instanceGO is null? {__instance?.gameObject == null}");

                // Find icPanel_Layout as the parent

                Transform icPanelLayoutTransform = __instance.FireButton.gameObject.transform.parent;
                GameObject icPanelLayoutGO = icPanelLayoutTransform.gameObject;
                if (icPanelLayoutGO == null) Mod.Log.Warn("FAILED TO FIND IC_PANEL_LAYOUT!");

                GameObject container = new GameObject();
                container.transform.parent = icPanelLayoutTransform;
                container.transform.SetSiblingIndex(1); // Move us above the description container
                container.name = "cbtbe_melee_container";
                if (container == null) Mod.Log.Warn("FAILED TO ADD CONTAINER!");

                HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
                if (hlg == null) Mod.Log.Warn("FAILED TO CREATE HORIZONTAL GROUP");
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = false;
                hlg.childControlWidth = true;
                hlg.childForceExpandWidth = true;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8f;

                LayoutElement le = container.AddComponent<LayoutElement>();
                if (le == null) Mod.Log.Warn("FAILED TO ADD LAYOUT ELEMENT");
                le.preferredHeight = 80f;
                le.preferredWidth = 600f;

                Mod.Log.Info($"CREATING PUNCH BUTTON");
                //CombatHUDFireButton punchFB = new CombatHUDFireButton();
                CombatHUDFireButton punchFB = GameObject.Instantiate<CombatHUDFireButton>(__instance.FireButton);
                Mod.Log.Info($"ADDING PUNCHGO to HLG");
                punchFB.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                //punchFB.gameObject.transform.localPosition = new Vector3(0f, 30f, 0f);
                punchFB.gameObject.name = "cbtbe_punch_button";
                punchFB.gameObject.transform.SetAsFirstSibling();
                LayoutElement punchLE = punchFB.gameObject.GetComponent<LayoutElement>();
                punchLE.minWidth = 160f;
                punchLE.preferredWidth = 160f;
                punchLE.ignoreLayout = false;
                GameObject punchOverheatWarn = punchFB.gameObject.transform.Find("overheatWarn").gameObject;
                punchOverheatWarn.SetActive(false);
                GameObject punchShutdownWarn = punchFB.gameObject.transform.Find("shutdownWarn").gameObject;
                punchShutdownWarn.SetActive(false);
                GameObject sideWedges = punchFB.gameObject.transform.Find("confirmFrame_sideWedges (1)").gameObject;
                sideWedges.SetActive(false);
                //punchFB.gameObject.transform.parent = icPanelLayoutGO.transform;
                Mod.Log.Info($"INITING PUNCHGO");
                punchFB.Init(Combat, HUD);
                punchFB.gameObject.transform.parent = hlg.transform;

                Mod.Log.Info($"CREATING KICK BUTTON");
                //CombatHUDFireButton punchFB = new CombatHUDFireButton();
                CombatHUDFireButton kickFB = GameObject.Instantiate<CombatHUDFireButton>(__instance.FireButton);
                Mod.Log.Info($"ADDING KICKGO to HLG");
                kickFB.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                //kickGO.gameObject.transform.localPosition = new Vector3(0f, 30f, 0f);
                kickFB.gameObject.name = "cbtbe_kick_button";
                LayoutElement kickLE = kickFB.gameObject.GetComponent<LayoutElement>();
                kickLE.minWidth = 160f;
                kickLE.preferredWidth = 160f;
                kickLE.ignoreLayout = false;
                GameObject kickOverheatWarn = kickFB.gameObject.transform.Find("overheatWarn").gameObject;
                kickOverheatWarn.SetActive(false);
                GameObject kickShutdownWarn = kickFB.gameObject.transform.Find("shutdownWarn").gameObject;
                kickShutdownWarn.SetActive(false);
                sideWedges = kickFB.gameObject.transform.Find("confirmFrame_sideWedges (1)").gameObject;
                sideWedges.SetActive(false);
                //kickGO.gameObject.transform.parent = icPanelLayoutGO.transform;
                Mod.Log.Info($"INITING KICKGO");
                kickFB.Init(Combat, HUD);
                kickFB.gameObject.transform.parent = hlg.transform;

                //GameObject chargeGO = GameObject.Instantiate(__instance.FireButton.gameObject);
                //if (chargeGO == null) Mod.Log.Warn("FAILED TO CREATE CHARGE GO");
                //chargeGO.name = "cbtbe_charge_button";
                //chargeGO.transform.parent = hlg.transform;

                hlg.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Mod.Log.Error($"Failed to create melee buttons!", e);
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
            Mod.Log.Info($"CHUDFB - Update FIRED FOR: {__instance.gameObject.name}");
        }
    }
}

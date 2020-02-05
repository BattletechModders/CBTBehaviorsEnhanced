using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Heat;
using Harmony;
using HBS;
using Localize;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    //[HarmonyPatch(typeof(CombatHUDHeatMeter), "")]
    //public static class CombatHUDHeatMeter_Foo {
    //}

    [HarmonyPatch(typeof(CombatHUDHeatDisplay), "Init")]
    [HarmonyPatch(new Type[] { typeof(float) })]
    public static class CombatHUDHeatDisplay_Init {

        // Resize the heat bar to its original value
        public static void Postfix(CombatHUDHeatDisplay __instance, float dangerLevel) {
            if (__instance.DisplayedActor != null && __instance.DisplayedActor is Mech) {
                Mech displayedMech = __instance.DisplayedActor as Mech;

                Traverse origWidthT = Traverse.Create(__instance).Property("origWidth");
                float origWidth = origWidthT.GetValue<float>();

                Traverse rectTransformT = Traverse.Create(__instance).Property("rectTransform");
                RectTransform rectTransform = rectTransformT.GetValue<RectTransform>();
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origWidth);
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

    // TODO: FIXME
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

            if (mech.IsShutDown) {
                Mod.Log.Debug($"Mech:{CombatantUtils.Label(mech)} is shutdown.");
                methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                        new Text("SHUT DOWN", new object[0]), new Text("This target is easier to hit, and Called Shots can be made against this target.", new object[0]),
                        __instance.defaultIconScale, false });
            } else if (mech.IsOverheated) {
                float shutdownChance = 0;
                float ammoExplosionChance = 0;
                // FIXME: Remove this old code
                Mod.Log.Debug($"Mech:{CombatantUtils.Label(mech)} is overheated, shutdownChance:{shutdownChance}% ammoExplosionChance:{ammoExplosionChance}%");

                string descr = string.Format("This unit may trigger a Shutdown at the end of the turn unless heat falls below critical levels." +
                    "\nShutdown Chance: {0:P2}\nAmmo Explosion Chance: {1:P2}",
                    shutdownChance, ammoExplosionChance);
                methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                        new Text("OVERHEATING", new object[0]), new Text(descr, new object[0]), __instance.defaultIconScale, false });
            }
        }
    }
}

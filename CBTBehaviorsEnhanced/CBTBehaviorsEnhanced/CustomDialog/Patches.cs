using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced.CustomDialog {

    // Register listeners for our events
    [HarmonyPatch(typeof(CombatHUD), "SubscribeToMessages")]
    public static class CombatHUD_SubscribeToMessages {
        public static void Postfix(CombatHUD __instance, bool shouldAdd) {
            if (__instance != null) {
                __instance.Combat.MessageCenter.Subscribe(
                    (MessageCenterMessageType)MessageTypes.OnCustomDialog, new ReceiveMessageCenterMessage(Coordinator.OnCustomDialogMessage), shouldAdd);
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "Init")]
    [HarmonyPatch(new Type[] {  typeof(CombatGameState) })]
    public static class CombatHUD_Init {
        public static void Postfix(CombatHUD __instance, CombatGameState Combat) {
            Coordinator.OnCombatHUDInit(Combat, __instance);
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "OnCombatGameDestroyed")]
    public static class CombatHUD_OnCombatGameDestroyed {
        public static void Prefix() {
            Coordinator.OnCombatGameDestroyed();
        }
    }

    //// TODO: Testing patch - remove
    [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int) })]
    public static class Mech_OnActivationEnd_TEST {
        public static void Postfix(Mech __instance) {
            Mod.Log.Info("TESTING DIALOG");
            // TODO: Fix castdef for player units?
            CastDef castDef = Coordinator.CreateCast(__instance);
            // Note: Supplying __instance.GUID breaks...
            DialogueContent content = new DialogueContent("Turns done!", Color.red, castDef.id, null, null, DialogCameraDistance.Medium, DialogCameraHeight.Default, 0f);
            content.ContractInitialize(__instance.Combat);

            Mod.Log.Info("  --PUBLISHING MESSAGE");
            __instance.Combat.MessageCenter.PublishMessage(
                new CustomDialogMessage(__instance, content)
                );
            Mod.Log.Info("  --PUBLISH DONE");
        }
    }
}

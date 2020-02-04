using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(CombatGameState __instance) {
            Mod.Log.Trace("CGS:OCGD - entered.");

            // Reset any combat state
            ModState.BreachCheck = 0f;
            ModState.BreachAttackId = 0;
            ModState.BreachHitsMech.Clear();
            ModState.BreachHitsTurret.Clear();
            ModState.BreachHitsVehicle.Clear();
        }
    }
}

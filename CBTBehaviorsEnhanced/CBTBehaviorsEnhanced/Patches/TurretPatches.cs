using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CustAmmoCategories;
using HarmonyLib;
using IRBTModUtils;
using IRBTModUtils.Extension;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches
{

    // Override max engage distance to be sprinting
    [HarmonyPatch(typeof(Turret), "DamageLocation")]
    [HarmonyPriority(Priority.Last)]
    public static class Turret_DamageLocation
    {
        public static void Prefix(Turret __instance)
        {
            // Invalidate any held state on damage
            ModState.InvalidateState(__instance);
        }
    }

}

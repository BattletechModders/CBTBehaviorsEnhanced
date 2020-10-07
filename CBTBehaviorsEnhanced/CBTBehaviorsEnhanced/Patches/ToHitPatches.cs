using BattleTech;
using Harmony;
using Localize;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(ToHit), "GetAllMeleeModifiers")]
    static class ToHit_GetAllMeleeModifiers
    {
        static void Postfix(ToHit __instance, ref float __result, Mech attacker, ICombatant target, Vector3 targetPosition, MeleeAttackType meleeAttackType)
        {
            Mod.Log.Trace?.Write("TH:GAMM entered");

            if (__instance == null || ModState.MeleeStates?.SelectedState == null) return;

            Mod.Log.Debug?.Write("Adding CBTBE modifiers to ToHit");
            int sumMod = 0;
            foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
            {
                string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                Mod.Log.Debug?.Write($" - Found attack modifier: {localText} = {kvp.Value}, adding to sum modifier");
                sumMod += kvp.Value;
            }

            __result += (float)sumMod;

        }
    }

    // Always return a zero for DFA modifiers; we'll handle it with our custom modifiers
    [HarmonyPatch(typeof(ToHit), "GetDFAModifier")]
    static class ToHit_GetDFAModifier
    {
        static void Postfix(ToHit __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("TH:GDFAM entered");

            __result = 0;

        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
    static class ToHit_GetAllModifiersDescription
    {
        static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace?.Write("TH:GAMD entered");

            // Check melee patches
            if (ModState.MeleeStates?.SelectedState != null && weapon.Type == WeaponType.Melee)
            {

                foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                {
                    string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                    Mod.Log.Info?.Write($" - Found attack modifier for desc: {localText} = {kvp.Value}");

                    __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, kvp.Value);
                }

            }
        }
    }

    // TODO: Memoize this; its invoked multiple times
    // Apply an attack modifier for shooting when overheated
    [HarmonyPatch(typeof(ToHit), "GetHeatModifier")]
    public static class ToHit_GetHeatModifier
    {
        public static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker)
        {
            Mod.Log.Trace?.Write("TH:GHM entered.");

            // Set the modifier to 0 regardless of input, and allow real work to happen in CACDelegates.HeatToHitModifier
            __result = 0; 
        }
    }

}

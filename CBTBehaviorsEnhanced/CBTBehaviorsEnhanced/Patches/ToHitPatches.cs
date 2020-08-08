using BattleTech;
using BattleTech.UI;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
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
            Mod.Log.Trace("TH:GAMM entered");

            if (__instance == null || ModState.MeleeStates?.SelectedState == null) return;

            Mod.Log.Debug("Adding CBTBE modifiers to ToHit");
            int sumMod = 0;
            foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
            {
                string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                Mod.Log.Debug($" - Found attack modifier: {localText} = {kvp.Value}, adding to sum modifier");
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
            Mod.Log.Trace("TH:GDFAM entered");

            __result = 0;

        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
    static class ToHit_GetAllModifiers
    {
        static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAM entered");

            if (__instance == null || weapon == null) return;

            if (
                (attacker.HasMovedThisRound && attacker.JumpedLastRound) ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump)
                )
            {
                __result += (float)Mod.Config.ToHitSelfJumped;
            }
        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
    static class ToHit_GetAllModifiersDescription
    {
        static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAMD entered");

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
            {
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, Mod.Config.ToHitSelfJumped);
            }

            // Check melee patches
            if (ModState.MeleeStates?.SelectedState != null && weapon.Type == WeaponType.Melee)
            {

                foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                {
                    string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                    Mod.Log.Info($" - Found attack modifier for desc: {localText} = {kvp.Value}");

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
            Mod.Log.Trace("TH:GHM entered.");
            if (attacker is Mech mech && mech.IsOverheated)
            {

                float penalty = 0f;
                foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing)
                {
                    if (mech.CurrentHeat >= kvp.Key)
                    {
                        penalty = kvp.Value;
                    }
                }

                Mod.Log.Trace($"  AttackPenalty: {penalty:+0;-#} from heat: {mech.CurrentHeat} for actor: {attacker.DistinctId()}");
                __result = penalty;
            }
        }
    }

}

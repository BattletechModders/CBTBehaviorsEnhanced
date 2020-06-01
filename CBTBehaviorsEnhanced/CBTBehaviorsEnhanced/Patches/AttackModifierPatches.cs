using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
    public static class ToHit_GetAllModifiers
    {
        private static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAM entered");

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound &&
                // Special trigger for dz's abilities
                !(ModConfig.dZ_Abilities && attacker.SkillTactics != 10))
            {
                __result = __result + (float)Mod.Config.ToHitSelfJumped;
            }
        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
    public static class ToHit_GetAllModifiersDescription
    {
        private static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAMD entered");

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound)
            {
                __result = string.Format("{0}JUMPED {1:+#;-#}; ", __result, Mod.Config.ToHitSelfJumped);
            }
        }
    }

    // Update the hover text in the case of a modifier
    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
    public static class CombatHUDWeaponSlot_SetHitChance
    {

        private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) return;

            Mod.Log.Trace("CHUDWS:SHC entered");

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound)
            {
                Traverse addToolTipDetailT = Traverse.Create(__instance)
                    .Method("AddToolTipDetail", "JUMPED SELF", Mod.Config.ToHitSelfJumped);
                Mod.Log.Trace($"Invoking addToolTipDetail for: JUMPED SELF = {Mod.Config.ToHitSelfJumped}");
                addToolTipDetailT.GetValue();
            }
        }
    }
}

using BattleTech;
using Harmony;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.Melee
{
    [HarmonyPatch(typeof(MeleeRules), "SelectRandomMeleeAttack")]
    static class MeleeRules_SelectRandomMeleeAttack
    {
        static void Postfix(MeleeRules __instance, Mech attacker, Vector3 attackPosition, ICombatant target, float rnd, ref MeleeAttackType __result)
        {
            if (ModState.MeleeStates?.SelectedState != null)
            {
                Mod.Log.Info($"FORCING RANDOM MELEE ATTACK TO: {ModState.MeleeStates.SelectedState.AttackAnimation}");
                __result = ModState.MeleeStates.SelectedState.AttackAnimation;
            }
        }
    }

}

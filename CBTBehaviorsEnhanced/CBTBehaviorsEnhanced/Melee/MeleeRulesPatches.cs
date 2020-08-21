using BattleTech;
using Harmony;
using IRBTModUtils.Extension;

namespace CBTBehaviorsEnhanced.Patches.Melee
{
    [HarmonyPatch(typeof(MeleeRules), "SelectRandomMeleeAttack")]
    static class MeleeRules_SelectRandomMeleeAttack
    {
        static void Postfix(Mech attacker, ICombatant target, ref MeleeAttackType __result)
        {
            if (ModState.MeleeStates?.SelectedState != null)
            {
                Mod.Log.Info?.Write($"Forcing melee animation to {ModState.MeleeStates.SelectedState.AttackAnimation} for " +
                    $"attacker: {attacker.DistinctId()} vs. target: {target.DistinctId()}");
                __result = ModState.MeleeStates.SelectedState.AttackAnimation;
            }
        }
    }

}

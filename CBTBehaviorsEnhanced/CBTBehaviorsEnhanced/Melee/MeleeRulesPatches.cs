using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
using IRBTModUtils.Extension;

namespace CBTBehaviorsEnhanced.Patches.Melee
{
    [HarmonyPatch(typeof(MeleeRules), "SelectRandomMeleeAttack")]
    static class MeleeRules_SelectRandomMeleeAttack
    {
        static void Postfix(Mech attacker, ICombatant target, ref MeleeAttackType __result)
        {
            MeleeAttack selectedAttack = ModState.GetSelectedAttack(attacker);
            if (selectedAttack != null)
            {
                Mod.Log.Info?.Write($"Forcing melee animation to {selectedAttack.AttackAnimation} for " +
                    $"attacker: {attacker.DistinctId()} vs. target: {target.DistinctId()}");
                __result = selectedAttack.AttackAnimation;
            }
        }
    }

}

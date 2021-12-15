using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(Weapon), "WillFire", MethodType.Getter)]
    static class Weapon_WillFire_Getter
    {
        // Prevent players from enabling a weapon if the weapon is prevented by the active melee state
        static void Postfix(Weapon __instance, ref bool __result)
        {
            if (__instance == null || !Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack) return; // nothing to do
            if (__result == false || !__instance.IsEnabled) return; // nothing to do

            if (__instance.WeaponSubType == WeaponSubType.Melee || __instance.WeaponSubType == WeaponSubType.DFA)
                return; // nothing to do

            // Check for an active attack sequence and attack
            if (SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor != null)
            {
                MeleeAttack selectedAttack = ModState.GetSelectedAttack(SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor);
                if (selectedAttack != null)
                {
                    if (!selectedAttack.IsRangedWeaponAllowed(__instance))
                    {
                        Mod.UILog.Trace?.Write($"Weapon: {__instance.UIName} can NOT be used with melee state: {selectedAttack?.Label} on " +
                            $"actor: {SharedState.CombatHUD?.SelectionHandler?.ActiveState?.SelectedActor.DistinctId()}");
                        __result = false;
                    }
                    else
                    {
                        Mod.UILog.Trace?.Write($"  -- weapon: {__instance.UIName} allowed by melee state");
                    }
                }

            }

        }
    }
}

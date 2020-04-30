using BattleTech;
using BattleTech.UI;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(CombatDebugHUD), "DEBUG_OverheatTarget")]
    static class CombatDebugHUDPatches
    {
        static void Postfix(CombatHUD ___combatHUD)
        {
            if (___combatHUD.SelectedTarget != null && ___combatHUD.SelectedTarget is Mech targetMech)
            {
                Mod.Log.Debug($"Adding 100 heat to targetMech: {CombatantUtils.Label(targetMech)} ");
                targetMech.AddExternalHeat("CBTBE_DEBUG", 100);
            }
            else if (___combatHUD.SelectedActor != null && ___combatHUD.SelectedActor is Mech actorMech)
            {
                Mod.Log.Debug($"Adding 100 heat to actorMech: {CombatantUtils.Label(actorMech)} ");
                actorMech.AddExternalHeat("CBTBE_DEBUG", 100);
            }
        }
    }
}

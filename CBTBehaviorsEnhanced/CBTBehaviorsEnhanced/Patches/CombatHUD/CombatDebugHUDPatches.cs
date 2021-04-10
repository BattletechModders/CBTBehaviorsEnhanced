using BattleTech;
using BattleTech.UI;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches
{
    // Simple patch that adds 100 heat when the 'overheat' debug command is used.
    [HarmonyPatch(typeof(CombatDebugHUD), "DEBUG_OverheatTarget")]
    static class CombatDebugHUDPatches
    {
        static void Postfix(CombatHUD ___combatHUD)
        {
            if (___combatHUD.SelectedTarget != null && ___combatHUD.SelectedTarget is Mech targetMech)
            {
                Mod.UILog.Info?.Write($"Adding {Mod.Config.Heat.MaxHeat} external heat to targetMech: {CombatantUtils.Label(targetMech)} ");
                targetMech.AddExternalHeat("CBTBE_DEBUG", Mod.Config.Developer.DebugHeatToAdd);
            }
            else if (___combatHUD.SelectedActor != null && ___combatHUD.SelectedActor is Mech actorMech)
            {
                Mod.UILog.Info?.Write($"Adding {Mod.Config.Heat.MaxHeat} external heat to actorMech: {CombatantUtils.Label(actorMech)} ");
                actorMech.AddExternalHeat("CBTBE_DEBUG", Mod.Config.Developer.DebugHeatToAdd);
            }
        }
    }
}

using BattleTech;

namespace CBTBehaviors {

    public static class CombatantHelper {
        public static string LogLabel(ICombatant combatant) {

            string truncatedGUID = combatant.GUID != null ? string.Format("{0:X}", combatant.GUID.GetHashCode()) : "0xDEADBEEF";
            string label = "";
            if (combatant is AbstractActor actor) {
                label = $"{actor.DisplayName}_{actor?.GetPilot()?.Name}_{truncatedGUID}";
            } else {
                label = $"{combatant.DisplayName}";
            }
            return label;
        }

    }
}

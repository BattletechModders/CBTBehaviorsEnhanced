
using BattleTech;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    public static class ModState {

        public static CombatGameState Combat = null;

        public static float BreachCheck = 0f;
        public static int BreachAttackId = 0;

        public static Dictionary<ChassisLocations, int> BreachHitsMech = new Dictionary<ChassisLocations, int>();
        public static Dictionary<BuildingLocation, int> BreachHitsTurret = new Dictionary<BuildingLocation, int>();
        public static Dictionary<VehicleChassisLocations, int> BreachHitsVehicle = new Dictionary<VehicleChassisLocations, int>();

        public static void Reset() {
            // Reinitialize state
            Combat = null;

            BreachCheck = 0f;
            BreachAttackId = 0;
            BreachHitsMech.Clear();
            BreachHitsTurret.Clear();
            BreachHitsVehicle.Clear();
        }
    }

}



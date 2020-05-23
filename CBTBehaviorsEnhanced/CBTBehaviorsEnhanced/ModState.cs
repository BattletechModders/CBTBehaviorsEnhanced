
using BattleTech;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    public static class ModState {

        public const int NO_ATTACK_SEQUENCE_ID = -1; // Attack sequences are always 0 or positive integers (see AttackDirector:44) Use -1 to signify 'no sequence'

        public static CombatGameState Combat = null;

        public static float BreachCheck = 0f;
        public static int BreachAttackId = NO_ATTACK_SEQUENCE_ID; 

        public static Dictionary<ChassisLocations, int> BreachHitsMech = new Dictionary<ChassisLocations, int>();
        public static Dictionary<BuildingLocation, int> BreachHitsTurret = new Dictionary<BuildingLocation, int>();
        public static Dictionary<VehicleChassisLocations, int> BreachHitsVehicle = new Dictionary<VehicleChassisLocations, int>();

        public static void Reset() {
            // Reinitialize state
            Combat = null;

            BreachCheck = 0f;
            BreachAttackId = NO_ATTACK_SEQUENCE_ID;
            BreachHitsMech.Clear();
            BreachHitsTurret.Clear();
            BreachHitsVehicle.Clear();
        }
    }

}



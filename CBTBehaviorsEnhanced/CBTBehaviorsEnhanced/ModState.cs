
using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviorsEnhanced {
    public static class ModState {

        public const int NO_ATTACK_SEQUENCE_ID = -1; // Attack sequences are always 0 or positive integers (see AttackDirector:44) Use -1 to signify 'no sequence'

        // HBS CombatGameState 
        public static CombatGameState Combat = null;
        public static CombatHUD CombatHUD = null;

        // Melee weapon detection state
        public static Weapon MeleeWeapon = null;
        public static MeleeAttackType MeleeType = MeleeAttackType.NotSet;
        public static MeleeStates MeleeStates = null;
        public static Vector3 MeleePreviewPos = Vector3.one;
        public static float CachedDFASelfDamage = 0f;
        public static DamageTable ForceDamageTable = DamageTable.NONE;

        // Breach state
        public static float BreachCheck = 0f;
        public static int BreachAttackId = NO_ATTACK_SEQUENCE_ID;
        public static Dictionary<ChassisLocations, int> BreachHitsMech = new Dictionary<ChassisLocations, int>();
        public static Dictionary<BuildingLocation, int> BreachHitsTurret = new Dictionary<BuildingLocation, int>();
        public static Dictionary<VehicleChassisLocations, int> BreachHitsVehicle = new Dictionary<VehicleChassisLocations, int>();

        // UI Elements
        public static GameObject MeleeAttackContainer = null;
        public static CombatHUDFireButton ChargeFB = null;
        public static CombatHUDFireButton KickFB = null;
        public static CombatHUDFireButton PhysicalWeaponFB = null;
        public static CombatHUDFireButton PunchFB = null;

        public static void Reset() {
            // Reinitialize state
            Combat = null;
            CombatHUD = null;

            // Melee weapon state
            MeleeWeapon = null;
            MeleeType = MeleeAttackType.NotSet;
            MeleeStates = null;
            MeleePreviewPos = Vector3.one;
            CachedDFASelfDamage = 0f;
            ForceDamageTable = DamageTable.NONE;

            // Breach state
            BreachCheck = 0f;
            BreachAttackId = NO_ATTACK_SEQUENCE_ID;
            BreachHitsMech.Clear();
            BreachHitsTurret.Clear();
            BreachHitsVehicle.Clear();

            // UI Elements
            MeleeAttackContainer = null;
            ChargeFB = null;
            KickFB = null;
            PhysicalWeaponFB = null;
            PunchFB = null;
        }
    }

}



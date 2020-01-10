using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.HullIntegrity {

    [HarmonyPatch(typeof(Mech), "ApplyStructureStatDamage")]
    public static class Mech_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Mech __instance, ChassisLocations location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("M:ASSD - entered.");
        }
    }

    [HarmonyPatch(typeof(Turret), "ApplyStructureStatDamage")]
    public static class Turret_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Turret __instance, BuildingLocation location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("T:ASSD - entered.");
        }
    }

    [HarmonyPatch(typeof(Vehicle), "ApplyStructureStatDamage")]
    public static class Vehicle_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Vehicle __instance, VehicleChassisLocations location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("V:ASSD - entered.");
        }
    }

}

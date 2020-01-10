using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using Localize;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.HullIntegrity {

    // Apply hull integrity breaches as per Tac-Ops pg. 54. 

    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    public static class CombatGameState__Init {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(CombatGameState __instance) {
            Mod.Log.Trace("CGS:_I - entered.");

            switch (__instance.MapMetaData.biomeSkin) {
                case Biome.BIOMESKIN.lunarVacuum:
                    ModState.BreachCheck = Mod.Config.Breaches.VacuumCheck;
                    Mod.Log.Debug($"Lunar biome detected - setting breach chance to: {ModState.BreachCheck}");
                    break;
                case Biome.BIOMESKIN.martianVacuum:
                    ModState.BreachCheck = Mod.Config.Breaches.ThinAtmoCheck;
                    Mod.Log.Debug($"Martian biome detected - setting breach chance to: {ModState.BreachCheck}");
                    break;
                default:
                    return;
            }
        }

    }


    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(CombatGameState __instance) {
            Mod.Log.Trace("CGS:OCGD - entered.");

            // Reset any combat state
            ModState.BreachCheck = 0f;
        }
    }


    [HarmonyPatch(typeof(Mech), "ApplyStructureStatDamage")]
    public static class Mech_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Mech __instance, ChassisLocations location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("M:ASSD - entered.");

            if (ModState.BreachCheck == 0f) { return; } // nothing to do

            bool passedCheck = CheckHelper.DidCheckPassThreshold(ModState.BreachCheck, __instance, 0f, Mod.Config.LocalizedFloaties[ModConfig.FT_Hull_Breach]);
            Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance)} HULL BREACH check: {passedCheck} for location: {location}");
            if (!passedCheck) {

                string floatieText = new Text(Mod.Config.LocalizedFloaties[ModConfig.FT_Hull_Breach]).ToString();
                MultiSequence showInfoSequence = new ShowActorInfoSequence(__instance, floatieText, FloatieMessage.MessageNature.Debuff, false);
                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(showInfoSequence));

                QuipHelper.PublishQuip(__instance, Mod.Config.Qips.Breach);

                if (location <= ChassisLocations.RightTorso) {
                    switch (location) {
                        case ChassisLocations.Head:
                            Mod.Log.Debug($"  Head structure damage taken, killing pilot!");
                            __instance.GetPilot().KillPilot(__instance.Combat.Constants, "", 0, DamageType.Enemy, null, null);
                            break;
                        case ChassisLocations.CenterTorso:                            
                        default:
                            if (location == ChassisLocations.CenterTorso) { Mod.Log.Debug($"  Center Torso hull breach!"); }
                            // Walk the location and disable every component in it
                            foreach (MechComponent mc in __instance.allComponents.Where(mc => mc.mechComponentRef.MountedLocation == location)) {
                                Mod.Log.Debug($"  Damaging component: {mc.defId} of type: {mc.componentDef.Description.Name}, setting status nonfunctional");
                                mc.DamageComponent(default(WeaponHitInfo), ComponentDamageLevel.NonFunctional, true);
                            }
                            break;
                    }
                }
            } 
        }
    }

    [HarmonyPatch(typeof(Turret), "ApplyStructureStatDamage")]
    public static class Turret_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Turret __instance, BuildingLocation location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("T:ASSD - entered.");
        }
    }

    [HarmonyPatch(typeof(Vehicle), "applyStructureStatDamage")]
    public static class Vehicle_ApplyStructureStatDamage {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(Vehicle __instance, VehicleChassisLocations location, float damage, WeaponHitInfo hitInfo) {
            Mod.Log.Trace("V:ASSD - entered.");
        }
    }

}

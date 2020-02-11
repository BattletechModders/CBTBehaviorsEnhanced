using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(TurnDirector), "OnEncounterBegin")]
    public static class TurnDirector_OnEncounterBegin { 
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message) {

            // Remain interleaved until the end of the round. This prevents the case where you immediately get dropped out of combat, then get forced back into it.
            if (__instance != null && __instance.Combat.EncounterLayerData != null)
            {
                __instance.Combat.EncounterLayerData.turnDirectorBehavior = TurnDirectorBehaviorType.RemainInterleaved;
                Mod.Log.Info("Interleaved mode set to: REMAIN_INTERLEAVED");
            }
            else
            {
                Mod.Log.Warn("COULD NOT FIND ENCOUNTER_LAYER_DATA - INTERLEAVED FIX CANNOT BE APPLIED!");
            }

            // Check for hull breach biomes
            TerrainGenerator terrainGenerator = Terrain.activeTerrains != null ? Terrain.activeTerrain.GetComponent<TerrainGenerator>() : null;
            Biome.BIOMESKIN biomeSkin = terrainGenerator != null && terrainGenerator.biome != null ? terrainGenerator.biome.biomeSkin : Biome.BIOMESKIN.UNDEFINED;
            if (__instance.Combat.MapMetaData == null) {
                Mod.Log.Warn("COULD NOT DETERMINE BIOMESKIN, SKIPPING BREACH CHECK");
            } else {
                switch (__instance.Combat.MapMetaData.biomeSkin) {
                    case Biome.BIOMESKIN.lunarVacuum:
                        ModState.BreachCheck = Mod.Config.Breaches.VacuumCheck;
                        Mod.Log.Debug($"Lunar biome detected - setting breach chance to: {ModState.BreachCheck}");
                        break;
                    case Biome.BIOMESKIN.martianVacuum:
                        ModState.BreachCheck = Mod.Config.Breaches.ThinAtmoCheck;
                        Mod.Log.Debug($"Martian biome detected - setting breach chance to: {ModState.BreachCheck}");
                        break;
                    default:
                        Mod.Log.Debug($"Biome of {__instance.Combat.MapMetaData.biomeSkin} detected. No special behaviors.");
                        return;
                }
            }
        }
    }
}

using BattleTech;
using Harmony;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(TurnDirector), "OnEncounterBegin")]
    public static class TurnDirector_OnEncounterBegin { 
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message) {

            // Remain interleaved until the end of the round. This prevents the case where you immediately get dropped out of combat, then get forced back into it.
            if (__instance != null && __instance.Combat.EncounterLayerData != null) {
                __instance.Combat.EncounterLayerData.turnDirectorBehavior = TurnDirectorBehaviorType.RemainInterleaved;
                Mod.Log.Info("Interleaved mode set to: REMAIN_INTERLEAVED");
            } else {
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

    [HarmonyPatch(typeof(TurnDirector), "OnTurnActorActivateComplete")]
    public static class TurnDirector_OnTurnActorActivateComplete {
        private static bool Prefix(TurnDirector __instance) {
            Mod.Log.Trace($"TD:OTAAC invoked");

            if (__instance.IsMissionOver) {
                return false;
            }

            Mod.Log.Debug($"TD isInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.Combat.TurnDirector.IsInterleavePending}" +
                $"  isNonInterleavePending: {__instance.Combat.TurnDirector.IsNonInterleavePending}");

            foreach (TurnActor turnActor in __instance.TurnActors)
            {
                if (turnActor is Team teamActor) 
                {
                    Mod.Log.Debug($" -- TEAM: {teamActor.Name} --");
                    foreach (AbstractActor actor in teamActor.units)
                    {
                        Mod.Log.Debug($" ---- UNIT: {actor}");
                    }
                }
                else if (turnActor is AITeam aiTeam)
                {
                    Mod.Log.Debug($" -- AI TEAM: {aiTeam.Name} --");
                    foreach (AbstractActor actor in aiTeam.units)
                    {
                        Mod.Log.Debug($" ---- UNIT: {actor}");
                    }
                } 
                else
                {
                    Mod.Log.Debug($"Unknown team activated! {turnActor.GUID}");
                }
            }

            List<ITaggedItem> objectsOfType = __instance.Combat.ItemRegistry.GetObjectsOfType(TaggedObjectType.Unit);
            for (int i = 0; i < objectsOfType.Count; i++)
            {
                AbstractActor abstractActor = objectsOfType[i] as AbstractActor;
                if (abstractActor != null && abstractActor.team == null)
                {
                    Mod.Log.Error($"Unit {CombatantUtils.Label(abstractActor)} has no assigned team!");
                }
            }

            int numUnusedUnitsForCurrentPhase = __instance.TurnActors[__instance.ActiveTurnActorIndex].GetNumUnusedUnitsForCurrentPhase();
            Mod.Log.Info($"There are {numUnusedUnitsForCurrentPhase} unusedUnits in the current phase");

            if (!__instance.IsInterleavePending && !__instance.IsInterleaved && numUnusedUnitsForCurrentPhase > 0) {
                Mod.Log.Info("Sending TurnActorActivateMessage");
                Traverse staamT = Traverse.Create(__instance).Method("SendTurnActorActivateMessage", new object[] { __instance.ActiveTurnActorIndex });
                staamT.GetValue();
            } else {
                Mod.Log.Debug("Incrementing ActiveTurnActor");
                Traverse iataT = Traverse.Create(__instance).Method("IncrementActiveTurnActor");
                iataT.GetValue();
            }

            return false;
        }
    }

    // Intercept the condition where contact has been lost, and shift that logic to the end of the round
    [HarmonyPatch(typeof(TurnDirector), "NotifyContact")]
    public static class TurnDirector_NotifyContact {
        public static bool Prefix(TurnDirector __instance, VisibilityLevel contactLevel) {
            Mod.Log.Trace($"TD:NC - entered.");
            if (__instance.IsInterleaved && contactLevel == VisibilityLevel.None && !__instance.DoAnyUnitsHaveContactWithEnemy) {
                Mod.Log.Info("Intercepting lostContact state, allowing remainder of actors to move.");
                return false;
            } else {
                return true;
            }
        }
    }

    // Intercept the condition where contact has been lost, and shift that logic to the beginning of a turn.
    [HarmonyPatch(typeof(TurnDirector), "EndCurrentRound")]
    public static class TurnDirector_EndCurrentRound {
        public static void Postfix(TurnDirector __instance) {
            Mod.Log.Trace($"TD:ECR - entered.");
            if (__instance.IsInterleaved && !__instance.DoAnyUnitsHaveContactWithEnemy) {
                Mod.Log.Info("No actors have contact, returning to non-interleaved mode.");

                Traverse turnDirectorT = Traverse.Create(__instance).Property("IsInterleaved");
                turnDirectorT.SetValue(false);

                __instance.Combat.MessageCenter.PublishMessage(new LostContactMessage());
                return;
            }
        }
    }

    // Because we've intercepted this, we can safely change to non-interleaved mode
    [HarmonyPatch(typeof(TurnDirector), "OnLostContact")]
    public static class TurnDirector_OnLostContact {
        public static void Postfix(TurnDirector __instance) {
            Mod.Log.Trace($"TD:OLC - entered.");

            Mod.Log.Info($"TD isInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.Combat.TurnDirector.IsInterleavePending}" +
                $"  isNonInterleavePending: {__instance.Combat.TurnDirector.IsNonInterleavePending}");
            Mod.Log.Info("Changing interleaved type!");

            __instance.Combat.MessageCenter.PublishMessage(new InterleaveChangedMessage());
        }
    }
}

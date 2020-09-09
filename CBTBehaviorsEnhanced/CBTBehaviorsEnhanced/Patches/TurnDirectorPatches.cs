using BattleTech;
using Harmony;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(TurnDirector), "OnEncounterBegin")]
    public static class TurnDirector_OnEncounterBegin
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {

            // Remain interleaved until the end of the round. This prevents the case where you immediately get dropped out of combat, then get forced back into it.
            if (__instance != null && __instance.Combat.EncounterLayerData != null)
            {
                __instance.Combat.EncounterLayerData.turnDirectorBehavior = TurnDirectorBehaviorType.RemainInterleaved;
                Mod.Log.Info?.Write("Interleaved mode set to: REMAIN_INTERLEAVED");
            }
            else
            {
                Mod.Log.Warn?.Write("COULD NOT FIND ENCOUNTER_LAYER_DATA - INTERLEAVED FIX CANNOT BE APPLIED!");
            }

            // Check for hull breach biomes
            TerrainGenerator terrainGenerator = Terrain.activeTerrains != null ? Terrain.activeTerrain.GetComponent<TerrainGenerator>() : null;
            Biome.BIOMESKIN biomeSkin = terrainGenerator != null && terrainGenerator.biome != null ? terrainGenerator.biome.biomeSkin : Biome.BIOMESKIN.UNDEFINED;
            if (__instance.Combat.MapMetaData == null)
            {
                Mod.Log.Warn?.Write("COULD NOT DETERMINE BIOMESKIN, SKIPPING BREACH CHECK");
            }
            else
            {
                switch (__instance.Combat.MapMetaData.biomeSkin)
                {
                    case Biome.BIOMESKIN.lunarVacuum:
                        ModState.BreachCheck = Mod.Config.Breaches.VacuumCheck;
                        Mod.Log.Debug?.Write($"Lunar biome detected - setting breach chance to: {ModState.BreachCheck}");
                        break;
                    case Biome.BIOMESKIN.martianVacuum:
                        ModState.BreachCheck = Mod.Config.Breaches.ThinAtmoCheck;
                        Mod.Log.Debug?.Write($"Martian biome detected - setting breach chance to: {ModState.BreachCheck}");
                        break;
                    default:
                        Mod.Log.Debug?.Write($"Biome of {__instance.Combat.MapMetaData.biomeSkin} detected. No special behaviors.");
                        return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnTurnActorActivateComplete")]
    public static class TurnDirector_OnTurnActorActivateComplete
    {
        private static bool Prefix(TurnDirector __instance)
        {
            Mod.Log.Trace?.Write($"TD:OTAAC invoked");

            if (__instance.IsMissionOver)
            {
                return false;
            }

            Mod.Log.Debug?.Write($"TD isInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.Combat.TurnDirector.IsInterleavePending}" +
                $"  isNonInterleavePending: {__instance.Combat.TurnDirector.IsNonInterleavePending}");

            foreach (TurnActor turnActor in __instance.TurnActors)
            {
                if (turnActor is Team teamActor)
                {
                    Mod.Log.Debug?.Write($" -- TEAM: {teamActor.Name} --");
                    foreach (AbstractActor actor in teamActor.units)
                    {
                        Mod.Log.Debug?.Write($" ---- UNIT: {actor}");
                    }
                }
                else if (turnActor is AITeam aiTeam)
                {
                    Mod.Log.Debug?.Write($" -- AI TEAM: {aiTeam.Name} --");
                    foreach (AbstractActor actor in aiTeam.units)
                    {
                        Mod.Log.Debug?.Write($" ---- UNIT: {actor}");
                    }
                }
                else
                {
                    Mod.Log.Debug?.Write($"Unknown team activated! {turnActor.GUID}");
                }
            }

            List<ITaggedItem> objectsOfType = __instance.Combat.ItemRegistry.GetObjectsOfType(TaggedObjectType.Unit);
            for (int i = 0; i < objectsOfType.Count; i++)
            {
                AbstractActor abstractActor = objectsOfType[i] as AbstractActor;
                if (abstractActor != null && abstractActor.team == null)
                {
                    Mod.Log.Error?.Write($"Unit {CombatantUtils.Label(abstractActor)} has no assigned team!");
                }
            }

            int numUnusedUnitsForCurrentPhase = __instance.TurnActors[__instance.ActiveTurnActorIndex].GetNumUnusedUnitsForCurrentPhase();
            Mod.Log.Info?.Write($"There are {numUnusedUnitsForCurrentPhase} unusedUnits in the current phase");

            if (!__instance.IsInterleavePending && !__instance.IsInterleaved && numUnusedUnitsForCurrentPhase > 0)
            {
                Mod.Log.Info?.Write("Sending TurnActorActivateMessage");
                Traverse staamT = Traverse.Create(__instance).Method("SendTurnActorActivateMessage", new object[] { __instance.ActiveTurnActorIndex });
                staamT.GetValue();
            }
            else
            {
                Mod.Log.Debug?.Write("Incrementing ActiveTurnActor");
                Traverse iataT = Traverse.Create(__instance).Method("IncrementActiveTurnActor");
                iataT.GetValue();
            }

            return false;
        }
    }

    // Intercept the condition where contact has been lost, and shift that logic to the end of the round
    [HarmonyPatch(typeof(TurnDirector), "NotifyContact")]
    public static class TurnDirector_NotifyContact
    {
        public static bool Prefix(TurnDirector __instance, VisibilityLevel contactLevel)
        {
            Mod.Log.Trace?.Write($"TD:NC - entered.");
            if (__instance.IsInterleaved && contactLevel == VisibilityLevel.None && !__instance.DoAnyUnitsHaveContactWithEnemy)
            {
                Mod.Log.Info?.Write("Intercepting lostContact state, allowing remainder of actors to move.");
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    // Intercept the condition where contact has been lost, and shift that logic to the beginning of a turn.
    [HarmonyPatch(typeof(TurnDirector), "EndCurrentRound")]
    public static class TurnDirector_EndCurrentRound
    {
        public static void Postfix(TurnDirector __instance)
        {
            Mod.Log.Trace?.Write($"TD:ECR - entered.");
            if (__instance.IsInterleaved && !__instance.DoAnyUnitsHaveContactWithEnemy)
            {
                Mod.Log.Info?.Write("No actors have contact, returning to non-interleaved mode.");

                Traverse turnDirectorT = Traverse.Create(__instance).Property("IsInterleaved");
                turnDirectorT.SetValue(false);

                __instance.Combat.MessageCenter.PublishMessage(new LostContactMessage());
                return;
            }
        }
    }

    // Because we've intercepted this, we can safely change to non-interleaved mode
    [HarmonyPatch(typeof(TurnDirector), "OnLostContact")]
    public static class TurnDirector_OnLostContact
    {
        public static void Postfix(TurnDirector __instance)
        {
            Mod.Log.Trace?.Write($"TD:OLC - entered.");

            Mod.Log.Info?.Write($"TD isInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}  isInterleavePending: {__instance.Combat.TurnDirector.IsInterleavePending}" +
                $"  isNonInterleavePending: {__instance.Combat.TurnDirector.IsNonInterleavePending}");
            Mod.Log.Info?.Write("Changing interleaved type!");

            __instance.Combat.MessageCenter.PublishMessage(new InterleaveChangedMessage());
        }
    }
}

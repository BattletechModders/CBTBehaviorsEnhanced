using BattleTech;
using Harmony;
using IRBTModUtils.Extension;
using System;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
    static class AbstractActor_InitEffectStats
    {
        static void Postfix(AbstractActor __instance)
        {
            Mod.Log.Debug?.Write($"AA:IES entered- setting CanShootAfterSprinting for actor:{__instance.DisplayName}");
            __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnActivationBegin")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int) })]
    static class AbstractActor_OnActivationBegin
    {
        static void Postfix(AbstractActor __instance)
        {
            Mod.ActivationLog.Debug?.Write($"AA:OAB entered - for actor: {__instance.DisplayName} with TD.IsInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}. " +
                $"Setting CanShootAfterSprinting: {__instance.Combat.TurnDirector.IsInterleaved}");
            //This is an easy place to put this where it will always be checked. This is the key to full non-interleaved combat.
            __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, __instance.Combat.TurnDirector.IsInterleaved);
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnRecomputePathing")]
    static class AbstractActor_OnRecomputePathing
    {
        static void Prefix(AbstractActor __instance)
        {
            Mod.MoveLog.Info?.Write($"Recomputing pathing for actor: {__instance.DistinctId()}");
        }

        static void Postfix(AbstractActor __instance)
        {
            Traverse walkGridT = Traverse.Create(__instance.Pathing).Property("WalkingGrid");
            PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
            Traverse sprintGridT = Traverse.Create(__instance.Pathing).Property("SprintingGrid");
            PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
            Traverse backwardGridT = Traverse.Create(__instance.Pathing).Property("BackwardGrid");
            PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

            Mod.MoveLog.Info?.Write($" -- after reset, actor: {__instance.DistinctId()} has maxDistance => " +
                $" walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "ResetPathing")]
    static class AbstractActor_ResetPathing
    {
        static void Prefix(AbstractActor __instance)
        {
            Mod.MoveLog.Info?.Write($"Resetting pathing for actor: {__instance.DistinctId()}");
        }

        static void Postfix(AbstractActor __instance)
        {
            Traverse walkGridT = Traverse.Create(__instance.Pathing).Property("WalkingGrid");
            PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
            Traverse sprintGridT = Traverse.Create(__instance.Pathing).Property("SprintingGrid");
            PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
            Traverse backwardGridT = Traverse.Create(__instance.Pathing).Property("BackwardGrid");
            PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

            Mod.MoveLog.Info?.Write($" -- after reset, actor: {__instance.DistinctId()} has maxDistance => " +
                $" walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
        }
    }

    [HarmonyPatch(typeof(Pathing), "ResetPathGrid")]
    static class Pathing_ResetPathGrid
    {
        static void Prefix(Pathing __instance, Vector3 origin, float beginAngle, AbstractActor actor, bool justStoodUp)
        {
            Mod.MoveLog.Info?.Write($"Resetting path grid for actor: {actor.DistinctId()} for origin: {origin} and beginAngle: {beginAngle}");
        }

        static void Postfix(Pathing __instance, Vector3 origin, float beginAngle, AbstractActor actor, bool justStoodUp)
        {
            Traverse walkGridT = Traverse.Create(__instance).Property("WalkingGrid");
            PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
            Traverse sprintGridT = Traverse.Create(__instance).Property("SprintingGrid");
            PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
            Traverse backwardGridT = Traverse.Create(__instance).Property("BackwardGrid");
            PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

            Mod.MoveLog.Info?.Write($" -- after reset, actor: {actor.DistinctId()} has maxDistance => " +
                $" walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
        }
    }
}

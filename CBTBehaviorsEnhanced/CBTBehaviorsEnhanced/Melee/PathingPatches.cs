﻿using CustAmmoCategories;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee
{

    [HarmonyPatch(typeof(Pathing), "GetMeleeDestsForTarget")]
    static class Pathing_GetMeleeDestsForTarget
    {

        // WARNING: Prefix false code here!
        // Allow the player to move if they are already in combat. Vanilla normally disables this.
        static void Prefix(ref bool __runOriginal, Pathing __instance, AbstractActor target, ref List<PathNode> __result)
        {
            if (!__runOriginal) return;

            Mod.MeleeLog.Debug?.Write($"Building melee pathing from attacker: {CombatantUtils.Label(__instance.OwningActor)} at pos: {__instance.OwningActor.CurrentPosition} rot: {__instance.OwningActor.CurrentRotation} " +
                $"to target: {CombatantUtils.Label(target)} at pos: {target.CurrentPosition} rot: {target.CurrentRotation}");

            // If the target isn't visible, prevent building any path to them.
            VisibilityLevel visibilityLevel = __instance.OwningActor.VisibilityToTargetUnit(target);
            if (visibilityLevel < VisibilityLevel.LOSFull && visibilityLevel != VisibilityLevel.BlipGhost)
            {
                __result = new List<PathNode>();
                __runOriginal = false;
                return;
            }

            // If the target ignores pathing (it's a VTOL or similar), prevent building any pathing to them
            if (target != null && target.UnaffectedPathing())
            {
                Mod.Log.Info?.Write($"Target: {target.DistinctId()} is unaffected by pathing - cannot be meleed!");
                __result = new List<PathNode>();
                __runOriginal = false;
                return;
            }

            // Get the Melee and Sprinting grids
            Traverse walkingGridT = Traverse.Create(__instance).Property("WalkingGrid");
            PathNodeGrid walkingGrid = walkingGridT.GetValue<PathNodeGrid>();
            Traverse sprintingGridT = Traverse.Create(__instance).Property("SprintingGrid");
            PathNodeGrid sprintingGrid = sprintingGridT.GetValue<PathNodeGrid>();

            PathNodeGrid meleeGrid = sprintingGrid;
            if (__instance.OwningActor.MaxWalkDistance > __instance.OwningActor.MaxSprintDistance)
            {
                Mod.MeleeLog.Info?.Write($"Using walkingGrid to find all possible nodes");
                meleeGrid = walkingGrid;
            }

            // Calculate all possible nodes
            List<PathNode> pathNodesForPoints = Pathing.GetPathNodesForPoints(
                target.Combat.HexGrid.GetAdjacentPointsOnGrid(target.CurrentPosition), meleeGrid);

            // Remove any that have blockers, or are beyond the max vertical offset
            for (int i = pathNodesForPoints.Count - 1; i >= 0; i--)
            {
                bool beyondMaxVerticalOffset = Mathf.Abs(pathNodesForPoints[i].Position.y - target.CurrentPosition.y) > target.Combat.Constants.MoveConstants.MaxMeleeVerticalOffset;
                bool isBlocked = meleeGrid.FindBlockerReciprocal(pathNodesForPoints[i].Position, target.CurrentPosition);
                if (beyondMaxVerticalOffset || isBlocked)
                {
                    pathNodesForPoints.RemoveAt(i);
                }
            }
            Mod.MeleeLog.Debug?.Write($"  found {pathNodesForPoints.Count} nodes that are not blocked and within height");

            if (pathNodesForPoints.Count > 1)
            {
                // Sort the nodes either by pathing cost, or current distance from attacker
                if (target.Combat.Constants.MoveConstants.SortMeleeHexesByPathingCost)
                {
                    pathNodesForPoints.Sort((PathNode a, PathNode b) => a.CostToThisNode.CompareTo(b.CostToThisNode));
                }
                else
                {
                    pathNodesForPoints.Sort(
                        (PathNode a, PathNode b) => Vector3.Distance(a.Position, __instance.OwningActor.CurrentPosition)
                        .CompareTo(Vector3.Distance(b.Position, __instance.OwningActor.CurrentPosition))
                        );
                }

                // Limit the number of possible solutions. 
                int maxChoices = target.Combat.Constants.MoveConstants.NumMeleeDestinationChoices;

                // If you are already in melee, don't allow moving. Node 0 should be your current position by either sort, 
                //  and thus if you don't have to move you should be prevented from doing so.
                // TODO: Test that this works!
                //Vector3 vector = __instance.OwningActor.CurrentPosition - pathNodesForPoints[0].Position;
                //vector.y = 0f;
                //if (vector.magnitude < 10f) {
                //    maxChoices = 1;
                //}

                while (pathNodesForPoints.Count > maxChoices)
                {
                    pathNodesForPoints.RemoveAt(pathNodesForPoints.Count - 1);
                }
            }

            __result = pathNodesForPoints;

            __runOriginal = false;
            return;
        }
    }

    // TODO: New code

    [HarmonyPatch(typeof(Pathing), "getGrid")]
    static class Pathing_getGrid
    {
        static void Postfix(Pathing __instance, MoveType moveType, ref PathNodeGrid __result)
        {
            if (moveType == MoveType.Melee)
            {
                Mod.MeleeLog.Info?.Write($"{__instance.OwningActor.DistinctId()} has " +
                    $"maxWalkDistance: {__instance.OwningActor.MaxWalkDistance}  " +
                    $"maxSprintDistance: {__instance.OwningActor.MaxSprintDistance} ");

                if (__instance.OwningActor.MaxSprintDistance > __instance.OwningActor.MaxWalkDistance)
                {
                    Mod.MeleeLog.Info?.Write($"Setting meleeGrid to SprintingGrid");

                    Traverse sprintingGridT = Traverse.Create(__instance).Property("SprintingGrid");
                    __result = sprintingGridT.GetValue<PathNodeGrid>();
                }
                else
                {
                    Mod.MeleeLog.Info?.Write($"Setting meleeGrid to meleeGrid");
                }
            }
        }
    }

    // If melee from sprint is enable, use a transpile to swap the grids used to calculate the path
    [HarmonyPatch(typeof(Pathing), "SetMeleeTarget")]
    public static class Pathing_SetMeleeTarget
    {
        static void Prefix(ref bool __runOriginal, Pathing __instance, AbstractActor target)
        {
            if (!__runOriginal) return;

            __instance.UnlockPosition();

            Traverse moveTypeT = Traverse.Create(__instance).Property("MoveType");
            moveTypeT.SetValue(MoveType.Melee);

            Traverse currentMeleeTargetT = Traverse.Create(__instance).Property("CurrentMeleeTarget");
            currentMeleeTargetT.SetValue(target);
            Traverse currentDestinationT = Traverse.Create(__instance).Property("CurrentDestination");
            currentDestinationT.SetValue(target.CurrentPosition);

            Traverse hasMeleeDestSelectionT = Traverse.Create(__instance).Property("HasMeleeDestSelection");
            hasMeleeDestSelectionT.SetValue(false);
            if (target != null)
            {
                List<AbstractActor> allActors = SharedState.Combat.AllActors;
                allActors.Remove(__instance.OwningActor);
                allActors.Remove(target);
                if (__instance.GetMeleeDestination(target, allActors, out var endNode, out __instance.ResultDestination, out __instance.ResultAngle))
                {
                    Traverse meleeGridT = Traverse.Create(__instance).Property("MeleeGrid");
                    Traverse sprintingGridT = Traverse.Create(__instance).Property("SprintingGrid");
                    PathNodeGrid pathNodeGrid = __instance.OwningActor.MaxSprintDistance > __instance.OwningActor.MaxWalkDistance ?
                        sprintingGridT.GetValue<PathNodeGrid>() : meleeGridT.GetValue<PathNodeGrid>();

                    Mod.MeleeLog.Info?.Write($"{__instance.OwningActor.DistinctId()} has " +
                        $"maxWalkDistance: {__instance.OwningActor.MaxWalkDistance}  " +
                        $"maxSprintDistance: {__instance.OwningActor.MaxSprintDistance} ");

                    __instance.CurrentPath = pathNodeGrid.BuildPathFromEnd(endNode,
                        __instance.OwningActor.MaxMeleeEngageRangeDistance, __instance.ResultDestination,
                        target.CurrentPosition, target, out var _,
                        out __instance.ResultDestination, out __instance.ResultAngle);
                }
                else
                {
                    Mod.MeleeLog.Info?.Write($"{__instance.OwningActor.DistinctId()} attempted to melee a target that could not be pathed to!");
                }
            }
            else
            {
                Mod.MeleeLog.Warn?.Write($"{__instance.OwningActor.DistinctId()} is attemping to melee a building. WTF?");
            }
            __instance.LockPosition();

            __runOriginal = false;
        }
    }

    [HarmonyPatch(typeof(Pathing), "UpdateMeleePath")]
    [HarmonyBefore("io.mission.customunits")]
    static class Pathing_UpdateMeleePath
    {
        private static Vector3 resultDest = Vector3.zero;

        static void Postfix(Pathing __instance, bool calledFromUI)
        {
            try
            {
                if (__instance.ResultDestination == null || __instance.CurrentPath == null || __instance.OwningActor == null || __instance.CurrentGrid == null)
                {
                    Mod.Log.Warn?.Write($"UpdateMeleePath diagnostics failed - CU patch may NRE shortly!");
                    Mod.Log.Warn?.Write($"  -- OwningActor != null? {(__instance.OwningActor != null)}  owningActor: {__instance.OwningActor.DistinctId()}");
                    Mod.Log.Warn?.Write($"  -- CurrentGrid != null? {(__instance.CurrentGrid != null)}  ResultDestination != null? {(__instance.ResultDestination != null)}");
                    Mod.Log.Warn?.Write($"  -- CurrentPath != null? {(__instance.CurrentPath != null)}");
                }

                if (__instance.CurrentPath != null)
                {
                    foreach (PathNode node in __instance.CurrentPath)
                    {
                        if (node.Position == null)
                        {
                            Mod.Log.Warn?.Write($"Found a path node with no position! This should not happen!");
                            Mod.Log.Warn?.Write($"  -- nodeIndex: {node.Index}  occupyingActor: {node.OccupyingActor}");
                        }

                    }
                }

            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, $"UpdateMeleePath diagnostics failed - CU patch may NRE shortly!");
                Mod.Log.Warn?.Write(e, $"  -- OwningActor != null? {(__instance.OwningActor != null)}  owningActor: {__instance.OwningActor.DistinctId()}");
                Mod.Log.Warn?.Write(e, $"  -- CurrentGrid != null? {(__instance.CurrentGrid != null)}  ResultDestination != null? {(__instance.ResultDestination != null)}");
                Mod.Log.Warn?.Write(e, $"  -- CurrentPath != null? {(__instance.CurrentPath != null)}");
            }
        }
    }
}

﻿using BattleTech;
using CustAmmoCategories;
using CustomUnits;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public class MeleeState
    {
        public ChargeAttack Charge;
        public DFAAttack DFA;
        public KickAttack Kick;
        public WeaponAttack PhysicalWeapon;
        public PunchAttack Punch;

        public readonly Mech attacker;
        public readonly Vector3 attackPos;
        public readonly AbstractActor target;
        public readonly bool skipValidatePathing;
        public readonly HashSet<MeleeAttackType> validAnimations;

        public bool HasWalkAttackNodes { get => walkNodes?.Count > 0; }
        private readonly List<PathNode> walkNodes;

        public bool HasSprintAttackNodes { get => sprintNodes?.Count > 0; }
        private readonly List<PathNode> sprintNodes;

        public MeleeState(Mech attacker, Vector3 attackPos, AbstractActor target, bool skipValidatePathing = false)
        {
            this.attacker = attacker;
            this.attackPos = attackPos;
            this.target = target;
            this.skipValidatePathing = skipValidatePathing;
            this.validAnimations = AvailableAttacks(attacker, attackPos, target);
            this.walkNodes = GetMeleeDestsForTarget(attacker, attackPos, target, true);
            this.sprintNodes = GetMeleeDestsForTarget(attacker, attackPos, target, false);

            Charge = new ChargeAttack(this);
            DFA = new DFAAttack(this);
            Kick = new KickAttack(this);
            PhysicalWeapon = new WeaponAttack(this);
            Punch = new PunchAttack(this);
        }

        public MeleeState()
        {
            validAnimations = new HashSet<MeleeAttackType>();
            walkNodes = new List<PathNode>();
            sprintNodes = new List<PathNode>();
        }

        // Players hate that charge gets auto-selected when other attacks are available. The
        //   preferred order is Kick, Weap, Punch, and Charge only if no other attack is valid
        public MeleeAttack GetHighestDamageAttackForUI()
        {
            MeleeAttack selectedAttack = null;

            List<MeleeAttack> attacks = new List<MeleeAttack> { PhysicalWeapon, Kick, Punch };
            float selectedDamage = 0;
            foreach (MeleeAttack attack in attacks)
            {
                if (attack != null && attack.IsValid)
                {
                    // TODO: Include attack modifiers for EV style calculaion
                    float typeDamage = attack.TargetDamageClusters.Sum();
                    if (typeDamage > selectedDamage)
                    {
                        selectedDamage = typeDamage;
                        selectedAttack = attack;
                    }
                }
                else
                {
                    Mod.MeleeLog.Debug?.Write($"Attack: {attack?.Label} is null or invalid, skipping.");
                }
            }

            // If everything remains zero, check charge
            if (selectedAttack == null && Charge.IsValid)
            {
                Mod.MeleeLog.Debug?.Write($"Selecting charge as there is no selected attack.");
                selectedAttack = Charge;
            }
            else
            {
                Mod.MeleeLog.Debug?.Write($"Already selected attack: {selectedAttack?.Label} or charge is invalid.");
            }

            return selectedAttack;
        }

        // TODO: One method for players, one for AI
        public MeleeAttack GetHighestTargetDamageStateForPlayer(bool includeDFA = false)
        {
            MeleeAttack selectedState = null;
            float selectedDamage = 0;

            // Do not include charge by default; only include it when all other attacks are null
            List<MeleeAttack> allStates = new List<MeleeAttack> { Kick, PhysicalWeapon, Punch };

            // Do not use DFA here, because it's selected differently in UI.
            if (includeDFA && DFA != null) allStates.Add(DFA);

            // Check kick, phyweap, punch first
            foreach (MeleeAttack state in allStates)
            {
                if (state != null)
                {
                    // TODO: Include attack modifiers for EV style calculaion
                    float typeDamage = state.TargetDamageClusters.Sum();
                    if (typeDamage > selectedDamage)
                    {
                        selectedDamage = typeDamage;
                        selectedState = state;
                    }
                }
            }

            // If everything remains zero, check charge
            if (selectedDamage == 0 && Charge.IsValid)
            {
                selectedState = Charge;
            }

            return selectedState;
        }

        private static HashSet<MeleeAttackType> AvailableAttacks(Mech attacker, Vector3 attackPos, ICombatant target)
        {
            Mod.MeleeLog.Info?.Write($"Checking available animations for " +
                $"attacker: {CombatantUtils.Label(attacker)} from position: {attackPos} " +
                $"versus target: {CombatantUtils.Label(target)} at position: {target.CurrentPosition} " +
                $"with distance: {(attackPos - target.CurrentPosition).magnitude}");

            HashSet<MeleeAttackType> availableAttacks = new HashSet<MeleeAttackType>();
            if (target == null) return availableAttacks;

            MeleeAttackHeight validMeleeHeights = GetValidMeleeHeights(attacker, attackPos, target);
            Mod.MeleeLog.Info?.Write($"ValidMeleeHeights => {validMeleeHeights}");

            // If prone, or the attack height is ground, the only thing we can do is stomp the target. 
            // TODO: HBS has vehicles only available as stomp targets. Reinstate that?
            if (target.IsProne || validMeleeHeights == MeleeAttackHeight.Ground)
            {
                availableAttacks.Add(MeleeAttackType.Stomp);
                Mod.MeleeLog.Info?.Write($" - Target is prone or attack height is ground, returning STOMP.");
                return availableAttacks;
            }

            // HBS prevents you from punching a turret. Why? We're changing that 
            bool atPunchHeight = (validMeleeHeights & MeleeAttackHeight.High) != MeleeAttackHeight.None;
            if (atPunchHeight)
            {
                if (attacker is TrooperSquad)
                {
                    Mod.MeleeLog.Info?.Write($" - attacker is a trooper, adding punch animation for physical attack");
                    availableAttacks.Add(MeleeAttackType.Punch);
                }
                else
                {
                    if (attacker.MechDef.Chassis.PunchesWithLeftArm)
                    {
                        if (!attacker.IsLocationDestroyed(ChassisLocations.LeftArm))
                        {
                            Mod.MeleeLog.Info?.Write($" - chassis requires left arm for punch anim, and it exists - adding");
                            availableAttacks.Add(MeleeAttackType.Punch);
                        }
                        else
                        {
                            Mod.MeleeLog.Info?.Write($" - chassis requires left arm for punch anim, but is missing - cannot punch");
                        }
                    }
                    else
                    {
                        if (!attacker.IsLocationDestroyed(ChassisLocations.RightArm))
                        {
                            Mod.MeleeLog.Info?.Write($" - chassis requires right arm for punch anim, and it exists - adding");
                            availableAttacks.Add(MeleeAttackType.Punch);
                        }
                        else
                        {
                            Mod.MeleeLog.Info?.Write($" - chassis requires right arm for punch anim, but is missing - cannot punch");
                        }

                    }
                }
            }

            bool atKickHeight = (validMeleeHeights & MeleeAttackHeight.Low) != MeleeAttackHeight.None;
            if (atKickHeight
                && !attacker.IsLocationDestroyed(ChassisLocations.LeftLeg)
                && !attacker.IsLocationDestroyed(ChassisLocations.RightLeg)
                )
            {
                Mod.MeleeLog.Info?.Write($" - Adding kick");
                availableAttacks.Add(MeleeAttackType.Kick);
            }

            bool atTackleHeight = (validMeleeHeights & MeleeAttackHeight.Medium) != MeleeAttackHeight.None;
            if (atTackleHeight)
            {
                Mod.MeleeLog.Info?.Write($" - Adding tackle");
                availableAttacks.Add(MeleeAttackType.Tackle);
            }

            Mod.MeleeLog.Info?.Write($" - Returning {availableAttacks.Count} available attack animations");
            return availableAttacks;
        }

        private static MeleeAttackHeight GetValidMeleeHeightsCU(CustomMech attacker, Vector3 attackPosition, CustomMech target)
        {
            float targetDist = Vector3.Distance(attackPosition, target.CurrentPosition);
            Mod.MeleeLog.Info?.Write($" - attackposition to target position:{targetDist} border {attacker.Combat.HexGrid.HexWidth * 1.5f}");
            if (targetDist > (attacker.Combat.HexGrid.HexWidth * 1.5f))
            {
                Mod.MeleeLog.Info?.Write($" - not near. Assume preview");
                return MeleeAttackHeight.Ground;
            }
            Vector3 attackToTargetPos = attackPosition;
            attackToTargetPos.y = target.CurrentPosition.y;
            attackToTargetPos = target.CurrentPosition - attackToTargetPos;
            Quaternion toTargetRot = Quaternion.LookRotation(attackToTargetPos);
            //Vector3[] targetLoS = target.GetLOSTargetPositions(target.CurrentPosition, target.CurrentRotation, 0f);
            Vector3[] attackerLoS = attacker.GetLOSSourcePositions(attackPosition, toTargetRot, 0f);
            float attackerHighestLoS = float.NaN;
            foreach (var los in attackerLoS)
            {
                if (float.IsNaN(attackerHighestLoS) || (attackerHighestLoS < los.y)) { attackerHighestLoS = los.y; }
            }
            if (float.IsNaN(attackerHighestLoS)) {
                attackerHighestLoS = attackPosition.y + (attacker.TargetPosition.y - attacker.FlyingHeight() - attacker.CurrentPosition.y);
            }
            float targetHighestLoS = target.TargetPosition.y - target.FlyingHeight();
            float targetLowestLoS = target.CurrentPosition.y;

            float attackerLowestLoS = attackPosition.y;
            float attackerHeight = attackerHighestLoS - attackerLowestLoS;
            MeleeAttackHeight result = MeleeAttackHeight.None;
            float delta20 = attackerHeight * 0.2f + attackerLowestLoS;
            float delta20_2 = attackerHeight * 0.2f + attackerLowestLoS;

            float delta30 = attackerHeight * 0.3f + attackerLowestLoS;

            float delta75 = attackerHeight * 0.75f + attackerLowestLoS;

            float delta45 = attackerHeight * 0.45f + attackerLowestLoS;
            float delta45_2 = attackerHeight * 0.45f + attackerLowestLoS;

            Mod.MeleeLog.Info?.Write($" - attackerLowestLoS: {attackerLowestLoS} attackerHighestLoS: {attackerHighestLoS} attackerHeight: {attackerHeight}");
            Mod.MeleeLog.Info?.Write($" - delta20: {delta20} delta30: {delta30} delta45: {delta45} delta45: {delta75}");
            Mod.MeleeLog.Info?.Write($" - targetLowestLoS: {targetLowestLoS} targetHighestLoS: {targetHighestLoS}");
            if ((attackerLowestLoS <= targetHighestLoS) && (delta20 >= targetLowestLoS)) {
                Mod.MeleeLog.Info?.Write(" - adding Ground attack.");
                result |= MeleeAttackHeight.Ground;
            }
            if ((delta20 <= targetHighestLoS) && (delta45 >= targetLowestLoS))
            {
                Mod.MeleeLog.Info?.Write(" - adding Low attack.");
                result |= MeleeAttackHeight.Low;
            }
            if ((delta30 <= targetHighestLoS) && (delta75 >= targetLowestLoS))
            {
                Mod.MeleeLog.Info?.Write(" - adding Medium attack.");
                result |= MeleeAttackHeight.Medium;
            }
            if ((delta45 <= targetHighestLoS) && (attackerHighestLoS >= targetLowestLoS))
            {
                Mod.MeleeLog.Info?.Write(" - adding High attack.");
                result |= MeleeAttackHeight.High;
            }
            return result;
        }

        // Duplication of HBS code for improved readability and logging
        private static MeleeAttackHeight GetValidMeleeHeights(Mech attacker, Vector3 attackPosition, ICombatant target)
        {
            Mod.MeleeLog.Info?.Write($"Evaluating melee attack height for {attacker.DistinctId()} at position: {attackPosition} " +
                $"vs. target: {target.DistinctId()}");

            MeleeAttackHeight meleeAttackHeight = MeleeAttackHeight.None;
            AbstractActor targetActor = target as AbstractActor;

            if (targetActor == null || targetActor.UnitType == UnitType.Turret)
            {
                return (MeleeAttackHeight.Low | MeleeAttackHeight.Medium | MeleeAttackHeight.High);
            }
            CustomMech attackerCU = attacker as CustomMech;
            CustomMech targetCU = targetActor as CustomMech;
            if ((attackerCU != null) && (target != null)) {
                Mod.MeleeLog.Info?.Write($" - both attacker and target is custom units. Behavior is extended");
                return GetValidMeleeHeightsCU(attackerCU, attackPosition, targetCU);
            }
            // CU integration; subtract flying height from current Position.
            //   Treat both units as if they were 'on the ground'
            float attackerLOS_Y = attacker.LOSSourcePositions[0].y - attacker.FlyingHeight();
            float attackerLOS0Height = attackerLOS_Y - attacker.CurrentPosition.y;
            Mod.MeleeLog.Info?.Write($" - attackLOSSrc_Y: {attacker.LOSSourcePositions[0].y} - flyingHeight: {attacker.FlyingHeight()} = attackerLOS_Y: {attackerLOS_Y} => attackerHeightBaseToLOS: {attackerLOS0Height}");

            float attackerBase_Y = attackPosition.y;
            attackerLOS_Y = attackerBase_Y + attackerLOS0Height;
            Mod.MeleeLog.Info?.Write($" - attackPosition_Y: {attackerBase_Y} + attackerHeightBaseToLOS: {attackerLOS0Height} = attackerLOS_Y: {attackerLOS_Y}");

            float targetBase_Y = target.CurrentPosition.y;
            float targetLOS_Y = ((AbstractActor)target).LOSSourcePositions[0].y;
            Mod.MeleeLog.Info?.Write($" - targetBase_Y: {targetBase_Y} targetLOS_Y: {targetLOS_Y}");

            // If attacker base > target base -> attacker base
            // else if target base > attacker LOS -> attacker LOS 
            // else else -> target base
            float lowestAttackPosOnTarget = (attackerBase_Y > targetBase_Y) ?
                attackerBase_Y :
                ((targetBase_Y > attackerLOS_Y) ? attackerLOS_Y : targetBase_Y);

            // If attacker LOS < target LOS -> attacker LOS
            // else if target LOS < attacker base -> attacker base
            // else else -> target LOS
            float highestAttackPosOnTarget = (attackerLOS_Y < targetLOS_Y) ?
                attackerLOS_Y :
                ((targetLOS_Y < attackerBase_Y) ? attackerBase_Y : targetLOS_Y);
            Mod.MeleeLog.Info?.Write($" - lowestAttackPosOnTarget: {lowestAttackPosOnTarget} highestAttackPosOnTarget: {highestAttackPosOnTarget}");

            float lowestAttack_Y = attackerBase_Y;

            float delta20 = attackerLOS0Height * 0.2f + attackerBase_Y;
            float delta20_2 = attackerLOS0Height * 0.2f + attackerBase_Y;

            float delta30 = attackerLOS0Height * 0.3f + attackerBase_Y;

            float delta75 = attackerLOS0Height * 0.75f + attackerBase_Y;

            float delta45 = attackerLOS0Height * 0.45f + attackerBase_Y;
            float delta45_2 = attackerLOS0Height * 0.45f + attackerBase_Y;

            float highestAttack_Y = attackerLOS_Y;

            Mod.MeleeLog.Info?.Write($" - lowestAttack_Y: {lowestAttack_Y}  delta20: {delta20}  delta30: {delta30}  delta45: {delta45}  " +
                $"delta75: {delta75}  highestAttack_Y: {highestAttack_Y}");

            float lowestAttackPosOnTarget_2 = lowestAttackPosOnTarget;

            if ((lowestAttackPosOnTarget_2 <= delta20 && lowestAttack_Y <= highestAttackPosOnTarget) || targetLOS_Y <= lowestAttack_Y)
            {
                Mod.MeleeLog.Info?.Write(" - adding Ground attack.");
                meleeAttackHeight |= MeleeAttackHeight.Ground;
            }

            if (lowestAttackPosOnTarget_2 <= delta45 && delta20_2 <= highestAttackPosOnTarget)
            {
                Mod.MeleeLog.Info?.Write(" - adding Low attack.");
                meleeAttackHeight |= MeleeAttackHeight.Low;
            }

            if (lowestAttackPosOnTarget_2 <= delta75 && delta30 <= highestAttackPosOnTarget)
            {
                Mod.MeleeLog.Info?.Write(" - adding Medium attack.");
                meleeAttackHeight |= MeleeAttackHeight.Medium;
            }

            if (lowestAttackPosOnTarget_2 <= highestAttack_Y && delta45_2 <= highestAttackPosOnTarget)
            {
                Mod.MeleeLog.Info?.Write(" - adding High attack.");
                meleeAttackHeight |= MeleeAttackHeight.High;
            }

            Mod.MeleeLog.Info?.Write($" - Melee attack height = {meleeAttackHeight}");
            return meleeAttackHeight;
        }

        //  Patch the HBS code to allow both walking and running grids
        public static List<PathNode> GetMeleeDestsForTarget(AbstractActor attacker, Vector3 attackPos, AbstractActor target, bool useWalkGrid = true)
        {
            Mod.MeleeLog.Info?.Write($"Evaluating melee dests for attacker: {attacker.DistinctId()} at postion: {attackPos}" +
                $" vs target: {target.DistinctId()} at position: {target.CurrentPosition}. UseWalkGrid? : {useWalkGrid}");

            VisibilityLevel visibilityLevel = attacker.VisibilityToTargetUnit(target);
            if (visibilityLevel < VisibilityLevel.LOSFull && visibilityLevel != VisibilityLevel.BlipGhost)
            {
                return new List<PathNode>();
            }

            PathNodeGrid attackGrid = useWalkGrid ? attacker.Pathing.getGrid(MoveType.Walking) : attacker.Pathing.getGrid(MoveType.Sprinting);
            List<Vector3> adjacentPoints = SharedState.Combat.HexGrid.GetAdjacentPointsOnGrid(target.CurrentPosition);
            foreach (Vector3 adjPoint in adjacentPoints)
            {
                Mod.MeleeLog.Debug?.Write($" -- target has adjacent point: {adjPoint}");
            }

            List<PathNode> pathNodesForPoints = Pathing.GetPathNodesForPoints(adjacentPoints, attackGrid);
            for (int num = pathNodesForPoints.Count - 1; num >= 0; num--)
            {
                bool isGreaterThanMaxMeleeOffset = Mathf.Abs(pathNodesForPoints[num].Position.y - target.CurrentPosition.y) > SharedState.Combat.Constants.MoveConstants.MaxMeleeVerticalOffset;
                bool hasBlockingNode = attackGrid.FindBlockerReciprocal(pathNodesForPoints[num].Position, target.CurrentPosition);
                if (isGreaterThanMaxMeleeOffset || hasBlockingNode)
                {
                    Mod.MeleeLog.Debug?.Write($"NodeIdx: {num} hasBlockingNode: {hasBlockingNode} or isGreaterThanMeleeOffset: {isGreaterThanMaxMeleeOffset} - discarding.");
                    pathNodesForPoints.RemoveAt(num);
                }
                else
                {
                    Mod.MeleeLog.Debug?.Write($"NodeIdx: {num} is valid point at position: {pathNodesForPoints[num].Position}");
                }
            }

            if (pathNodesForPoints.Count > 1)
            {
                if (SharedState.Combat.Constants.MoveConstants.SortMeleeHexesByPathingCost)
                {
                    Mod.MeleeLog.Debug?.Write($" -- sorting nodes by pathing cost");
                    pathNodesForPoints.Sort((PathNode a, PathNode b) => a.CostToThisNode.CompareTo(b.CostToThisNode));
                }
                else
                {
                    Mod.MeleeLog.Debug?.Write($" -- sorting nodes by raw distance");
                    pathNodesForPoints.Sort((PathNode a, PathNode b) => Vector3.Distance(a.Position, attackPos).CompareTo(Vector3.Distance(b.Position, attackPos)));
                }

                int numSelections = SharedState.Combat.Constants.MoveConstants.NumMeleeDestinationChoices;
                Vector3 vector = attackPos - pathNodesForPoints[0].Position;
                vector.y = 0f;
                if (vector.magnitude < 10f)
                {
                    Mod.MeleeLog.Debug?.Write($" -- attacker less than 10m from target, allowing one node only!");
                    numSelections = 1;
                }

                while (pathNodesForPoints.Count > numSelections)
                {
                    Mod.MeleeLog.Debug?.Write($"  -- removing pathnode as we already have : {pathNodesForPoints.Count} selections.");
                    pathNodesForPoints.RemoveAt(pathNodesForPoints.Count - 1);
                }
            }
            else if (pathNodesForPoints.Count < 1)
            {
                Mod.MeleeLog.Info?.Write($" -- No pathnodes found!");
            }

            return pathNodesForPoints;
        }
    }
}

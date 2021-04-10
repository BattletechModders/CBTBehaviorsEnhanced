using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using IRBTModUtils;
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

        public readonly HashSet<MeleeAttackType> validAnimations;

        public bool HasWalkAttackNodes { get => walkNodes?.Count > 0; }
        private readonly List<PathNode> walkNodes;

        public bool HasSprintAttackNodes { get => sprintNodes?.Count > 0; }
        private readonly List<PathNode> sprintNodes;

        public MeleeState(Mech attacker, Vector3 attackPos, AbstractActor target)
        {
            this.attacker = attacker;
            this.attackPos = attackPos;
            this.target = target;

            this.validAnimations = AvailableAttacks(attacker, attackPos, target);
            this.walkNodes = GetMeleeDestsForTarget(attacker, target, true);
            this.sprintNodes = GetMeleeDestsForTarget(attacker, target, false);

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
            
            List<MeleeAttack> attacks = new List<MeleeAttack> { Kick, PhysicalWeapon, Punch };
            float selectedDamage = 0;
            foreach (MeleeAttack attack in attacks)
            {
                if (attack != null)
                {
                    // TODO: Include attack modifiers for EV style calculaion
                    float typeDamage = attack.TargetDamageClusters.Sum();
                    if (typeDamage > selectedDamage)
                    {
                        selectedDamage = typeDamage;
                        selectedAttack = attack;
                    }
                }
            }

            // If everything remains zero, check charge
            if (selectedDamage == 0 && Charge.IsValid)
                selectedAttack = Charge;

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
                selectedDamage = Charge.TargetDamageClusters.Sum();
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

            Turret turret = target as Turret;
            // HBS prevents you from punching a turret. Why? 
            bool atPunchHeight = (validMeleeHeights & MeleeAttackHeight.High) != MeleeAttackHeight.None;
            if (atPunchHeight && turret == null)
            {
                if (attacker.MechDef.Chassis.PunchesWithLeftArm && !attacker.IsLocationDestroyed(ChassisLocations.LeftArm))
                {
                    Mod.MeleeLog.Info?.Write($" - Adding punch as left arm is not destroyed");
                    availableAttacks.Add(MeleeAttackType.Punch);
                }

                if (!attacker.MechDef.Chassis.PunchesWithLeftArm && !attacker.IsLocationDestroyed(ChassisLocations.RightArm))
                {
                    Mod.MeleeLog.Info?.Write($" - Adding punch as right arm is not destroyed");
                    availableAttacks.Add(MeleeAttackType.Punch);
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


        // Duplication of HBS code for improved readability and logging
        private static MeleeAttackHeight GetValidMeleeHeights(Mech attacker, Vector3 attackPosition, ICombatant target)
        {
            Mod.MeleeLog.Info?.Write($"Evaluating melee attack height for {CombatantUtils.Label(attacker)} at position: {attackPosition} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            MeleeAttackHeight meleeAttackHeight = MeleeAttackHeight.None;
            AbstractActor abstractActor = target as AbstractActor;

            if (abstractActor == null || abstractActor.UnitType == UnitType.Turret)
            {
                return (MeleeAttackHeight.Low | MeleeAttackHeight.Medium | MeleeAttackHeight.High);
            }

            float attackerBase_Y = attacker.CurrentPosition.y;
            float attackerLOS_Y = attacker.LOSSourcePositions[0].y;
            float attackerHeightBaseToLOS = attackerLOS_Y - attackerBase_Y;

            attackerBase_Y = attackPosition.y;
            attackerLOS_Y = attackerBase_Y + attackerHeightBaseToLOS;
            Mod.MeleeLog.Info?.Write($" - attackerBase_Y: {attackerBase_Y} attackerLOS_Y: {attackerLOS_Y} attackerHeightBaseToLOS: {attackerHeightBaseToLOS}");

            float targetBase_Y = target.CurrentPosition.y;
            float targetLOS_Y = ((AbstractActor)target).LOSSourcePositions[0].y;
            Mod.MeleeLog.Info?.Write($" - targetBase_Y: {targetBase_Y} targetLOS_Y: {targetLOS_Y }");

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

            float delta20 = attackerHeightBaseToLOS * 0.2f + attackerBase_Y;
            float delta20_2 = attackerHeightBaseToLOS * 0.2f + attackerBase_Y;

            float delta30 = attackerHeightBaseToLOS * 0.3f + attackerBase_Y;

            float delta75 = attackerHeightBaseToLOS * 0.75f + attackerBase_Y;

            float delta45 = attackerHeightBaseToLOS * 0.45f + attackerBase_Y;
            float delta45_2 = attackerHeightBaseToLOS * 0.45f + attackerBase_Y;

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
        public static List<PathNode> GetMeleeDestsForTarget(AbstractActor attacker, AbstractActor target, bool useWalkGrid = true)
        {
            VisibilityLevel visibilityLevel = attacker.VisibilityToTargetUnit(target);
            if (visibilityLevel < VisibilityLevel.LOSFull && visibilityLevel != VisibilityLevel.BlipGhost)
            {
                return new List<PathNode>();
            }

            PathNodeGrid attackGrid = useWalkGrid ? attacker.Pathing.getGrid(MoveType.Walking) : attacker.Pathing.getGrid(MoveType.Sprinting);
            List<Vector3> adjacentPoints = SharedState.Combat.HexGrid.GetAdjacentPointsOnGrid(target.CurrentPosition);
            List<PathNode> pathNodesForPoints = Pathing.GetPathNodesForPoints(adjacentPoints, attacker.Pathing.getGrid(MoveType.Walking));
            for (int num = pathNodesForPoints.Count - 1; num >= 0; num--)
            {
                if (Mathf.Abs(pathNodesForPoints[num].Position.y - target.CurrentPosition.y) > SharedState.Combat.Constants.MoveConstants.MaxMeleeVerticalOffset ||
                    attackGrid.FindBlockerReciprocal(pathNodesForPoints[num].Position, target.CurrentPosition))
                {
                    pathNodesForPoints.RemoveAt(num);
                }
            }

            if (pathNodesForPoints.Count > 1)
            {
                if (SharedState.Combat.Constants.MoveConstants.SortMeleeHexesByPathingCost)
                {
                    pathNodesForPoints.Sort((PathNode a, PathNode b) => a.CostToThisNode.CompareTo(b.CostToThisNode));
                }
                else
                {
                    pathNodesForPoints.Sort((PathNode a, PathNode b) => Vector3.Distance(a.Position, attacker.CurrentPosition).CompareTo(Vector3.Distance(b.Position, attacker.CurrentPosition)));
                }

                int num2 = SharedState.Combat.Constants.MoveConstants.NumMeleeDestinationChoices;
                Vector3 vector = attacker.CurrentPosition - pathNodesForPoints[0].Position;
                vector.y = 0f;
                if (vector.magnitude < 10f)
                {
                    num2 = 1;
                }

                while (pathNodesForPoints.Count > num2)
                {
                    pathNodesForPoints.RemoveAt(pathNodesForPoints.Count - 1);
                }
            }
            return pathNodesForPoints;
        }
    }
}

using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using CustomUnits;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced.Move
{

    internal abstract class IRBTModUtilMoveModifier
    {
        public abstract int Priority { get; }
        public abstract string Name { get; }

        public abstract float WalkMod(Mech mech, float current);
        public abstract float RunMod(Mech mech, float current);
    }

    // Should be first if at all possible
    internal class TTRun_MoveModifier : IRBTModUtilMoveModifier
    {
        public override int Priority { get { return 0; } }
        public override string Name { get { return "CBTBE_TTRun"; } }

        public override float WalkMod(Mech mech, float current)
        {
            return current;
        }

        public override float RunMod(Mech mech, float current)
        {
            if (mech.IsVehicle() || mech.IsNaval() || mech.IsTrooper()) return current;

            float runMultiMod = mech.StatCollection.GetValue<float>(ModStats.RunMultiMod);
            float runMulti = Mod.Config.Move.RunMulti;
            if (runMultiMod != 0f)
            {
                runMulti += runMultiMod;
            }
            Mod.MoveLog.Debug?.Write($"run mod => {runMulti} from CBTBE_RunMultiMod: {runMultiMod} + modConfig: {Mod.Config.Move.RunMulti}");

            float resetSpeed = mech.WalkSpeed * runMulti;
            Mod.MoveLog.Debug?.Write($"Resetting runSpeed to {resetSpeed} for actor: {mech.DistinctId()}");

            return resetSpeed;
        }
    }

    internal class Heat_MoveModifier : IRBTModUtilMoveModifier
    {
        public override int Priority { get { return 0; } }
        public override string Name { get { return "CBTBE_Heat"; } }

        public override float WalkMod(Mech mech, float current)
        {
            if (mech.IsVehicle() || mech.IsNaval() || mech.IsTrooper()) return current;
            if (mech.CurrentHeat == 0) return current;

            // Check for overheat penalties
            int movePenalty = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement)
            {
                if (mech.CurrentHeat >= kvp.Key)
                {
                    movePenalty = kvp.Value;
                }
            }

            float movePenaltyDist = movePenalty * Mod.Config.Move.HeatMovePenalty;
            Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has walk heat movePenalty: {movePenaltyDist}");
            return current - movePenaltyDist;
        }

        public override float RunMod(Mech mech, float current)
        {
            if (mech.IsVehicle() || mech.IsNaval() || mech.IsTrooper()) return current;
            if (mech.CurrentHeat == 0) return current;

            // Check for overheat penalties
            int movePenaltyKey = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement)
            {
                if (mech.CurrentHeat >= kvp.Key)
                {
                    movePenaltyKey = kvp.Value;
                }
            }

            float movePenaltyDist = movePenaltyKey * Mod.Config.Move.HeatMovePenalty * Mod.Config.Move.RunMulti;
            Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has run heat movePenalty: {movePenaltyDist}");
            return current - movePenaltyDist;
        }
    }

    // Should be last if possible
    internal class Legged_MoveModifier : IRBTModUtilMoveModifier
    {
        public override int Priority { get { return 100; } }
        public override string Name { get { return "CBTBE_Legged"; } }

        public override float WalkMod(Mech mech, float current)
        {
            if (mech.IsVehicle() || mech.IsNaval() || mech.IsTrooper()) return current;

            int destroyedLegs = DestroyedLegsCount(mech);
            if (destroyedLegs == 0) return current;

            if (mech.IsQuadMech())
            {
                if (destroyedLegs < 3)
                {
                    float moveMalus = Mod.Config.Move.MPMetersPerHex * destroyedLegs;
                    Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost {destroyedLegs} of 4 legs, applying speed penalty: " +
                        $"{moveMalus} => metersPerHex: {Mod.Config.Move.MPMetersPerHex} * destroyedLegs: {destroyedLegs}");
                    return moveMalus;
                }
                else
                {
                    Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost {destroyedLegs} of 4 legs, returning a minimum speed of: {Mod.Config.Move.MinimumMove}m");
                    return Mod.Config.Move.MinimumMove;
                }
            }
            else
            {
                Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost 1 of 2 legs, returning a minimum speed of: {Mod.Config.Move.MinimumMove}m");
                return Mod.Config.Move.MinimumMove;
            }

        }

        public override float RunMod(Mech mech, float current)
        {
            if (mech.IsVehicle() || mech.IsNaval() || mech.IsTrooper()) return current;

            int destroyedLegs = DestroyedLegsCount(mech);
            if (destroyedLegs == 0) return current;

            if (mech.IsQuadMech())
            {
                if (destroyedLegs < 3)
                {
                    // We are running, add the RunMulti
                    float moveMalus = Mod.Config.Move.MPMetersPerHex * destroyedLegs * Mod.Config.Move.RunMulti;
                    Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost {destroyedLegs} of 4 legs, applying speed penalty: " +
                        $"{moveMalus} => metersPerHex: {Mod.Config.Move.MPMetersPerHex} * destroyedLegs: {destroyedLegs} * runMulti: {Mod.Config.Move.RunMulti}");
                    return moveMalus;
                }
                else
                {
                    Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost {destroyedLegs} of 4 legs, returning a minimum speed of: {Mod.Config.Move.MinimumMove}m");
                    return Mod.Config.Move.MinimumMove;
                }
            }
            else
            {
                Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has lost 1 of 2 legs, returning a minimum speed of: {Mod.Config.Move.MinimumMove}m");
                return Mod.Config.Move.MinimumMove;
            }
        }

        private static int DestroyedLegsCount(Mech mech)
        {
            int destroyedLegs = 0;
            if (mech.IsLocationDestroyed(ChassisLocations.LeftLeg)) { ++destroyedLegs; }
            if (mech.IsLocationDestroyed(ChassisLocations.RightLeg)) { ++destroyedLegs; }
            UnitCustomInfo unitCustomInfo = mech.GetCustomInfo();
            if (unitCustomInfo != null)
            {
                if (unitCustomInfo.ArmsCountedAsLegs)
                {
                    if (mech.IsLocationDestroyed(ChassisLocations.LeftArm)) { ++destroyedLegs; }
                    if (mech.IsLocationDestroyed(ChassisLocations.RightArm)) { ++destroyedLegs; }
                }
            }
            return destroyedLegs;
        }
    }
}

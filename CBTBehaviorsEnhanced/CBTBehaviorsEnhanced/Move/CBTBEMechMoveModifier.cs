using BattleTech;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.Move
{
    public class CBTBEMechMoveModifier : MechMoveModifier
    {
        public override float ModifyJumpSpeed(Mech mech)
        {
            return 0f;
        }

        public override float ModifyRunSpeed(Mech mech)
        {
            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged)
            {
                float delta = (float)Math.Ceiling(mech.WalkSpeed - Mod.Config.Move.MinimumMove);
                Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} is legged, returning delta of: {delta}m");
                return delta;
            }

            float runMulti = Mod.Config.Move.RunMulti;
            if (mech.StatCollection.ContainsStatistic(ModStats.RunMultiMod))
            {
                float runMultiMod = mech.StatCollection.GetStatistic(ModStats.RunMultiMod).Value<float>();
                runMulti += runMultiMod;
            }
            Mod.MoveLog.Debug?.Write($" Using a final run multiplier of x{runMulti} (from base: {Mod.Config.Move.RunMulti})");

            // Per Battletech Manual, Running MP is always rounded up. Follow that principle here as well.
            float walkSpeed = mech.ModifiedWalkDistance();
            float runSpeed = (float)Math.Ceiling(walkSpeed * runMulti);
            Mod.MoveLog.Debug?.Write($" Mech: {mech.DistinctId()} has RunSpeed of {runSpeed} from walkSpeed: {walkSpeed} x runMulti: {runMulti}");
            
            float runDelta = (float)Math.Ceiling(mech.RunSpeed - runSpeed);
            Mod.MoveLog.Debug?.Write($" delta is: {runDelta}");

            return runDelta;

        }

        public override float ModifyWalkSpeed(Mech mech)
        {
            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged)
            {
                float delta = (float)Math.Ceiling(mech.WalkSpeed - Mod.Config.Move.MinimumMove);
                Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} is legged, returning delta of: {delta}m");
                return delta;
            }

            // Check for overheat penalties
            int movePenaltyKey = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement)
            {
                if (mech.CurrentHeat >= kvp.Key)
                {
                    movePenaltyKey = kvp.Value;
                }
            }
            float movePenaltyDist = movePenaltyKey * Mod.Config.Move.HeatMovePenalty;
            Mod.MoveLog.Debug?.Write($"Mech: {mech.DistinctId()} has move walkPenalty: {movePenaltyDist}");

            return movePenaltyDist;
        }
    }
}

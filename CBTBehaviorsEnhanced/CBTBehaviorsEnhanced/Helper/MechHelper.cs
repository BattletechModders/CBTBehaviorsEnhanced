using BattleTech;
using CBTBehaviors;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Helper {
    public class MechHelper {

        public static void InitModStats(Mech mech) {
            // Initialize mod-specific statistics
            mech.StatCollection.AddStatistic<int>(ModStats.TurnsOverheated, 0);
            mech.StatCollection.AddStatistic<int>(ModStats.MovementPenalty, 0);
            mech.StatCollection.AddStatistic<int>(ModStats.FiringPenalty, 0);

            // Override the heat and shutdown levels
            List<int> sortedKeys = Mod.Config.Heat.Shutdown.Keys.ToList().OrderBy(x => x).ToList();
            int overheatThreshold = sortedKeys.First();
            int maxHeat = sortedKeys.Last();
            Mod.Log.Info($"Setting overheat threshold to {overheatThreshold} and maxHeat to {maxHeat} for actor:{CombatantUtils.Label(mech)}");
            mech.StatCollection.Set<int>(ModStats.MaxHeat, maxHeat);
            mech.StatCollection.Set<int>(ModStats.OverHeatLevel, overheatThreshold);

            mech.StatCollection.Set<float>(ModStats.RunMultiMod, 0f);

            // Disable default heat penalties
            mech.StatCollection.Set<bool>("IgnoreHeatToHitPenalties", false);
            mech.StatCollection.Set<bool>("IgnoreHeatMovementPenalties", false);
        }

        public static float CalcWalkSpeed(Mech mech) {
            Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has walk: {mech.WalkSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged) {
                Mod.Log.Debug($"  Mech is legged, returning minimum move distance: {Mod.Config.Move.MinimumMove}");
                return Mod.Config.Move.MinimumMove;
            }

            // Check for overheat penalties
            float moveMod = 0f;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement) {
                if (mech.CurrentHeat >= kvp.Key) {
                    moveMod = kvp.Value * 30.0f;
                    Mod.Log.Debug($" Move penalty = {moveMod}m as currentHeat: {mech.CurrentHeat} >= bounds: {kvp.Key}");
                }
            }

            float walkDistance = mech.WalkSpeed;
            if (moveMod != 0f) {
                walkDistance = mech.WalkSpeed + moveMod;
                Mod.Log.Debug($"  Walk speed: {mech.WalkSpeed}m + modifier: {moveMod} = {walkDistance}m");
            }

            // Normalize to the minimum if somehow we're below that.
            if (walkDistance < Mod.Config.Move.MinimumMove) { walkDistance = Mod.Config.Move.MinimumMove; }

            return walkDistance;
        }

        public static float CalcRunSpeed(Mech mech) {
            Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has run: {mech.RunSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged) {
                Mod.Log.Debug($"  Mech is legged, returning minimum move distance: {Mod.Config.Move.MinimumMove}");
                return Mod.Config.Move.MinimumMove;
            }

            float runMulti = Mod.Config.Move.RunMulti;
            if (mech.StatCollection.ContainsStatistic(ModStats.RunMultiMod)) {
                float runMultiMod = mech.StatCollection.GetStatistic(ModStats.RunMultiMod).Value<float>();
                Mod.Log.Debug($"  Modifying base run multiplier {runMulti} + {runMultiMod}");
                runMulti += runMultiMod;
            }
            
            float walkSpeed = CalcWalkSpeed(mech);
            float runSpeed = walkSpeed * runMulti;
            Mod.Log.Debug($"  Run speed {runSpeed}m = walkSpeed: {walkSpeed}m x runMulti: {runMulti}");

            return runSpeed;
        }
    }
}

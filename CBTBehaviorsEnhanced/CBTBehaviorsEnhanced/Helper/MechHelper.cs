using BattleTech;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced {
    public class MechHelper {

        public static float CalcWalkSpeed(Mech mech) {
            //Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has walk: {mech.WalkSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

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
                    //Mod.Log.Debug($" Move penalty = {moveMod}m as currentHeat: {mech.CurrentHeat} >= bounds: {kvp.Key}");
                }
            }

            float walkDistance = mech.WalkSpeed;
            if (moveMod != 0f) {
                walkDistance = mech.WalkSpeed + moveMod;
                //Mod.Log.Debug($"  Walk speed: {mech.WalkSpeed}m + modifier: {moveMod} = {walkDistance}m");
            }

            // Normalize to the minimum if somehow we're below that.
            if (walkDistance < Mod.Config.Move.MinimumMove) { walkDistance = Mod.Config.Move.MinimumMove; }

            return walkDistance;
        }

        public static float CalcRunSpeed(Mech mech) {
            //Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has run: {mech.RunSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

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
            //Mod.Log.Debug($"  Run speed {runSpeed}m = walkSpeed: {walkSpeed}m x runMulti: {runMulti}");

            return runSpeed;
        }

        // Create a falling sequence, publish a floatie with the error
        public static void AddFallingSequence(Mech mech, MultiSequence parentSequence, string floatieText) {

            string fallDebuffText = new Text(Mod.Config.Floaties[floatieText]).ToString();
            MultiSequence sequence = new ShowActorInfoSequence(mech, fallDebuffText, FloatieMessage.MessageNature.Debuff, true);

            MechFallSequence mfs = new MechFallSequence(mech, floatieText, new Vector2(0f, -1f)) {
                RootSequenceGUID = parentSequence.SequenceGUID
            };
            sequence.AddChildSequence(mfs, 0);

            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
        }

    }
}

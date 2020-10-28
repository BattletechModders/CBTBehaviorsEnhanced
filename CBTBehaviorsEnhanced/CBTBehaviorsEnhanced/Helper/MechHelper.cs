using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using IRBTModUtils.Extension;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced {
    public class MechHelper {

        // Rationalize walk distance into MP, to allow CBT calculations to occur.
        public static int CalcWalkMP(Mech mech)
        {
            // Per Battletech Manual, Running MP is always rounded up. Follow that principle here as well.
            int rawWalkMP = (int)Math.Ceiling(mech.WalkSpeed / Mod.Config.Move.MPMetersPerHex);
            Mod.Log.Trace?.Write($"Raw walkMP = {mech.WalkSpeed} / {Mod.Config.Move.MPMetersPerHex} = {rawWalkMP}");

            // Check for overheat penalties
            int movePenalty = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement)
            {
                if (mech.CurrentHeat >= kvp.Key)
                {
                    movePenalty = kvp.Value;
                    //Mod.Log.Debug?.Write($" Move penalty = {moveMod}m as currentHeat: {mech.CurrentHeat} >= bounds: {kvp.Key}");
                }
            }
            int modifiedWalkMP = rawWalkMP + movePenalty;
            Mod.Log.Trace?.Write($"Modified walkMP = {rawWalkMP} - {movePenalty} = {modifiedWalkMP}");

            // Normalize to the minimum if somehow we're below that.
            return modifiedWalkMP >= 1 ? modifiedWalkMP : 1;
        }

        // Use the default walkSpeed, but don't rationalize to MP
        public static float CalcWalkDist(Mech mech)
        {
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
            float modifiedWalkSpeed = mech.WalkSpeed + movePenaltyDist;
            Mod.Log.Trace?.Write($"Modified mechs walk speed from {mech.WalkSpeed} by {movePenalty} x {Mod.Config.Move.HeatMovePenalty} = {modifiedWalkSpeed}");

            // Normalize to the minimum if somehow we're below that.
            return modifiedWalkSpeed < Mod.Config.Move.MinimumMove ? Mod.Config.Move.MinimumMove : modifiedWalkSpeed;
        }

        public static float FinalWalkSpeed(Mech mech) {
            //Mod.Log.Debug?.Write($"Actor:{CombatantUtils.Label(mech)} has walk: {mech.WalkSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            if (Mod.Config.Features.SpeedAsMP)
            {
                // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
                if (mech.IsLegged)
                {
                    Mod.Log.Debug?.Write($"  Mech is legged, returning 1MP.");
                    return 1f * Mod.Config.Move.MPMetersPerHex;
                }

                int walkMP = CalcWalkMP(mech);
                return walkMP * Mod.Config.Move.MPMetersPerHex;
            }
            else
            {
                if (mech.IsLegged)
                {
                    Mod.Log.Debug?.Write($"  Mech is legged, returning {Mod.Config.Move.MinimumMove}m");
                    return Mod.Config.Move.MinimumMove;
                }

                return CalcWalkDist(mech);
            }

        }

        public static float FinalRunSpeed(Mech mech) {
            //Mod.Log.Debug?.Write($"Actor:{CombatantUtils.Label(mech)} has run: {mech.RunSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            float runMulti = Mod.Config.Move.RunMulti;
            if (mech.StatCollection.ContainsStatistic(ModStats.RunMultiMod))
            {
                float runMultiMod = mech.StatCollection.GetStatistic(ModStats.RunMultiMod).Value<float>();
                runMulti += runMultiMod;
            }
            Mod.Log.Trace?.Write($" Using a final run multiplier of x{runMulti} (from base: {Mod.Config.Move.RunMulti})");

            if (Mod.Config.Features.SpeedAsMP)
            {
                // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
                if (mech.IsLegged)
                {
                    Mod.Log.Debug?.Write($"  Mech is legged, returning 1MP.");
                    return 1f * Mod.Config.Move.MPMetersPerHex;
                }

                int walkMP = CalcWalkMP(mech);
                // Per Battletech Manual, Running MP is always rounded up. Follow that principle here as well.
                int runMP = (int)Math.Ceiling(walkMP * runMulti);
                Mod.Log.Trace?.Write($" RunMP of {runMP} from walkMP: {walkMP} x runMulti: {runMulti}");

                return runMP * Mod.Config.Move.MPMetersPerHex;
            }
            else
            {
                // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
                if (mech.IsLegged)
                {
                    Mod.Log.Debug?.Write($"  Mech is legged, returning {Mod.Config.Move.MinimumMove}m");
                    return Mod.Config.Move.MinimumMove;
                }

                float walkSpeed = CalcWalkDist(mech);
                // Per Battletech Manual, Running MP is always rounded up. Follow that principle here as well.
                float runSpeed = (float) Math.Ceiling(walkSpeed * runMulti);
                Mod.Log.Trace?.Write($" RunSpeed of {runSpeed} from walkSpeed: {walkSpeed} x runMulti: {runMulti}");

                return runSpeed;
            }
        }

        // Create a falling sequence, publish a floatie with the error
        public static void AddFallingSequence(Mech mech, MultiSequence parentSequence, string floatieText) {

            Mod.Log.Info?.Write($"Adding falling sequence for mech: {mech.DistinctId()}");

            MechFallSequence mechFallSequence = new MechFallSequence(mech, floatieText, new Vector2(0f, -1f));

            string fallDebuffText = new Text(Mod.LocalizedText.Floaties[floatieText]).ToString();
            MultiSequence showInfoSequence = new ShowActorInfoSequence(mech, fallDebuffText, FloatieMessage.MessageNature.Debuff, false) {
                RootSequenceGUID = mechFallSequence.SequenceGUID
            };
            mechFallSequence.AddChildSequence(showInfoSequence, mechFallSequence.MessageIndex);
            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(mechFallSequence));
            Mod.Log.Info?.Write(" -- published fall sequence.");

            IStackSequence doneWithActorSequence = mech.DoneWithActor();
            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(doneWithActorSequence));
            Mod.Log.Info?.Write(" -- published doneWithActor sequence.");

        }

        public static void PilotCheckOnInstabilityDamage(Mech target, float stabilityDamage) {
            // Do nothing in these cases
            if (target.IsDead || target.IsOrWillBeProne || !target.IsUnsteady) {
                Mod.Log.Debug?.Write($"Target: {CombatantUtils.Label(target)} is dead, will be prone, or has no stability damage. Skipping.");
                return;
            }

            float pilotCheck = target.PilotCheckMod(Mod.Config.Piloting.SkillMulti);
            bool didCheckPass = CheckHelper.DidCheckPassThreshold(Mod.Config.Piloting.StabilityCheck, target, pilotCheck, ModText.FT_Check_Fall);
            if (!didCheckPass) {
                Mod.Log.Debug?.Write($"Actor: {CombatantUtils.Label(target)} failed a knockdown check due to instability, starting fall sequence.");
                target.FlagForKnockdown();

                string fallDebuffText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Check_Fall]).ToString();
                target.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(target, fallDebuffText, FloatieMessage.MessageNature.Debuff, true)
                    ));
            }

        }

    }
}

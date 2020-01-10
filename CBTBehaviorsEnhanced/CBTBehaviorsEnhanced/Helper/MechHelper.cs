using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Localize;
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
                runMulti += runMultiMod;
            }
            
            float walkSpeed = CalcWalkSpeed(mech);
            float runSpeed = walkSpeed * runMulti;
            //Mod.Log.Debug($"  Run speed {runSpeed}m = walkSpeed: {walkSpeed}m x runMulti: {runMulti}");

            return runSpeed;
        }

        // Create a falling sequence, publish a floatie with the error
        public static void AddFallingSequence(Mech mech, MultiSequence parentSequence, string floatieText) {

            MechFallSequence mechFallSequence = new MechFallSequence(mech, floatieText, new Vector2(0f, -1f));

            string fallDebuffText = new Text(Mod.Config.LocalizedFloaties[floatieText]).ToString();
            MultiSequence showInfoSequence = new ShowActorInfoSequence(mech, fallDebuffText, FloatieMessage.MessageNature.Debuff, false) {
                RootSequenceGUID = mechFallSequence.SequenceGUID
            };
            mechFallSequence.AddChildSequence(showInfoSequence, mechFallSequence.MessageIndex);

            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(mechFallSequence));
        }

        public static void PilotCheckOnInstabilityDamage(Mech target, float stabilityDamage) {
            // Do nothing in these cases
            if (target.IsDead || target.IsOrWillBeProne || !target.IsUnsteady) {
                Mod.Log.Debug($"Target: {CombatantUtils.Label(target)} is dead, will be prone, or has no stability damage. Skipping.");
                return;
            }

            float pilotCheck = target.PilotCheckMod(Mod.Config.Piloting.SkillMulti);
            bool didCheckPass = CheckHelper.DidCheckPassThreshold(Mod.Config.Piloting.StabilityCheck, target, pilotCheck, ModConfig.FT_Check_Fall);
            if (!didCheckPass) {
                Mod.Log.Debug($"Actor: {CombatantUtils.Label(target)} failed a knockdown check due to instability, starting fall sequence.");
                target.FlagForKnockdown();

                string fallDebuffText = new Text(Mod.Config.LocalizedFloaties[ModConfig.FT_Check_Fall]).ToString();
                target.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(target, fallDebuffText, FloatieMessage.MessageNature.Debuff, true)
                    ));
            }

        }

    }
}

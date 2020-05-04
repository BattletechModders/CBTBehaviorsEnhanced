﻿using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
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
            int rawWalkMP = (int)Math.Ceiling(mech.WalkSpeed / Mod.Config.Move.MetersPerHex);
            Mod.Log.Trace($"Raw walkMP = {mech.WalkSpeed} / {Mod.Config.Move.MetersPerHex} = {rawWalkMP}");

            // Check for overheat penalties
            int movePenalty = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Movement)
            {
                if (mech.CurrentHeat >= kvp.Key)
                {
                    movePenalty = kvp.Value;
                    //Mod.Log.Debug($" Move penalty = {moveMod}m as currentHeat: {mech.CurrentHeat} >= bounds: {kvp.Key}");
                }
            }
            int modifiedWalkMP = rawWalkMP - movePenalty;
            Mod.Log.Trace($"Modified walkMP = {rawWalkMP} - {movePenalty} = {modifiedWalkMP}");

            // Normalize to the minimum if somehow we're below that.
            return modifiedWalkMP >= 1 ? modifiedWalkMP : 1;
        }


        public static float CalcWalkSpeed(Mech mech) {
            //Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has walk: {mech.WalkSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged)
            {
                Mod.Log.Debug($"  Mech is legged, returning 1MP.");
                return 1f * Mod.Config.Move.MetersPerHex;
            }

            int walkMP = CalcWalkMP(mech);
            return walkMP * Mod.Config.Move.MetersPerHex;
        }

        public static float CalcRunSpeed(Mech mech) {
            //Mod.Log.Debug($"Actor:{CombatantUtils.Label(mech)} has run: {mech.RunSpeed}  isLegged: {mech.IsLegged}  heat: {mech.CurrentHeat}");

            // By TT rules, a legged mech has a single MP. Return the minimum, which should allow 1 hex of movement.
            if (mech.IsLegged) {
                Mod.Log.Debug($"  Mech is legged, returning 1MP.");
                return 1f * Mod.Config.Move.MetersPerHex;
            }

            float runMulti = Mod.Config.Move.RunMulti;
            if (mech.StatCollection.ContainsStatistic(ModStats.RunMultiMod)) {
                float runMultiMod = mech.StatCollection.GetStatistic(ModStats.RunMultiMod).Value<float>();
                runMulti += runMultiMod;
            }
            Mod.Log.Trace($" Using a final run multiplier of x{runMulti} (from base: {Mod.Config.Move.RunMulti})");

            int walkMP = CalcWalkMP(mech);
            // Per Battletech Manual, Running MP is always rounded up. Follow that principle here as well.
            int runMP = (int)Math.Ceiling(walkMP * runMulti);
            Mod.Log.Debug($" RunMP of {runMP} from walkMP: {walkMP} x runMulti: {runMulti}");

            return runMP * Mod.Config.Move.MetersPerHex;
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

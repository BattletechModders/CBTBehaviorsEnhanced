using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using IRBTModUtils.Extension;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced
{
    public class MechHelper
    {

        // Create a falling sequence, publish a floatie with the error
        public static void AddFallingSequence(Mech mech, MultiSequence parentSequence, string floatieText)
        {

            Mod.Log.Info?.Write($"Adding falling sequence for mech: {mech.DistinctId()}");

            MechFallSequence mechFallSequence = new MechFallSequence(mech, floatieText, new Vector2(0f, -1f));

            string fallDebuffText = new Text(Mod.LocalizedText.Floaties[floatieText]).ToString();
            MultiSequence showInfoSequence = new ShowActorInfoSequence(mech, fallDebuffText, FloatieMessage.MessageNature.Debuff, false)
            {
                RootSequenceGUID = mechFallSequence.SequenceGUID
            };
            mechFallSequence.AddChildSequence(showInfoSequence, mechFallSequence.MessageIndex);
            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(mechFallSequence));
            Mod.Log.Info?.Write(" -- published fall sequence.");

            IStackSequence doneWithActorSequence = mech.DoneWithActor();
            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(doneWithActorSequence));
            Mod.Log.Info?.Write(" -- published doneWithActor sequence.");

        }

        public static void PilotCheckOnInstabilityDamage(Mech target, float stabilityDamage)
        {
            // Do nothing in these cases
            if (target.IsDead || target.IsOrWillBeProne || !target.IsUnsteady)
            {
                Mod.Log.Debug?.Write($"Target: {CombatantUtils.Label(target)} is dead, will be prone, or has no stability damage. Skipping.");
                return;
            }

            float pilotCheck = target.PilotCheckMod(Mod.Config.SkillChecks.ModPerPointOfPiloting);
            bool didCheckPass = CheckHelper.DidCheckPassThreshold(Mod.Config.Piloting.StabilityCheck, target, pilotCheck, ModText.FT_Check_Fall);
            if (!didCheckPass)
            {
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

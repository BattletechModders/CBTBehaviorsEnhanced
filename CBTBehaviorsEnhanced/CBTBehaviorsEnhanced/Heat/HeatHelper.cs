
using BattleTech;
using Localize;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviors {

    public static class HeatHelper {

        public static bool DidCheckPassThreshold(SortedDictionary<int, float> dict, Mech mech, float skillMod, string floatieText) {
            float checkTarget = 0f;
            foreach (KeyValuePair<int, float> kvp in dict) {
                if (mech.CurrentHeat >= kvp.Key) {
                    checkTarget = kvp.Value;
                }
            }
            Mod.Log.Debug($"  target roll set to: {checkTarget}");
            return PassedCheck(checkTarget, mech, skillMod, floatieText);
        }
        public static bool DidCheckPassThreshold(float checkTarget, Mech mech, float skillMod, string floatieText) {
            return PassedCheck(checkTarget, mech, skillMod, floatieText);
        }

        private static bool PassedCheck(float checkTarget, Mech mech, float skillMod, string floatieText) {
            if (checkTarget <= 0f) { return true; }

            float randomRoll = mech.Combat.NetworkRandom.Float();
            float checkResult = randomRoll + skillMod;
            Mod.Log.Debug($"  pilotMod: {skillMod} + roll: {randomRoll} = checkResult: {checkResult}");

            string operatorText = "=";
            if (checkResult > checkTarget) { operatorText = ">"; } else if (checkResult < checkTarget) { operatorText = "<"; }

            mech.Combat.MessageCenter.PublishMessage(
                new FloatieMessage(mech.GUID, mech.GUID, $"{floatieText.ToString()} {randomRoll} + {skillMod } {new Text(operatorText).ToString()} {checkTarget}", FloatieMessage.MessageNature.Neutral)
                );

            return checkTarget != -1f && checkResult < checkTarget;
        }

        public static float GetPilotingMulti(AbstractActor actor) { return GetSkillMulti(actor, false); }
        public static float GetGutsMulti(AbstractActor actor) { return GetSkillMulti(actor, false);  }
        private static float GetSkillMulti(AbstractActor actor, bool gutsSkill) {
            float skillMulti = 0f;
            if (actor != null && actor.GetPilot() != null) {
                int actorSkill = SkillUtils.NormalizeSkill(gutsSkill ? actor.GetPilot().Guts : actor.GetPilot().Piloting);
                skillMulti = actorSkill * Mod.Config.Heat.PilotSkillMulti;
                Mod.Log.Debug($"Actor: {CombatantUtils.Label(actor)} has normalized guts: {actorSkill} adjusted to skillMulti: {skillMulti}");
            } else {
                Mod.Log.Info($"WARNING: Actor {actor.DisplayName} has no pilot!");
            }
            return skillMulti;
        }

        public class CBTPilotingRules {
            private readonly CombatGameState combat;

            public CBTPilotingRules(CombatGameState combat) {
                this.combat = combat;
            }

            public float GetGutsModifier(AbstractActor actor) {
                Pilot pilot = actor.GetPilot();

                float num = (pilot != null) ? ((float)pilot.Guts) : 1f;
                float gutsDivisor = Mod.Config.GutsDivisor;
                return num / gutsDivisor;
            }
        }

    }
}

using BattleTech;
using Localize;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    public class CheckHelper {
        public static bool DidCheckPassThreshold(SortedDictionary<int, float> dict, int heatValue, Mech mech, float skillMod, string floatieText) {
            float checkTarget = 0f;
            foreach (KeyValuePair<int, float> kvp in dict) {
                if (heatValue >= kvp.Key) {
                    checkTarget = kvp.Value;
                }
            }
            Mod.Log.Debug($"  target roll set to: {checkTarget} for heat: {heatValue}");
            return PassedCheck(checkTarget, mech, skillMod, floatieText);
        }
        public static bool DidCheckPassThreshold(float checkTarget, Mech mech, float skillMod, string floatieText) {
            return PassedCheck(checkTarget, mech, skillMod, floatieText);
        }

        private static bool PassedCheck(float checkTarget, Mech mech, float skillMod, string floatieText) {
            // If the threshold is -1, you auto-fail
            if (checkTarget == -1f) {
                mech.Combat.MessageCenter.PublishMessage(
                    new FloatieMessage(mech.GUID, mech.GUID,
                    $"{new Text(floatieText).ToString()} {new Text(Mod.Config.LocalizedFloaties[ModConfig.FT_Auto_Fail]).ToString()}", 
                    FloatieMessage.MessageNature.Neutral)
                    );
                return false;
            }
            // If there's no threshold, you auto-pass
            if (checkTarget <= 0f) { return true; }

            float randomRoll = mech.Combat.NetworkRandom.Float();
            float checkResult = randomRoll + skillMod;
            Mod.Log.Debug($"  pilotMod: {skillMod:#.##} + roll: {randomRoll:#.##} = checkResult: {checkResult:#.##} vs. checkTarget: {checkTarget:#.##}");

            string operatorText = "=";
            if (checkResult > checkTarget) { operatorText = ">"; } else if (checkResult < checkTarget) { operatorText = "<"; }

            bool passedCheck = checkTarget != -1f && checkResult >= checkTarget;
            if (!passedCheck) {
                mech.Combat.MessageCenter.PublishMessage(
                    new FloatieMessage(mech.GUID, mech.GUID,
                        $"{new Text(floatieText).ToString()} {checkResult:P1} {operatorText} {checkTarget:P1}",
                        FloatieMessage.MessageNature.Neutral)
                    );
            }

            return passedCheck;
        }
    }
}

using BattleTech;
using System;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Extensions {
    public static class MechExtensions {
        public static float PilotCheckMod(this Mech mech, float multi) {
            return CalculateCheckMod(mech, multi, false);
        }

        public static float HeatCheckMod(this Mech mech, float multi) {
            return CalculateCheckMod(mech, multi, true);
        }

        private static float CalculateCheckMod(Mech mech, float multi, bool gutsSkill) {

            int rawSkill = gutsSkill ? mech.SkillGuts : mech.SkillPiloting;
            int actorSkill = SkillUtils.NormalizeSkill(rawSkill);
            Mod.Log.Debug($"Actor: {CombatantUtils.Label(mech)} has rawSkill: {actorSkill} normalized to {actorSkill}");

            int malus = 0;
            if (mech.StatCollection != null && mech.StatCollection.ContainsStatistic(ModStats.PilotingMalus)) {
                malus = mech.StatCollection.GetStatistic(ModStats.PilotingMalus).Value<int>();
            }

            int adjustedSkill = Math.Max(0, (actorSkill - malus)/100);
            Mod.Log.Debug($"  AdjustedSkill: {adjustedSkill} = actorSkill: {actorSkill} - malus: {malus}.");

            float checkMod = adjustedSkill;
            Mod.Log.Debug($"  CheckMod: {checkMod} = adjustedSkill: {adjustedSkill} * multi: {multi}");
            return checkMod;
        }

    }

}

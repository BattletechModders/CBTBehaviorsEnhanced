
using BattleTech;
using us.frostraptor.modUtils;

namespace CBTBehaviors {

    public static class HeatHelper {

        public static float GetShutdownPercentageForTurn(int turn) {
            int count = Mod.Config.ShutdownPercentages.Length;

            if (turn <= 0) {
                return Mod.Config.ShutdownPercentages[0];
            }

            if (turn > count - 1) {
                turn = count - 1;
            }

            return Mod.Config.ShutdownPercentages[turn];
        }

        public static float GetAmmoExplosionPercentageForTurn(int turn) {
            int count = Mod.Config.AmmoExplosionPercentages.Length;

            if (turn <= 0) {
                return Mod.Config.AmmoExplosionPercentages[0];
            }

            if (turn > count - 1) {
                turn = count - 1;
            }

            return Mod.Config.AmmoExplosionPercentages[turn];
        }

        public static float GetOverheatedMovePenaltyForTurn(int turn) {
            int count = Mod.Config.OverheatedMovePenalty.Length;

            if (turn <= 0) {
                return (float)Mod.Config.OverheatedMovePenalty[0];
            }

            if (turn > count) {
                turn = count;
            }

            return Mod.Config.OverheatedMovePenalty[turn - 1];
        }

        public static float GetHeatToHitModifierForTurn(int turn) {
            int count = Mod.Config.HeatToHitModifiers.Length;

            if (turn <= 0) {
                return (float)Mod.Config.HeatToHitModifiers[0];
            }

            if (turn > count) {
                turn = count;
            }

            return (float)Mod.Config.HeatToHitModifiers[turn - 1];
        }

        public static bool CanAmmoExplode(Mech mech) {
            if (mech.ammoBoxes.Count == 0) {
                return false;
            }

            int ammoCount = 0;

            foreach (var ammoBox in mech.ammoBoxes) {
                ammoCount += ammoBox.CurrentAmmo;
            }

            if (ammoCount > 0) {
                return true;
            }

            return false;
        }

        public static float GetPilotMod(AbstractActor actor) {
            float shutdownMod = 0f;
            if (actor != null && actor.GetPilot() != null) {
                int actorSkill = SkillUtils.NormalizeSkill(actor.GetPilot().Piloting);
                shutdownMod = actorSkill * Mod.Config.Heat.PilotSkillMulti;
                Mod.Log.Debug($"Actor: {CombatantUtils.Label(actor)} has normalizedSkill: {actorSkill} with shutdownMod: {shutdownMod}");
            } else {
                Mod.Log.Info($"WARNING: Actor {actor.DisplayName} has no pilot!");
            }
            return shutdownMod;
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

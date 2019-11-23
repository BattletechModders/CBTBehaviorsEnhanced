
using BattleTech;

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

        public static float GetPilotShutdownMod(AbstractActor actor) {
            float shutdownMod = 0f;
            if (actor != null && actor.GetPilot() != null) {
                int actorSkill = NormalizeSkill(actor.GetPilot().Piloting);
                shutdownMod = actorSkill * Mod.Config.Heat.PilotSkillShutdownMulti;
                Mod.Log.Debug($"Actor: {actor.DisplayName}_{actor.GetPilot().Name} has normalizedSkill: {actorSkill} with shutdownMod: {shutdownMod}");
            } else {
                Mod.Log.Info($"WARNING: Actor {actor.DisplayName} has no pilot!");
            }
            return shutdownMod;
        }

        // Reduce RT elite pilots with skills > 10 to 11-13 to reduce their skill impact on specific checks
        private static int NormalizeSkill(int rawValue) {
            int normalizedVal = rawValue;
            if (rawValue >= 11 && rawValue <= 14) {
                normalizedVal = 11;
            } else if (rawValue >= 15 && rawValue <= 18) {
                normalizedVal = 12;
            } else if (rawValue == 19 || rawValue == 20) {
                normalizedVal = 13;
            } else if (rawValue <= 0) {
                normalizedVal = 1;
            } else if (rawValue > 20) {
                normalizedVal = 13;
            }
            return normalizedVal;
        }

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

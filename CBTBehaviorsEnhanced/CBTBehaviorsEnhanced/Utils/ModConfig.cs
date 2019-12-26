

using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    
    public class ModStats {
        public const string TurnsOverheated = "TurnsOverheated";
        public const string CanShootAfterSprinting = "CanShootAfterSprinting";
        public const string MeleeHitPushBackPhases = "MeleeHitPushBackPhases";

        public const string MovementPenalty = "CBTBE_MovePenalty";
        public const string FiringPenalty = "CBTBE_FirePenalty";

        // Modifies the base 1.5 multiplier for run from walk speed
        public const string RunMultiMod = "CBTBE_RunMultiMod";
        public const string OverHeatLevel = "OverheatLevel";
        public const string MaxHeat = "MaxHeat";

        // Reduces piloting effects one for one
        public const string PilotingMalus = "CBTBE_Piloting_Malus";
    }

    public class ModConfig {

        public bool Debug = true;
        public bool Trace = false;

        // Heat
        public float[] AmmoExplosionPercentages = new float[] { 0f, 0.083f, 0.278f, 0.583f };
        public bool UseGuts = false;
        public int GutsDivisor = 40;

        // 4+ => 91.66%, 6+ => 72.22%, 8+ => 41.67%, 10+ => 16.67%, 12+ => 2.78%

        // https://github.com/Bohica/BattletechCombatMachine/wiki/HEAT or Tactical Operations pg. 105
        public class HeatOptions {
            // 5:-1, 10:-2, 15:-3, 20:-4, 25:-5, 31:-6, 37:-7, 43:-8, 49:-9
            public SortedDictionary<int, int> Movement = new SortedDictionary<int, int> {
                { 15, -1 }, { 30, -2 }, { 45, -3 }, { 60, -4 }, { 75, -5 },
                { 93, -6 }, { 111, -7 }, { 129, -8 }, { 147, -9 }
            };

            // 8:-1, 13:-2, 17:-3, 24:-4, 33:-5, 41:-6, 48:-7
            public SortedDictionary<int, int> Firing = new SortedDictionary<int, int> {
                { 24, 1 }, { 39 , 2 }, { 51, 3 }, { 72, 4 }, { 99, 5 },
                { 123, 6 }, { 144, 7 }
            };

            // 14:4+, 18:6+, 22:8+, 26:10+, 30:12+, 34:14+, 38:16+, 42:18+, 46:20+, 50:INFINITY
            // If shutdown, needing piloting skill roll or fall over; roll has a +3 modifier
            public SortedDictionary<int, float> Shutdown = new SortedDictionary<int, float> {
                { 42, 0.1f }, { 54, 0.3f }, { 66, 0.6f}, { 78, 0.8f }, { 90, 0.9f },
                { 102, 1.0f }, { 114, 1.1f }, { 126, 1.2f }, { 138, 1.3f }, { 150, -1f }
            };

            // 19:4+, 23:6+, 28:8+, 35:10+, 40:12+, 45:INFINITY
            // Explosion should impact most damaging ammo first
            // Inferno weapons require a 2nd roll in addition to the first 
            // Any ammo explosion automatically causes 2 points of pilot damage and forces a conciousness roll
            public SortedDictionary<int, float> Explosion = new SortedDictionary<int, float> {
                {  57, 0.1f },
                {  69, 0.3f },
                {  84, 0.5f },
                { 105, 0.8f },
                { 120, 0.95f },
                { 135, -1f },
            };

            // 32:8+, 39:10+, 47:12+
            // If life support damaged, can't be avoided and is in addition to normal damage
            public SortedDictionary<int, float> PilotInjury = new SortedDictionary<int, float> {
                { 84, 0.3f }, { 117, 0.6f}, { 141, 0.8f }
            };

            // 36:8+, 44:10+
            // If roll fails, roll a hit location on the front column of mech critical hit table and apply single critical hit to location
            public SortedDictionary<int, float> SystemFailures = new SortedDictionary<int, float> {
                { 108, 0.3f }, { 132, 0.6f},
            };

            // 1:0.05, 2:0.1, 3:0.15, 4:0.2, 5:0.25, 6:0.3, 7:0.35, 8:0.4, 9:0.45, 10:0.5
            public int ShowLowOverheatAnim = 42; // When to show as steaming
            public int ShowExtremeOverheatAnim = 90; // When to show as glowing hot
            public float ShutdownFallThreshold = 0.75f;
        }
        public HeatOptions Heat = new HeatOptions();

        // 4+ => 91.66%, 6+ => 72.22%, 8+ => 41.67%, 10+ => 16.67%, 12+ => 2.78%
        public class PilotingOptions {
            public float SkillMulti = 0.05f;
            public float StabilityCheck = 0.30f;
            public float DFAReductionMulti = 0.05f;
            public bool ShowAllStabilityRolls = false;

            public float StandAttemptFallChance = 0.30f;

            // How many damage points 
            public int FallingDamagePerTenTons = 5;
        }
        public PilotingOptions Piloting = new PilotingOptions();

        // 4+ => 91.66%, 6+ => 72.22%, 8+ => 41.67%, 10+ => 16.67%, 12+ => 2.78%
        public class MeleeOptions {
            public float SkillMulti = 0.05f;

            public float MadeChargeFallChance = 0.60f;
            public float MadeDFAFallChance = 0.90f;
            public float MissedKickFallChance = 0.30f;

            public float HitByChargeFallChance = 0.60f;
            public float HitByDFAFallChance = 0.60f;
            public float HitByKickFallChance = 0.30f;

            // TODO: Is this even necessary? Just make DFA a high value. public bool ForceFallOnFailedDFA = true;
        }
        public MeleeOptions Melee = new MeleeOptions();

        public class MoveOptions {
            // The minimum amount a unit should always be allowed to take. This shouldn't go below 45m, as otherwise the unit won't be able to move at all.
            // 45 is used because while a hex is 30m, design masks can increase/decrease this by some amount. Most don't go beyond 1.2/0.8, so 40 is a number
            //  that should guarantee one move on all terrain types
            public float MinimumMove = 40.0f;

            // When calculating RunSpeed, multiply the current WalkSpeed by this amount. 
            public float RunMulti = 1.5f;
        }
        public MoveOptions Move = new MoveOptions();

        // Piloting

        // Movement
        public int ToHitSelfJumped = 2;

        public const string FT_Shutdown_Override = "SHUTDOWN_OVERRIDE_SUCCESS";
        public const string FT_Shutdown_Failed_Overide = "SHUTDOWN_OVERRIDE_FAILED";
        public const string FT_Shutdown_Fall = "SHUTDOWN_FALL";
        public const string FT_Check_Explosion = "EXPLOSION_CHECK";
        public const string FT_Check_Shutdown = "SHUTDOWN_CHECK";
        public const string FT_Check_Injury = "INJURY_CHECK";
        public const string FT_Check_System_Failure = "SYSTEM_FAILURE_CHECK";
        public const string FT_Check_Fall = "FALLING_CHECK";
        public const string FT_Melee_Kick = "MELEE_KICK";
        public const string FT_Melee_Charge = "MELEE_CHARGE";
        public const string FT_Melee_DFA = "MELEE_DFA";
        public Dictionary<string, string> Floaties = new Dictionary<string, string> {
            { FT_Shutdown_Override, "Passed Shutdown Override" },
            { FT_Shutdown_Failed_Overide, "Failed Shutdown Override" },
            { FT_Shutdown_Fall, "Falling from Shutdown" },
            { FT_Check_Explosion, "Ammo Explosion Check" },
            { FT_Check_Shutdown, "Shutdown Check" },
            { FT_Check_Injury, "Pilot Injury Check" },
            { FT_Check_System_Failure, "System Failure Check" },
            { FT_Melee_Kick, "Kick Falling Check" },
            { FT_Melee_Charge, "Charge Falling Check" },
            { FT_Melee_DFA, "DFA Falling Check" }
        };

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info("=== MOVEMENT OPTIONS ===");

            Mod.Log.Info("=== HEAT OPTIONS ===");

            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}

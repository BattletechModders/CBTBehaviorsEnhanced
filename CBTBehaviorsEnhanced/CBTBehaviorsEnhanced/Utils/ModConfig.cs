﻿

using System.Collections.Generic;

namespace CBTBehaviors {
    
    public class ModStats {
        public const string TurnsOverheated = "TurnsOverheated";
        public const string CanShootAfterSprinting = "CanShootAfterSprinting";
        public const string MeleeHitPushBackPhases = "MeleeHitPushBackPhases";

        public const string MovementPenalty = "CBTB_MovePenalty";
        public const string FiringPenalty = "CBTB_FirePenalty";
    }

    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;

        // Heat
        public float[] ShutdownPercentages = new float[] { 0.083f, 0.278f, 0.583f, 0.833f };
        public float[] AmmoExplosionPercentages = new float[] { 0f, 0.083f, 0.278f, 0.583f };
        public int[] HeatToHitModifiers = new int[] { 1, 2, 3, 4 };
        public bool UseGuts = false;
        public int GutsDivisor = 40;
        public float[] OverheatedMovePenalty = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };

        // 4+   ==> 91.66%
        // 6+   ==> 72.22%
        // 8+   ==> 41.67%
        // 10+  ==> 16.67%
        // 12+  ==>  2.78%

        // https://github.com/Bohica/BattletechCombatMachine/wiki/HEAT or Tactical Operations pg. 105
        public class HeatOptions {
            // 5:-1, 10:-2, 15:-3, 20:-4, 25:-5, 31:-6, 37:-7, 43:-8, 49:-9
            public Dictionary<int, int> Movement = new Dictionary<int, int> {
                { 15, -1 }, { 30, -2 }, { 45, -3 }, { 60, -4 }, { 75, -5 },
                { 93, -6 }, { 111, -7 }, { 129, -8 }, { 147, -9 }
            };

            // 8:-1, 13:-2, 17:-3, 24:-4, 33:-5, 41:-6, 48:-7
            public Dictionary<int, int> Firing = new Dictionary<int, int> {
                { 24, -1 }, { 39 , -2 }, { 51, -3 }, { 72, -4 }, { 99, -5 },
                { 123, -6 }, { 144, -7 }
            };

            // 14:4+, 18:6+, 22:8+, 26:10+, 30:12+, 34:14+, 38:16+, 42:18+, 46:20+, 50:INFINITY
            // If shutdown, needing piloting skill roll or fall over; roll has a +3 modifier
            public Dictionary<int, float> Shutdown = new Dictionary<int, float> {
                { 42, 0.1f }, { 54, 0.3f }, { 66, 0.6f}, { 78, 0.8f }, { 90, 0.9f },
                { 102, 1.0f }, { 114, 1.1f }, { 126, 1.2f }, { 138, 1.3f }, { 150, -1f }
            };
            // 1:0.05, 2:0.1, 3:0.15, 4:0.2, 5:0.25, 6:0.3, 7:0.35, 8:0.4, 9:0.45, 10:0.5
            public float PilotSkillShutdownMulti = 0.05f;

            // 19:4+, 23:6+, 28:8+, 35:10+, 40:12+, 45:INFINITY
            // Explosion should impact most damaging ammo first
            // Inferno weapons require a 2nd roll in addition to the first 
            // Any ammo explosion automatically causes 2 points of pilot damage and forces a conciousness roll
            public Dictionary<int, float> Explosion = new Dictionary<int, float> {
                {  57, 0.1f },
                {  69, 0.3f },
                {  84, 0.5f },
                { 105, 0.8f },
                { 120, 0.95f },
                { 135, -1f },
            };

            // 32:8+, 39:10+, 47:12+
            // If life support damaged, can't be avoided and is in addition to normal damage
            public Dictionary<int, int> PilotInjury = new Dictionary<int, int> { };

            // 36:8+, 44:10+
            // If roll fails, roll a hit location on the front column of mech critical hit table and apply single critical hit to location
            public Dictionary<int, float> SystemFailures = new Dictionary<int, float> { };
        }
        public HeatOptions Heat = new HeatOptions();

        // Piloting
        public float PilotStabilityCheck = 0.30f;
        public bool ShowAllStabilityRolls = false;

        // Movement
        public int ToHitSelfJumped = 2;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}

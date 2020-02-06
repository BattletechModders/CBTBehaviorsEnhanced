

using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    
    public class ModStats {
        public const string IgnoreHeatToHitPenalties = "IgnoreHeatToHitPenalties";
        public const string IgnoreHeatMovementPenalties = "IgnoreHeatMovementPenalties";
        public const string CanShootAfterSprinting = "CanShootAfterSprinting";
        public const string MeleeHitPushBackPhases = "MeleeHitPushBackPhases";
        public const string OverHeatLevel = "OverheatLevel";
        public const string MaxHeat = "MaxHeat";

        public const string MovementPenalty = "CBTBE_MovePenalty"; // int
        public const string FiringPenalty = "CBTBE_FirePenalty"; // int
        // Reduces piloting effects one for one
        public const string ActuatorDamageMalus = "CBTBE_ActuatorDamage_Malus";  // int
        // Modifies the base 1.5 multiplier for run from walk speed
        public const string RunMultiMod = "CBTBE_RunMultiMod"; // float
        public const string HullBreachImmunity = "CBTBE_HullBreachImmunity";

        // This value is set by the ME DamageIgnore feature - see https://github.com/BattletechModders/MechEngineer/blob/master/source/Features/DamageIgnore/DamageIgnoreHelper.cs
        public const string ME_IgnoreDamage = "ignore_damage";
        // This value is a vanilla value
        public const string VAN_HeatSinkCapacity = "HeatSinkCapacity";
    }

    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;
        public static bool EnablePermanentEvasion = true;

        public class FeatureList {
            public bool BiomeBreaches = true;
            public bool StartupChecks = true;
        }
        public FeatureList Features = new FeatureList();

        public class QipsConfig {
            public List<string> Breach = new List<string>() {
                "Shit, explosive decomission!",
                "Hull integrity breach detected!",
                "I've lost something to atmo!",
                "I hope life support holds up",
                "I cant breathe!",
                "There are cracks in my cockpit!"
            };

            public List<string> Knockdown = new List<string>() {
                "Oh .. shit!",
                "FML",
                "This is going to hurt..",
                "This takes the cake",
                "Not again..",
                "A bitter pill",
                "Balls!",
                "And... here we go",
                "D'oh!",
                "Shaken, not stirred",
                "Resistance is futile",
                "Well, isn't that special?",
                "I love you, too",
                "Just some armor...",
                "Biting the dust!"
            };

            public List<string> Startup = new List<string> {
                "Start damn you",
                "Can't see through this heat",
                "Where is the start button?",
                "Override damn it, override!",
                "Time to void the warranty",
                "Why won't you turn on",
                "I put in the startup sequence!"
            };
        }
        public QipsConfig Qips = new QipsConfig();

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
        public class MeleeOptions {
            public float SkillMulti = 0.05f;

            public float MadeChargeFallChance = 0.60f;
            public float MadeDFAFallChance = 0.90f;
            public float MissedKickFallChance = 0.30f;

            public float HitByChargeFallChance = 0.60f;
            public float HitByDFAFallChance = 0.60f;
            public float HitByKickFallChance = 0.30f;

            public bool AllowMeleeFromSprint = true;

            // TODO: Is this even necessary? Just make DFA a high value. public bool ForceFallOnFailedDFA = true;
        }
        public MeleeOptions Melee = new MeleeOptions();

        public class MoveOptions {
            public float SkillMulti = 0.05f;

            // The minimum amount a unit should always be allowed to take. This shouldn't go below 45m, as otherwise the unit won't be able to move at all.
            // 45 is used because while a hex is 30m, design masks can increase/decrease this by some amount. Most don't go beyond 1.2/0.8, so 40 is a number
            //  that should guarantee one move on all terrain types
            public float MinimumMove = 40.0f;

            // When calculating RunSpeed, multiply the current WalkSpeed by this amount. 
            public float RunMulti = 1.5f;

            // If you have leg damage and run, you can fall
            public float FallAfterRunChance = 0.30f;

            // If you have leg damage and jump, you can fall
            public float FallAfterJumpChance = 0.30f;
        }
        public MoveOptions Move = new MoveOptions();

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

        public class BiomeBreachOptions {
            public float VacuumCheck = 0.17f;
            public float ThinAtmoCheck = 0.03f;
        }
        public BiomeBreachOptions Breaches = new BiomeBreachOptions();

        // Movement
        public int ToHitSelfJumped = 2;

        // Floatie localization text
        public const string FT_Shutdown_Override = "SHUTDOWN_OVERRIDE_SUCCESS";
        public const string FT_Shutdown_Failed_Overide = "SHUTDOWN_OVERRIDE_FAILED";
        public const string FT_Shutdown_Fall = "SHUTDOWN_FALL";
        public const string FT_Check_Explosion = "EXPLOSION_CHECK";
        public const string FT_Check_Volatile_Explosion = "VOLATILE_EXPLOSION_CHECK";
        public const string FT_Check_Shutdown = "SHUTDOWN_CHECK";
        public const string FT_Check_Startup = "STARTUP_CHECK";
        public const string FT_Check_Injury = "INJURY_CHECK";
        public const string FT_Check_System_Failure = "SYSTEM_FAILURE_CHECK";
        public const string FT_Check_Fall = "FALLING_CHECK";
        public const string FT_Melee_Kick = "MELEE_KICK";
        public const string FT_Melee_Charge = "MELEE_CHARGE";
        public const string FT_Melee_DFA = "MELEE_DFA";
        public const string FT_Fall_After_Run = "RUN_AND_FALL";
        public const string FT_Fall_After_Jump = "JUMP_AND_FALL";
        public const string FT_Auto_Fail = "AUTO_FAIL";
        public const string FT_Hull_Breach = "HULL_BREACH";
        public Dictionary<string, string> LocalizedFloaties = new Dictionary<string, string> {
            { FT_Shutdown_Override, "Passed Shutdown Override" },
            { FT_Shutdown_Failed_Overide, "Failed Shutdown Override" },
            { FT_Shutdown_Fall, "Falling from Shutdown" },

            { FT_Check_Explosion, "Ammo Explosion Check" },
            { FT_Check_Volatile_Explosion, "Volatile Ammo Explosion Check" },
            { FT_Check_Shutdown, "Shutdown Check" },
            { FT_Check_Startup, "Startup Check" },
            { FT_Check_Injury, "Pilot Injury Check" },
            { FT_Check_System_Failure, "System Failure Check" },
            { FT_Check_Fall, "Falling Check" },

            { FT_Melee_Kick, "Kick Falling Check" },
            { FT_Melee_Charge, "Charge Falling Check" },
            { FT_Melee_DFA, "DFA Falling Check" },

            { FT_Fall_After_Run, "Sprinted with Damage" },
            { FT_Fall_After_Jump, "Jumped with Damage" },

            { FT_Auto_Fail, "Automatic Failure" },
            { FT_Hull_Breach, "Hull Breach Check" }
        };

        // CombatHUDTooltip Localization 
        public const string CHUD_TT_Title = "TITLE";
        public const string CHUD_TT_Heat = "HEAT_AND_SINKING";
        public const string CHUD_TT_Explosion = "AMMO_EXP_CHANCE";
        public const string CHUD_TT_Explosion_Warning = "AMMO_EXP_WARNING";
        public const string CHUD_TT_Injury = "PILOT_INJURY_CHANCE";
        public const string CHUD_TT_Sys_Failure = "SYSTEM_FAILURE_CHANCE";
        public const string CHUD_TT_Shutdown = "SHUTDOWN_CHANCE";
        public const string CHUD_TT_Shutdown_Warning = "SHUTDOWN_WARNING";
        public const string CHUD_TT_Attack = "ATTACK_PENALTY";
        public const string CHUD_TT_Move = "MOVEMENT_PENALTY";

        // Overheat warning
        public const string CHUDSP_TT_WARN_SHUTDOWN_TITLE = "SHUTDOWN_ICON_TITLE";
        public const string CHUDSP_TT_WARN_SHUTDOWN_TEXT = "SHUTDOWN_ICON_TEXT";
        public const string CHUDSP_TT_WARN_OVERHEAT_TITLE = "OVERHEAT_ICON_TITLE";
        public const string CHUDSP_TT_WARN_OVERHEAT_TEXT = "OVERHEAT_ICON_TEXT";

        public Dictionary<string, string> LocalizedCHUDTooltips = new Dictionary<string, string> {
            { CHUD_TT_Title, "HEAT LEVEL" },
            { CHUD_TT_Heat, "Heat: {0} of {1}  Will Sink: {2} of {3} (<color=#{4}>x{5:#.#}</color>)" },
            { CHUD_TT_Explosion, "\nAmmo Explosion on d100+{0} < {1:P1}" },
            { CHUD_TT_Explosion_Warning, "Guaranteed Ammo Explosion!" },
            { CHUD_TT_Injury, "\nPilot Injury on d100+{0} < {1:P1}" },
            { CHUD_TT_Sys_Failure, "\nSystem Failure on d100+{0} < {1:P1}" },
            { CHUD_TT_Shutdown, "\nShutdown on d100+{0} < {1:P1}" },
            { CHUD_TT_Shutdown_Warning, "\nGuaranteed Shutdown!" },
            { CHUD_TT_Attack, "\nAttack Penalty: <color=#FF0000>+{0}</color>" },
            { CHUD_TT_Move, "\nMovement Penalty: <color=#FF0000>-{0}m</color>" },

            { CHUDSP_TT_WARN_SHUTDOWN_TITLE, "SHUT DOWN" },
            { CHUDSP_TT_WARN_SHUTDOWN_TEXT, "This target is easier to hit, and Called Shots can be made against this target. When clicking the restart button, a piloting check will if the BattleMech restarts." },
            { CHUDSP_TT_WARN_OVERHEAT_TITLE, "OVERHEATING" },
            { CHUDSP_TT_WARN_OVERHEAT_TEXT, "This unit will suffer penalties, may shutdown or even explode unless heat is reduced past critical levels.\n<i>Hover over the heat bar to see a detailed breakdown.</i>" },
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

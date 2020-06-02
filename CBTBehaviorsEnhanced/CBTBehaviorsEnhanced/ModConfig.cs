

using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    
    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;

        // If true, will enable evasion 
        public static bool EnablePermanentEvasion = true;

        // If true, applies special behaviors for DonZappo's abilities
        public static bool dZ_Abilities = false;

        // Movement - should be a +3 per BT Manual pg. 28
        public int ToHitSelfJumped = 2;

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

        public class CustomCategoryOpts
        {
            public string HipActuatorCategoryId = "LegHip";
            public string UpperLegActuatorCategoryId = "LegUpperActuator";
            public string LowerLegActuatorCategoryId = "LegLowerActuator";
            public string FootActuatorCategoryId = "LegFootActuator";

            public string ShoulderActuatorCategoryId = "ArmShoulder";
            public string UpperArmActuatorCategoryId = "ArmUpperActuator";
            public string LowerArmActuatorCategoryId = "ArmLowerActuator";
            public string HandActuatorCategoryId = "ArmHandActuator";

        }
        public CustomCategoryOpts CustomCategories = new CustomCategoryOpts();

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

            // When to show the shutdown warning and when to where to place the 'overheated' bar
            public int MaxHeat = 150;
            public int WarnAtHeat = 42;
        }
        public HeatOptions Heat = new HeatOptions();


        public class ChargeMeleeOpts
        {
            // TT => 1 point / 10, HBS => 5 points / 10 == 0.5 points per ton
            public float AttackerDamagePerTargetTon = 0.5f;
            public float TargetDamagePerAttackerTon= 0.5f;

            public float AttackerInstabilityPerTargetTon = 0.5f;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            // When an attack does damage, it will be split into N groups of no more than this value 
            public float DamageClusterDivisor = 25.0f;

            // If true, make the attack apply unsteady before applying instability
            public bool AttackAppliesUnsteady = false;
        }

        // BT Manual pg.37 
        public class DFAMeleeOpts
        {
            // TT => 1 point / 5, HBS => 5 points / 5 == 1 points per ton
            public float TargetDamagePerAttackerTon = 0.5f;
            // Multiplies tonnage result
            public float TargetDamageMultiplier = 3.0f;

        }

        public class KickMeleeOps
        {
            // The base bonus applied for a kick 
            public int BaseAttackBonus = -2;
            public int LegActuatorDamageMalus = -2;
            public int FootActuatorDamageMalus = -1;

            // TT => 1 point / 5, HBS => 5 points / 5 == 1 points per ton
            public float TargetDamagePerAttackerTon = 1;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            public float LegActuatorDamageReduction = 0.5f;

            // If true, make the attack apply unsteady before applying instability
            public bool AttackAppliesUnsteady = false;
        }

        public class PhysicalWeaponMeleeOps
        {

        }

        public class PunchMeleeOps
        {
            public int ArmActuatorDamageMalus = -2;
            public int HandActuatorDamageMalus = -1;

            // TT => 1 point / 10, HBS => 5 points / 10 == 0.5 points per ton
            public float TargetDamagePerAttackerTon = 0.5f;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            public float ArmActuatorDamageReduction = 0.5f;

            // If true, make the attack apply unsteady before applying instability
            public bool AttackAppliesUnsteady = false;
        }


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

            // Prone target modifier
            public int ProneTargetAttackModifier = -2;

            public ChargeMeleeOpts Charge = new ChargeMeleeOpts();
            public DFAMeleeOpts DFA = new DFAMeleeOpts();
            public KickMeleeOps Kick = new KickMeleeOps();
            public PhysicalWeaponMeleeOps PhysicalWeapon = new PhysicalWeaponMeleeOps();
            public PunchMeleeOps Punch = new PunchMeleeOps();

        }
        public MeleeOptions Melee = new MeleeOptions();

        public class MoveOptions {
            // This is set to 40m, because it should the minimum required to move across one 'hex' no 
            //   matter the penalties on that hex.
            public float MinimumMove = 40f;

            // How much walk distance is removed for each point of heat penalty
            public float HeatMovePenalty = 24f;

            // When calculating RunSpeed, multiply the current WalkSpeed by this amount. 
            public float RunMulti = 1.5f;

            // Multiplier for the pilot's piloting skill used in the check for FallAfterRunChance and FallAfterJumpChance
            public float SkillMulti = 0.05f;

            // If you have leg damage and run, you can fall
            public float FallAfterRunChance = 0.30f;

            // If you have leg damage and jump, you can fall
            public float FallAfterJumpChance = 0.30f;

            // If true, walk and run speeds will be normalized to MP instead of the HBS speeds.
            // General - should match setting from https://github.com/BattletechModders/MechEngineer/blob/master/source/Features/Engines/EngineSettings.cs#L32
            public bool SpeedAsMP = false;

            //   This is set to 24m, because both ME and HexGrid.HexWidth reply upon it. However, it should likely be larger, as designMasks and vertical distances
            //   could prevent a unit from moving *at all* if this value is too low. A value like 40m should ensure a unit can always move, even through designMasks 
            //   with 0.8 movement mods and with a 0.8 elevation pitch.
            public float MPMetersPerHex = 24f;

        }
        public MoveOptions Move = new MoveOptions();

        // 4+ => 91.66%, 6+ => 72.22%, 8+ => 41.67%, 10+ => 16.67%, 12+ => 2.78%
        public class PilotingOptions {
            public float SkillMulti = 0.05f;
            public float StabilityCheck = 0.30f;
            public float DFAReductionMulti = 0.05f;

            // How many damage points 
            public int FallingDamagePerTenTons = 5;
        }
        public PilotingOptions Piloting = new PilotingOptions();

        public class BiomeBreachOptions {
            public float VacuumCheck = 0.17f;
            public float ThinAtmoCheck = 0.03f;
        }
        public BiomeBreachOptions Breaches = new BiomeBreachOptions();

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

        public const string FT_Death_By_Overheat = "PILOT_DEATH_OVERHEAT";
        public const string FT_Death_By_Falling = "PILOT_DEATH_FALLING";

        public const string FT_Melee_Kick = "MELEE_KICK";
        public const string FT_Melee_Charge = "MELEE_CHARGE";
        public const string FT_Melee_DFA = "MELEE_DFA";
        public const string FT_Fall_After_Run = "RUN_AND_FALL";
        public const string FT_Fall_After_Jump = "JUMP_AND_FALL";
        public const string FT_Auto_Fail = "AUTO_FAIL";
        public const string FT_Hull_Breach = "HULL_BREACH";

        // Localized Floaties
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

            { FT_Death_By_Overheat, "PILOT KILLED BY HEAT" },
            { FT_Death_By_Falling, "PILOT KILLED BY FALLING" },

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
        public const string CHUD_TT_End_Heat = "END_OF_TURN_HEAT";
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

        // Localized tooltips
        public Dictionary<string, string> LocalizedCHUDTooltips = new Dictionary<string, string> {
            { CHUD_TT_Title, "HEAT LEVEL" },
            { CHUD_TT_End_Heat, "Projected Heat: {0} of {1}" },
            { CHUD_TT_Heat, "\n  Current Heat: {0} of {1}  Heat Sinking: {2} of {3} (<color=#{4}>x{5:#.#}</color>)" },
            { CHUD_TT_Explosion, "\nAmmo Explosion on (d100+{0}) < {1}" },
            { CHUD_TT_Explosion_Warning, "Guaranteed Ammo Explosion!" },
            { CHUD_TT_Injury, "\nPilot Injury on (d100+{0}) < {1}" },
            { CHUD_TT_Sys_Failure, "\nSystem Failure on (d100+{0}) < {1}" },
            { CHUD_TT_Shutdown, "\nShutdown on (d100+{0}) < {1}" },
            { CHUD_TT_Shutdown_Warning, "\nGuaranteed Shutdown!" },
            { CHUD_TT_Attack, "\nAttack Penalty: <color=#FF0000>+{0}</color>" },
            { CHUD_TT_Move, "\nMovement Penalty: <color=#FF0000>-{0}m</color>" },

            { CHUDSP_TT_WARN_SHUTDOWN_TITLE, "SHUT DOWN" },
            { CHUDSP_TT_WARN_SHUTDOWN_TEXT, "This target is easier to hit, and Called Shots can be made against this target. When clicking the restart button, a piloting check will if the BattleMech restarts." },
            { CHUDSP_TT_WARN_OVERHEAT_TITLE, "OVERHEATING" },
            { CHUDSP_TT_WARN_OVERHEAT_TEXT, "This unit will suffer penalties, may shutdown or even explode unless heat is reduced past critical levels.\n<i>Hover over the heat bar to see a detailed breakdown.</i>" },
        };

        // Localized strings for the attack descriptions 

        // Labels for weapon tooltips
        public const string LT_AtkDesc_ComparativeSkill_Piloting = "ATK_MOD_COMPARATIVE_PILOTING";
        public const string LT_AtkDesc_Easy_to_Kick = "ATK_MOD_EASY_TO_KICK";
        public const string LT_AtkDesc_Acutator_Damage = "ATK_MOD_ACTUATOR_DAMAGE";
        public const string LT_AtkDesc_Target_Prone = "ATK_MOD_TARGET_PRONE";

        // Labels for descriptions
        public const string LT_AtkDesc_Charge_Desc = "CHARGE_DESC";
        public const string LT_AtkDesc_Kick_Desc = "KICK_DESC";
        public const string LT_AtkDesc_Physical_Weapon_Desc = "PHYSICAL_WEAPON_DESC";
        public const string LT_AtkDesc_Punch_Desc = "PUNCH_DESC";

        public Dictionary<string, string> LocalizedAttackDescs = new Dictionary<string, string>
        {
            { LT_AtkDesc_ComparativeSkill_Piloting, "COMPARATIVE PILOTING" },
            { LT_AtkDesc_Easy_to_Kick, "EASY TO KICK" },
            { LT_AtkDesc_Acutator_Damage, "ACTUATOR DAMAGE" },
            { LT_AtkDesc_Target_Prone, "PRONE MELEE TARGET" },

            { LT_AtkDesc_Charge_Desc, "Charges damage both " +
                "the attacker and target. Damage is randomized across all locations in 25 " +
                "point clusters." +
                "<color=#ff0000>Attacker Damage: {0]  Instability: {0}</color>" +
                "<color=#00ff00>Target Damage: {0]  Instability: {0}</color>"
            },
            { LT_AtkDesc_Kick_Desc, "Kicks inflict a single hit that strikes the legs of the target. " +
                "Prone targets received damage on their rear torsos, arms, and legs." +
                "<color=#ff0000>Damage: {0]  Instability: {0}</color>"
            },
            { LT_AtkDesc_Physical_Weapon_Desc, "Physical weapons inflict damage and instability to " +
                "the target. Damage is applied in a single hit randomized across all target locations. " +
                "Some weapons will target punch or kick location" +
                "<color=#ff0000>Damage: {0]  Instability: {0}</color>"
            },
            { LT_AtkDesc_Punch_Desc, "Punches inflict a single hit that strikes " +
                "the arms, torsos, and head of the target." +
                "<color=#ff0000>Attacker Damage: {0]  Instability: {0}</color>" +
                "<color=#00ff00>Target Damage: {0]  Instability: {0}</color>"
            },


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

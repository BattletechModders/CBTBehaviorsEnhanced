using System.Collections.Generic;

namespace CBTBehaviorsEnhanced
{
    public class ModText
    {
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

        public const string FT_Swarm_Attack = "SWARM_ATTACK";
        public Dictionary<string, string> Floaties = new();

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
        public Dictionary<string, string> Tooltips = new();

        // Long-form Attack descriptions
        public const string LT_AtkDesc_Charge_Desc = "CHARGE_DESC";
        public const string LT_AtkDesc_DFA_Desc = "DFA_DESC";
        public const string LT_AtkDesc_Kick_Desc = "KICK_DESC";
        public const string LT_AtkDesc_Physical_Weapon_Desc = "PHYSICAL_WEAPON_DESC";
        public const string LT_AtkDesc_Punch_Desc = "PUNCH_DESC";
        public Dictionary<string, string> AttackDescriptions = new();

        // Labels for weapon tooltips
        public const string LT_Label_Actuator_Damage = "ATK_MOD_ACTUATOR_DAMAGE";
        public const string LT_Label_Attacker_Jumped = "ATK_MOD_ATTACKER_JUMPED";
        public const string LT_Label_ComparativeSkill_Piloting = "ATK_MOD_COMPARATIVE_PILOTING";
        public const string LT_Label_Easy_to_Kick = "ATK_MOD_EASY_TO_KICK";
        public const string LT_Label_Target_Prone = "ATK_MOD_TARGET_PRONE";

        public const string LT_Label_Charge_Attack_Mod = "ATK_MOD_CHARGE_MOD";
        public const string LT_Label_Kick_Attack_Mod = "ATK_MOD_KICK_MOD";
        public const string LT_Label_Punch_Attack_Mod = "ATK_MOD_PUNCH_MOD";
        public const string LT_Label_Physical_Weapon_Attack_Mod = "ATK_MOD_PHYS_WEP_MOD";
        public const string LT_Label_DFA_Attack_Mod = "ATK_MOD_DFA_MOD";

        public const string LT_Label_Weapon_Hover_Damage = "WEAPON_HOVER_DAMAGE";
        public const string LT_Label_Weapon_Hover_Instability = "WEAPON_HOVER_INSTABILITY";
        public const string LT_Label_Weapon_Hover_Heat = "WEAPON_HOVER_HEAT";

        // Fire button labels
        public const string LT_Label_Melee_Type_Charge = "MELEE_TYPE_CHARGE";
        public const string LT_Label_Melee_Type_DeathFromAbove = "MELEE_TYPE_DFA";
        public const string LT_Label_Melee_Type_Kick = "MELEE_TYPE_KICK";
        public const string LT_Label_Melee_Type_Physical_Weapon = "MELEE_TYPE_PHYSICAL_WEAPON";
        public const string LT_Label_Melee_Type_Punch = "MELEE_TYPE_PUNCH";

        public const string LT_Label_Melee_Table_Standard = "MELEE_TABLE_STANDARD";
        public const string LT_Label_Melee_Table_Punch = "MELEE_TABLE_PUNCH";
        public const string LT_Label_Melee_Table_Legs = "MELEE_TABLE_LEGS";

        // Labels for melee and dfa weapons in the weapons panel
        public const string LT_Label_Weapon_Panel_Melee_Weapon = "WEAPON_PANEL_MELEE_WEAPON";
        public const string LT_Label_Weapon_Panel_Melee_No_Attack_Type = "WEAPON_PANEL_MELEE_NO_ATTACK_TYPE";
        public const string LT_Label_Weapon_Panel_Melee_No_Attack_Type_Damage = "WEAPON_PANEL_MELEE_NO_ATTACK_TYPE_DAMAGE";
        public Dictionary<string, string> Labels = new();

        // In-game quips that can be displayed
        public class QuipsConfig
        {
            public List<string> Breach = new();
            public List<string> Knockdown = new();
            public List<string> Startup = new();
        }
        public QuipsConfig Quips = new();

        // Newtonsoft seems to merge values into existing dictionaries instead of replacing them entirely. So instead
        //   populate default values in dictionaries through this call instead
        public void InitUnsetValues()
        {

            if (this.Floaties.Count == 0)
            {
                this.Floaties = new Dictionary<string, string> 
                {
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
                    { FT_Hull_Breach, "Hull Breach Check" },

                    { FT_Swarm_Attack, "Swarm Attack!" }
                };
            }

            if (this.Tooltips.Count == 0)
            {
                this.Tooltips = new Dictionary<string, string> 
                {
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
                    { CHUDSP_TT_WARN_OVERHEAT_TEXT, "This unit will suffer penalties, may shutdown or even explode unless heat is reduced past critical levels.\n<i>Hover over the heat bar to see a detailed breakdown.</i>" }
                };
            }

            if (this.AttackDescriptions.Count == 0)
            {
                this.AttackDescriptions = new()
                {
                    { LT_AtkDesc_Charge_Desc,
                        "Charges damage both the attacker and target. Damage is randomized across all locations in 25 point clusters.\n" +
                        "<color=#ff0000>Attacker Damage: {0}  Instability: {1}</color>\n" +
                        "<color=#00ff00>Target Damage: {2}  Instability: {3}</color>"
                    },
                    { LT_AtkDesc_DFA_Desc, "Death-From-Above attacks damage both " +
                        "the attacker and target. Damage is randomized across all locations in 25 " +
                        "point clusters." +
                        "<color=#ff0000>Attacker Damage: {0}  Instability: {1}</color>" +
                        "<color=#00ff00>Target Damage: {2}  Instability: {3}</color>"
                    },
                    { LT_AtkDesc_Kick_Desc, "Kicks inflict a single hit that strikes the legs of the target. " +
                        "<color=#ff0000>Damage: {0}  Instability: {1}</color>"
                    },
                    { LT_AtkDesc_Physical_Weapon_Desc, "Physical weapons inflict damage and instability to " +
                        "the target. Damage is applied in a single hit randomized across all target locations. " +
                        "Some weapons will target punch or kick locations." +
                        "<color=#ff0000>Damage: {0}  Instability: {1}</color>"
                    },
                    { LT_AtkDesc_Punch_Desc, "Punches inflict a single hit that strikes " +
                        "the arms, torsos, and head of the target." +
                        "<color=#ff0000>Damage: {0}  Instability: {1}</color>"
                    }
                };
            }
            
            if (this.Labels.Count == 0)
            {
                this.Labels = new Dictionary<string, string>
                {
                    // Attack labels
                    { LT_Label_Actuator_Damage, "ACTUATOR DAMAGE" },
                    { LT_Label_Attacker_Jumped, "ATTACKER JUMPED" },
                    { LT_Label_ComparativeSkill_Piloting, "PILOTING DELTA" },
                    { LT_Label_Easy_to_Kick, "EASY TO KICK" },
                    { LT_Label_Target_Prone, "PRONE MELEE TARGET" },

                    { LT_Label_Charge_Attack_Mod, "CHARGE MOD" },
                    { LT_Label_Kick_Attack_Mod, "KICK MOD" },
                    { LT_Label_Punch_Attack_Mod, "PUNCH MOD" },
                    { LT_Label_Physical_Weapon_Attack_Mod, "P.WEAP MOD" },
                    { LT_Label_DFA_Attack_Mod, "DFA MOD" },

                    // Weapon hover labels
                    { LT_Label_Weapon_Hover_Damage, "{0} dmg" },
                    { LT_Label_Weapon_Hover_Instability, "{0} stab" },
                    { LT_Label_Weapon_Hover_Heat, "+{0} heat" },

                    // General attack type labels
                    { LT_Label_Melee_Type_Charge, "CHARGE" },
                    { LT_Label_Melee_Type_DeathFromAbove, "DFA" },
                    { LT_Label_Melee_Type_Kick, "KICK" },
                    { LT_Label_Melee_Type_Physical_Weapon, "PHY. WEAPON" },
                    { LT_Label_Melee_Type_Punch, "PUNCH" },

                    { LT_Label_Melee_Table_Standard, "MELEE_TABLE_STANDARD" },
                    { LT_Label_Melee_Table_Punch, "MELEE_TABLE_PUNCH" },
                    { LT_Label_Melee_Table_Legs, "MELEE_TABLE_LEGS" },

                    // Weapon panel labels
                    { LT_Label_Weapon_Panel_Melee_Weapon, "MELEE - {0}" },
                    { LT_Label_Weapon_Panel_Melee_No_Attack_Type, "(w/p/k)" },
                    { LT_Label_Weapon_Panel_Melee_No_Attack_Type_Damage, "{0} / {1} / {2}" }
                };
            }

            if (this.Quips.Breach.Count == 0)
            {
                this.Quips.Breach = new List<string>()
                {
                    "Shit, explosive decompression!",
                    "Hull breach detected!",
                    "I've lost something to atmo!",
                    "I hope life support holds up",
                    "I cant breathe!",
                    "There are cracks in my cockpit!",
                    "Components crippled by vacuum",
                    "Alot of red from that hit",
                    "Damn, lost alot of gear"
                };
            }

            if (this.Quips.Knockdown.Count == 0)
            {
                this.Quips.Knockdown = new List<string>()
                {
                    "Oh .. shit!",
                    "FML",
                    "This is going to hurt..",
                    "Can't keep her steady!",
                    "Gyro's not compensating!",
                    "Screw you gravity!",
                    "Going down!",
                    "You're a bastard Newton",
                    "Stability lost, controls unresponsive!",
                    "Controls are locked up, can't stop the fall!",
                    "Past the critical point, she's going down!",
                    "Bracing for ground impact",
                    "Impact protection don't fail",
                    "Dampening neurohelm feedback",
                    "Hope the armor takes it",
                    "About to bite the dust!"
                };
            }

            if (this.Quips.Startup.Count == 0)
            {
                this.Quips.Startup = new List<string>
                {
                    "Start damn you",
                    "Can't see through this heat",
                    "Where is the start button?",
                    "Override damn it, override!",
                    "Time to void the warranty",
                    "Why won't you turn on",
                    "I put in the startup sequence!"
                };
            }
        }
    }
}

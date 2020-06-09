

using System.Collections.Generic;

namespace CBTBehaviorsEnhanced {
    
    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;

        public class FeatureList {
            // If true, hull breaches will be allowed in certain biomes
            public bool BiomeBreaches = true;

            // If true, evasion won't be removed by attacks
            public bool PermanentEvasion = true;

            // If true, walk and run speeds will be normalized to MP instead of the HBS speeds.
            // General - should match setting from https://github.com/BattletechModders/MechEngineer/blob/master/source/Features/Engines/EngineSettings.cs#L32
            public bool SpeedAsMP = false;

            // If true, mechs must make a piloting skill roll (PSR) to restart. On a failure, they remain shutdown.
            public bool StartupChecks = true;
        }
        public FeatureList Features = new FeatureList();

        // Movement - should be a +3 per BT Manual pg. 28
        public int ToHitSelfJumped = 3;

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
            public bool UnsteadyAttackerOnHit = false;
            public bool UnsteadyAttackerOnMiss = false;
            public bool UnsteadyTargetOnHit = false;
        }

        // BT Manual pg.37 
        public class DFAMeleeOpts
        {
            // TT => 1 point / 10, HBS => 5 points / 10 == 0.5 points per ton, x3 for DFA
            public float AttackerDamagePerTargetTon = 0.5f;
            public float TargetDamagePerAttackerTon = 1.5f;

            public float AttackerInstabilityPerTargetTon = 0.5f;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            // When an attack does damage, it will be split into N groups of no more than this value 
            public float DamageClusterDivisor = 25.0f;

            // If true, make the attack apply unsteady before applying instability
            public bool UnsteadyAttackerOnHit = false;
            public bool UnsteadyAttackerOnMiss = false;
            public bool UnsteadyTargetOnHit = false;
        }

        public class KickMeleeOps
        {
            // The base bonus applied for a kick 
            public int BaseAttackBonus = -2;
            public int LegActuatorDamageMalus = 2;
            public int FootActuatorDamageMalus = 1;

            // TT => 1 point / 5, HBS => 5 points / 5 == 1 points per ton
            public float TargetDamagePerAttackerTon = 1;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            public float LegActuatorDamageReduction = 0.5f;

            // If true, make the attack apply unsteady before applying instability
            public bool UnsteadyAttackerOnHit = false;
            public bool UnsteadyAttackerOnMiss = false;
            public bool UnsteadyTargetOnHit = false;
        }

        public class PhysicalWeaponMeleeOps
        {
            public int ArmActuatorDamageMalus = 2;

            public float DefaultDamagePerAttackerTon = 2;
            public float DefaultInstabilityPerAttackerTon = 1f;

            public bool DefaultUnsteadyAttackerOnHit = false;
            public bool DefaultUnsteadyAttackerOnMiss = false;
            public bool DefaultUnsteadyTargetOnHit = false;
        }

        public class PunchMeleeOps
        {
            public int ArmActuatorDamageMalus = 2;
            public int HandActuatorDamageMalus = 1;

            // TT => 1 point / 10, HBS => 5 points / 10 == 0.5 points per ton
            public float TargetDamagePerAttackerTon = 0.5f;
            public float TargetInstabilityPerAttackerTon = 0.5f;

            public float ArmActuatorDamageReduction = 0.5f;

            // If true, make the attack apply unsteady before applying instability
            public bool UnsteadyAttackerOnHit = false;
            public bool UnsteadyAttackerOnMiss = false;
            public bool UnsteadyTargetOnHit = false;
        }


        // 4+ => 91.66%, 6+ => 72.22%, 8+ => 41.67%, 10+ => 16.67%, 12+ => 2.78%
        public class MeleeOptions {

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

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  Debug: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info($"  FEATURES => BiomeBreaches: {this.Features.BiomeBreaches}  " +
                $"PermanentEvasion: {this.Features.PermanentEvasion}  SpeedAsMP: {this.Features.SpeedAsMP}  " +
                $"StartupChecks: {this.Features.StartupChecks}");
            Mod.Log.Info($"  ToHitSelfJumped: {this.ToHitSelfJumped}");
            Mod.Log.Info("");

            Mod.Log.Info("=== CUSTOM CATEGORY OPTIONS ===");
            Mod.Log.Info($"  ShoulderActuatorCategoryId: '{this.CustomCategories.ShoulderActuatorCategoryId}'  UpperArmActuatorCategoryId: '{this.CustomCategories.UpperArmActuatorCategoryId}'  " +
                $"LowerArmActuatorCategoryId: '{this.CustomCategories.LowerArmActuatorCategoryId}'  HandActuatorCategoryId: '{this.CustomCategories.HandActuatorCategoryId}'  ");
            Mod.Log.Info($"  HipActuatorCategoryId: '{this.CustomCategories.HipActuatorCategoryId}'  UpperLegActuatorCategoryId: '{this.CustomCategories.UpperLegActuatorCategoryId}'  " +
                $"LowerLegActuatorCategoryId: '{this.CustomCategories.LowerLegActuatorCategoryId}'  FootActuatorCategoryId: '{this.CustomCategories.FootActuatorCategoryId}'  ");
            Mod.Log.Info("");

            Mod.Log.Info("=== HEAT OPTIONS ===");
            Mod.Log.Info("");

            Mod.Log.Info("=== MELEE OPTIONS ===");
            Mod.Log.Info($"  AllowMeleeFromSprint: {this.Melee.AllowMeleeFromSprint}  ProneTargetAttackModifier: {this.Melee.ProneTargetAttackModifier}");
            Mod.Log.Info("  -- CHARGE OPTIONS --");
            Mod.Log.Info($"  AttackerDamagePerTargetTon: {this.Melee.Charge.AttackerDamagePerTargetTon}  AttackerInstabilityPerTargetTon: {this.Melee.DFA.AttackerInstabilityPerTargetTon}");
            Mod.Log.Info($"  TargetDamagePerAttackerTon: {this.Melee.Charge.TargetDamagePerAttackerTon}  TargetInstabilityPerAttackerTon: {this.Melee.DFA.TargetInstabilityPerAttackerTon}");
            Mod.Log.Info($"  DamageClusterDivisor: {this.Melee.Charge.DamageClusterDivisor}");
            Mod.Log.Info($"  Unsteady => AttackerOnHit: {this.Melee.Charge.UnsteadyAttackerOnHit}  AttackerOnMiss: {this.Melee.Charge.UnsteadyAttackerOnMiss}  TargetOnHit: {this.Melee.Charge.UnsteadyTargetOnHit}");

            Mod.Log.Info("  -- DFA OPTIONS --");
            Mod.Log.Info($"  AttackerDamagePerTargetTon: {this.Melee.DFA.AttackerDamagePerTargetTon}  AttackerInstabilityPerTargetTon: {this.Melee.DFA.AttackerInstabilityPerTargetTon}");
            Mod.Log.Info($"  TargetDamagePerAttackerTon: {this.Melee.DFA.TargetDamagePerAttackerTon}  TargetInstabilityPerAttackerTon: {this.Melee.DFA.TargetInstabilityPerAttackerTon}");
            Mod.Log.Info($"  DamageClusterDivisor: {this.Melee.DFA.DamageClusterDivisor}");
            Mod.Log.Info($"  Unsteady => AttackerOnHit: {this.Melee.DFA.UnsteadyAttackerOnHit}  AttackerOnMiss: {this.Melee.DFA.UnsteadyAttackerOnMiss}  TargetOnHit: {this.Melee.DFA.UnsteadyTargetOnHit}");

            Mod.Log.Info("  -- KICK OPTIONS --");
            Mod.Log.Info($"  BaseAttackBonus: {this.Melee.Kick.BaseAttackBonus}  LegActuatorDamageMalus: {this.Melee.Kick.LegActuatorDamageMalus}  FootActuatorDamageMalus: {this.Melee.Kick.FootActuatorDamageMalus}");
            Mod.Log.Info($"  TargetDamagePerAttackerTon: {this.Melee.Kick.TargetDamagePerAttackerTon}  TargetInstabilityPerAttackerTon: {this.Melee.Kick.TargetInstabilityPerAttackerTon}");
            Mod.Log.Info($"  LegActuatorDamageReduction: {this.Melee.Kick.LegActuatorDamageReduction}");
            Mod.Log.Info($"  Unsteady => AttackerOnHit: {this.Melee.Kick.UnsteadyAttackerOnHit}  AttackerOnMiss: {this.Melee.Kick.UnsteadyAttackerOnMiss}  TargetOnHit: {this.Melee.Kick.UnsteadyTargetOnHit}");

            Mod.Log.Info("  -- PHYSICAL WEAPON OPTIONS --");
            Mod.Log.Info($"  DefaultDamagePerAttackTon: {this.Melee.PhysicalWeapon.DefaultDamagePerAttackerTon}  DefaultInstabilityPerAttackerTon: {this.Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon}");
            Mod.Log.Info($"  ArmActuatorDamageMalus: {this.Melee.PhysicalWeapon.ArmActuatorDamageMalus}");
            Mod.Log.Info($"  Unsteady Default => AttackerOnHit: {this.Melee.PhysicalWeapon.DefaultUnsteadyAttackerOnHit}  AttackerOnMiss: {this.Melee.PhysicalWeapon.DefaultUnsteadyAttackerOnMiss}  TargetOnHit: {this.Melee.PhysicalWeapon.DefaultUnsteadyTargetOnHit}");

            Mod.Log.Info("  -- PUNCH OPTIONS --");
            Mod.Log.Info($"  TargetDamagePerAttackerTon: {this.Melee.Punch.TargetDamagePerAttackerTon}  TargetInstabilityPerAttackerTon: {this.Melee.Punch.TargetInstabilityPerAttackerTon}");
            Mod.Log.Info($"  ArmActuatorDamageMalus: {this.Melee.Punch.ArmActuatorDamageMalus}  HandActuatorDamageMalus: {this.Melee.Punch.HandActuatorDamageMalus}");
            Mod.Log.Info($"  ArmActuatorDamageReduction: {this.Melee.Punch.ArmActuatorDamageReduction}");
            Mod.Log.Info($"  Unsteady => AttackerOnHit: {this.Melee.Punch.UnsteadyAttackerOnHit}  AttackerOnMiss: {this.Melee.Punch.UnsteadyAttackerOnMiss}  TargetOnHit: {this.Melee.Punch.UnsteadyTargetOnHit}");
            Mod.Log.Info("");

            Mod.Log.Info("=== MOVE OPTIONS ===");
            Mod.Log.Info($"  MinimumMove: {this.Move.MinimumMove}m  HeatMovePenalty: {this.Move.HeatMovePenalty}m  RunMulti: x{this.Move.RunMulti}  SkillMulti: x{this.Move.SkillMulti}");
            Mod.Log.Info($"  FallAfterChances =>   Jump: {this.Move.FallAfterJumpChance}  Run: {this.Move.FallAfterRunChance}");
            Mod.Log.Info($"  MPMetersPerHex: {this.Move.MPMetersPerHex}m");
            Mod.Log.Info("");

            Mod.Log.Info("=== PILOTING OPTIONS ===");
            Mod.Log.Info($"  SkillMulti: x{this.Piloting.SkillMulti}  StabilityCheck: {this.Piloting.StabilityCheck}");
            Mod.Log.Info($"  DFAReductionMulti: x{this.Piloting.DFAReductionMulti}  FallingDamagePerTenTons: {this.Piloting.FallingDamagePerTenTons}");
            Mod.Log.Info("");

            Mod.Log.Info("=== BREACHES OPTIONS ===");
            Mod.Log.Info($"  ThinAtmoCheck: {this.Breaches.ThinAtmoCheck}  VacuumCheck: {this.Breaches.VacuumCheck}");
            Mod.Log.Info("");



            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}


// Container for all constant values, like statistic IDs
namespace CBTBehaviorsEnhanced
{
    public class ModStats
    {
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

        // Melee damage modifier stats
        public const string ChargeDamageMod = "CBTBE_Charge_Damage_Mod";
        public const string ChargeDamageMulti = "CBTBE_Charge_Damage_Multi";

        public const string DFAWeaponDamageMod = "CBTBE_DFA_Damage_Mod";
        public const string DFAWeaponDamageMulti = "CBTBE_DFA_Damage_Multi";

        public const string KickDamageMod = "CBTBE_Kick_Damage_Mod";
        public const string KickDamageMulti = "CBTBE_Kick_Damage_Multi";

        public const string PunchDamageMod = "CBTBE_Punch_Damage_Mod";
        public const string PunchDamageMulti = "CBTBE_Punch_Damage_Multi";

        public const string PhysicalWeaponDamageMod = "CBTBE_Physical_Damage_Mod";
        public const string PhysicalWeaponDamageMulti = "CBTBE_Physical_Damage_Multi";

        public const string PunchIsPhysicalWeapon = "CBTBE_Punch_Is_Physical_Weapon"; // If true, 
        public const string PhysicalWeaponTable = "CTBE_Physical_Weapon_Table"; // Allows setting attack type to punch or standard
        public const string PhysicalWeaponDivisor = "CTBE_Physical_Weapon_Damage_Divisor"; // Allows setting attack type to punch or standard
    }

    public class ModConsts
    {
        public const string Container_GO_ID = "cbtbe_melee_container";
        public const string ChargeFB_GO_ID = "cbtbe_charge_button";
        public const string KickFB_GO_ID = "cbtbe_kick_button";
        public const string PhysicalWeaponFB_GO_ID = "cbtbe_phys_weapon_button";
        public const string PunchFB_GO_ID = "cbtbe_punch_button";
    }
}

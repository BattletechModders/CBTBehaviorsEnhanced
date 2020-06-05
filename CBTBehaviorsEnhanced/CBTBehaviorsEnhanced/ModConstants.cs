
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

        // HBS Values
        public const string HBS_HeatSinkCapacity = "HeatSinkCapacity";
        public const string HBS_Weapon_DamagePerShot = "DamagePerShot";
        public const string HBS_Weapon_Instability = "Instability";


        // Melee damage modifier stats
        public const string ChargeDamageMod = "CBTBE_Charge_Damage_Mod"; // int
        public const string ChargeDamageMulti = "CBTBE_Charge_Damage_Multi"; // float

        public const string DFAWeaponDamageMod = "CBTBE_DFA_Damage_Mod"; // int
        public const string DFAWeaponDamageMulti = "CBTBE_DFA_Damage_Multi"; // float

        public const string KickDamageMod = "CBTBE_Kick_Damage_Mod"; // int
        public const string KickDamageMulti = "CBTBE_Kick_Damage_Multi"; // float

        public const string PunchDamageMod = "CBTBE_Punch_Damage_Mod"; // int
        public const string PunchDamageMulti = "CBTBE_Punch_Damage_Multi"; // float

        public const string PunchIsPhysicalWeapon = "CBTBE_Punch_Is_Physical_Weapon"; // bool - if true, signals that this unit has a physical attack
        public const string PhysicalWeaponDamageMod = "CBTBE_Physical_Damage_Mod"; // int
        public const string PhysicalWeaponDamageMulti = "CBTBE_Physical_Damage_Multi"; // float
        public const string PhysicalWeaponLocationTable = "CBTBE_Physical_Weapon_Location_Table"; // string Allows setting attack type to punch or standard
        public const string PhysicalWeaponDamageDivisor = "CBTBE_Physical_Weapon_Damage_Divisor"; // float - how much damage / ton the weapon does
        public const string PhysicalWeaponInstabilityDivisor = "CBTBE_Physical_Weapon_Instability_Divisor"; // float - how much stab / ton the weapon does
        public const string PhysicalWeaponAppliesUnsteady = "CBTBE_Physical_Weapon_Applies_Unsteady"; // bool - Allows setting attack type to punch or standard
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

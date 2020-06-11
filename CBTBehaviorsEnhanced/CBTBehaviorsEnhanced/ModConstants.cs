
// Container for all constant values, like statistic IDs
using RootMotion.FinalIK;

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
        public const string HBS_DFA_Self_Damage = "DFASelfDamage";
        public const string HBS_DFA_Causes_Self_Unsteady = "DFACausesSelfUnsteady";
        public const string HBS_Received_Instability_Multi = "ReceivedInstabilityMultiplier";
        public const string HBS_Ignore_Pilot_Injuries = "IgnorePilotInjuries";

        // Melee damage modifier stats
        public const string ChargeAttackMod = "CBTBE_Charge_Attack_Mod"; // int - a straight modifier to the attack roll

        public const string ChargeAttackerDamageMod = "CBTBE_Charge_Attacker_Damage_Mod"; // int
        public const string ChargeAttackerDamageMulti = "CBTBE_Charge_Attacker_Damage_Multi"; // float
        public const string ChargeAttackerInstabilityMod = "CBTBE_Charge_Attacker_Instability_Mod"; // int
        public const string ChargeAttackerInstabilityMulti = "CBTBE_Charge_Attacker_Instability_Multi"; // float

        public const string ChargeTargetDamageMod = "CBTBE_Charge_Target_Damage_Mod"; // int
        public const string ChargeTargetDamageMulti = "CBTBE_Charge_Target_Damage_Multi"; // float
        public const string ChargeTargetInstabilityMod = "CBTBE_Charge_Target_Instability_Mod"; // int
        public const string ChargeTargetInstabilityMulti = "CBTBE_Charge_Target_Instability_Multi"; // float

        public const string DeathFromAboveAttackMod = "CBTBE_DFA_Attack_Mod"; // int - a straight modifier to the attack roll

        public const string DeathFromAboveAttackerDamageMod = "CBTBE_DFA_Attacker_Damage_Mod"; // int
        public const string DeathFromAboveAttackerDamageMulti = "CBTBE_DFA_Attacker_Damage_Multi"; // float
        public const string DeathFromAboveAttackerInstabilityMod = "CBTBE_DFA_Attacker_Instability_Mod"; // int
        public const string DeathFromAboveAttackerInstabilityMulti = "CBTBE_DFA_Attacker_Instability_Multi"; // float

        public const string DeathFromAboveTargetDamageMod = "CBTBE_DFA_Target_Damage_Mod"; // int
        public const string DeathFromAboveTargetDamageMulti = "CBTBE_DFA_Target_Damage_Multi"; // float
        public const string DeathFromAboveTargetInstabilityMod = "CBTBE_DFA_Target_Instability_Mod"; // int
        public const string DeathFromAboveTargetInstabilityMulti = "CBTBE_DFA_Target_Instability_Multi"; // float

        public const string KickAttackMod = "CBTBE_Kick_Attack_Mod"; // int - a straight modifier to the attack roll
        public const string KickExtraHitsCount = "CBTBE_Kick_Extra_Hits_Count"; // float - a number of extra hits (using the calculated damage of the single strike) that will be applied. Will be rounded down.

        public const string KickTargetDamageMod = "CBTBE_Kick_Target_Damage_Mod"; // int
        public const string KickTargetDamageMulti = "CBTBE_Kick_Target_Damage_Multi"; // float
        public const string KickTargetInstabilityMod = "CBTBE_Kick_Target_Instability_Mod"; // int
        public const string KickTargetInstabilityMulti = "CBTBE_Kick_Target_Instability_Multi"; // float

        public const string PunchAttackMod = "CBTBE_Punch_Attack_Mod"; // int - a straight modifier to the attack roll
        public const string PunchExtraHitsCount = "CBTBE_Punch_Extra_Hits_Count"; // float - a number of extra hits (using the calculated damage of the single strike) that will be applied. Will be rounded down.

        public const string PunchTargetDamageMod = "CBTBE_Punch_Target_Damage_Mod"; // int
        public const string PunchTargetDamageMulti = "CBTBE_Punch_Target_Damage_Multi"; // float
        public const string PunchTargetInstabilityMod = "CBTBE_Punch_Target_Instability_Mod"; // int
        public const string PunchTargetInstabilityMulti = "CBTBE_Punch_Target_Instability_Multi"; // float

        public const string PunchIsPhysicalWeapon = "CBTBE_Punch_Is_Physical_Weapon"; // bool - if true, signals that this unit has a physical attack
        public const string PhysicalWeaponLocationTable = "CBTBE_Physical_Weapon_Location_Table"; // string Allows setting attack type to punch or standard
        public const string PhysicalWeaponAttackMod = "CBTBE_Physical_Weapon_Attack_Mod"; // int - a straight modifier to the attack roll
        public const string PhysicalWeaponExtraHitsCount = "CBTBE_Physical_Weapon_Extra_Hits_Count"; // float - a number of extra hits (using the calculated damage of the single strike) that will be applied. Will be rounded down.

        public const string PhysicalWeaponUnsteadyAttackerOnHit = "CBTBE_Physical_Weapon_Unsteady_Attacker_On_Hit"; // bool - if true, attacker will be unsteady on a hit
        public const string PhysicalWeaponUnsteadyAttackerOnMiss = "CBTBE_Physical_Weapon_Unsteady_Attacker_On_Miss"; // bool - if true, attacker will be unsteady on a miss
        public const string PhysicalWeaponUnsteadyTargetOnHit = "CBTBE_Physical_Weapon_Unsteady_Target_On_Hit"; // bool - if true, target will be made unsteady by a hit

        public const string PhysicalWeaponTargetDamage = "CBTBE_Physical_Weapon_Target_Damage_Per_Attacker_Ton"; // float - how much damage * attacker ton the weapon does
        public const string PhysicalWeaponTargetDamageMod = "CBTBE_Physical_Weapon_Target_Damage_Mod"; // int - flat modifier to damage
        public const string PhysicalWeaponTargetDamageMulti = "CBTBE_Physical_Weapon_Target_Damage_Multi"; // float - multiplier of base damage + damageMod

        public const string PhysicalWeaponTargetInstability = "CBTBE_Physical_Weapon_Target_Instability_Per_Attacker_Ton"; // float - how much stab / ton the weapon does
        public const string PhysicalWeaponTargetInstabilityMod = "CBTBE_Physical_Weapon_Target_Instability_Mod"; // int
        public const string PhysicalWeaponTargetInstabilityMulti = "CBTBE_Physical_Weapon_Target_Instability_Multi"; // float

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

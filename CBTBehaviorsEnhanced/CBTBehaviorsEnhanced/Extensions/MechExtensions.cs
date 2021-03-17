using BattleTech;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Extensions
{
    public static class MechExtensions
    {

        public static int ActuatorDamageMalus(this Mech mech)
        {
            int malus = 0;
            if (mech.StatCollection != null &&
                mech.StatCollection.ContainsStatistic(ModStats.ActuatorDamageMalus))
            {
                malus = mech.StatCollection.GetStatistic(ModStats.ActuatorDamageMalus).Value<int>();
            }
            return malus;
        }

        public static float PilotCheckMod(this Mech mech, float multi)
        {
            return CalculateCheckMod(mech, multi, false);
        }

        public static float HeatCheckMod(this Mech mech, float multi)
        {
            return CalculateCheckMod(mech, multi, true);
        }

        private static float CalculateCheckMod(Mech mech, float multi, bool gutsSkill)
        {

            int rawSkill = gutsSkill ? mech.SkillGuts : mech.SkillPiloting;
            int actorSkill = SkillUtils.NormalizeSkill(rawSkill);
            Mod.Log.Debug?.Write($"Actor: {CombatantUtils.Label(mech)} has rawSkill: {actorSkill} normalized to {actorSkill}");

            int malus = 0;
            if (!gutsSkill)
            {
                // Piloting checks must use damage malus
                malus += mech.ActuatorDamageMalus();
            }

            float adjustedSkill = actorSkill - malus > 0f ? actorSkill - malus : 0f;
            Mod.Log.Debug?.Write($"  AdjustedSkill: {adjustedSkill} = actorSkill: {actorSkill} - malus: {malus}.");

            float checkMod = adjustedSkill * multi;
            Mod.Log.Debug?.Write($"  CheckMod: {checkMod} = adjustedSkill: {adjustedSkill} * multi: {multi}");
            return checkMod;
        }

        public static float ChargeAttackerDamage(this Mech mech, float targetTonnage)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePerTargetTon * targetTonnage);
            Mod.MeleeLog.Debug?.Write($"Charge Attacker baseDamage: {Mod.Config.Melee.Charge.AttackerDamagePerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeAttackerDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeAttackerDamageMulti) : 1f;
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Attacker damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeTargetDamage(this Mech mech, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePerAttackerTon * mech.tonnage * hexesMoved);
            Mod.MeleeLog.Debug?.Write($"Charge Target baseDamage: {Mod.Config.Melee.Charge.TargetDamagePerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeTargetDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Target damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeAttackerInstability(this Mech mech, float targetTonnage, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon * targetTonnage * hexesMoved);
            Mod.MeleeLog.Debug?.Write($"Charge Attacker instability: {Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeAttackerInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeAttackerInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Attacker instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeTargetInstability(this Mech mech, float targetTonnage, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetInstabilityPerAttackerTon * mech.tonnage * hexesMoved);
            Mod.MeleeLog.Debug?.Write($"Charge Target instability: {Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeTargetInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFAAttackerDamage(this Mech mech, float targetTonnage)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerDamagePerTargetTon * targetTonnage);
            Mod.MeleeLog.Debug?.Write($"DFA Attacker baseDamage: {Mod.Config.Melee.DFA.AttackerDamagePerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Attacker damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFATargetDamage(this Mech mech)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetDamagePerAttackerTon * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"DFA Target baseDamage: {Mod.Config.Melee.DFA.TargetDamagePerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Target damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFAAttackerInstability(this Mech mech, float targetTonnage)
        {
            // Resolve attacker instability
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerInstabilityPerTargetTon * targetTonnage);
            Mod.MeleeLog.Debug?.Write($"DFA Attacker instability: {Mod.Config.Melee.DFA.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Attacker instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFATargetInstability(this Mech mech)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetInstabilityPerAttackerTon * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"DFA target instability: {Mod.Config.Melee.DFA.TargetInstabilityPerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float KickDamage(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanKick()) return 0;

            float raw = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetDamagePerAttackerTon * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"KICK baseDamage: {Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.KickTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.KickTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.KickTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.KickTargetDamageMulti) : 1f;

            // Leg actuator damage
            float leftLegReductionMulti = 1f;
            int damagedLeftActuators = 2 - attackerCondition.LeftLegActuatorsCount;
            for (int i = 0; i < damagedLeftActuators; i++) leftLegReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Left leg actuator damage multi is: {leftLegReductionMulti}");

            float rightLegReductionMulti = 1f;
            int damagedRightActuators = 2 - attackerCondition.RightLegActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightLegReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Right leg actuator damage multi is: {rightLegReductionMulti}");

            float actuatorMulti = leftLegReductionMulti >= rightLegReductionMulti ? leftLegReductionMulti : rightLegReductionMulti;
            Mod.MeleeLog.Debug?.Write($" - Using leg actuator damage multi of: {actuatorMulti}");

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

        public static float KickInstability(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanKick()) return 0;

            float raw = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"KICK baseStability: {Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon} x " +
                $"attacker tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.KickTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.KickTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.KickTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.KickTargetInstabilityMulti) : 1f;

            // Leg actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftCount = 2 - attackerCondition.LeftLegActuatorsCount;
            for (int i = 0; i < damagedLeftCount; i++) leftReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Left actuator damage multi is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRight = 2 - attackerCondition.RightLegActuatorsCount;
            for (int i = 0; i < damagedRight; i++) rightReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Right actuator damage multi is: {rightReductionMulti}");

            float actuatorMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeLog.Debug?.Write($" - Using actuator damage multi of: {actuatorMulti}");

            // Roll up instability
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeLog.Debug?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

        public static float PhysicalWeaponDamage(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanUsePhysicalAttack()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamage) &&
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamage) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamage) :
                Mod.Config.Melee.PhysicalWeapon.DefaultDamagePerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"PHYSICAL WEAPON damage => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamageMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float PhysicalWeaponInstability(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanUsePhysicalAttack()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstability) &&
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstability) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstability) :
                Mod.Config.Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"PHYSICAL WEAPON instability => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstabilityMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeLog.Debug?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float PunchDamage(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanPunch()) return 0;

            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamage) &&
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamage) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamage) :
                Mod.Config.Melee.Punch.TargetDamagePerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"PUNCH damage => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PunchTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamageMulti) : 1f;

            // Actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftActuators = 2 - attackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftActuators; i++) leftReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Left arm actuator damage is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRightActuators = 2 - attackerCondition.RightArmActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Right arm actuator damage is: {rightReductionMulti}");

            float reductionMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeLog.Debug?.Write($" - Using arm actuator damage reduction of: {reductionMulti}");

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi * reductionMulti);
            Mod.MeleeLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x reductionMulti: {reductionMulti}");

            return final;
        }

        public static float PunchInstability(this Mech mech, MechMeleeCondition attackerCondition)
        {
            if (!attackerCondition.CanPunch()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstability) &&
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstability) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstability) :
                Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeLog.Debug?.Write($"PUNCH instability => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PunchTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstabilityMulti) : 1f;

            // Leg actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftCount = 2 - attackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftCount; i++) leftReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Left actuator damage multi is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRightCount = 2 - attackerCondition.RightArmActuatorsCount;
            for (int i = 0; i < damagedRightCount; i++) rightReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeLog.Debug?.Write($" - Right actuator damage multi is: {rightReductionMulti}");

            float actuatorMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeLog.Debug?.Write($" - Using actuator damage multi of: {actuatorMulti}");

            // Roll up instability
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeLog.Info?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

    }

}

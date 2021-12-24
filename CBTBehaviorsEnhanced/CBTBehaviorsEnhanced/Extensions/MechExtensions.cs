using BattleTech;
using IRBTModUtils;
using IRBTModUtils.Extension;
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
            Mod.Log.Info?.Write($"Calculating pilot check modifier for actor: {CombatantUtils.Label(mech)}");
            return CalculateCheckMod(mech, multi, false);
        }

        public static float HeatCheckMod(this Mech mech, float multi)
        {
            Mod.Log.Info?.Write($"Calculating heat check modifier for actor: {CombatantUtils.Label(mech)}");
            return CalculateCheckMod(mech, multi, true);
        }

        private static float CalculateCheckMod(Mech mech, float multi, bool gutsSkill)
        {

            int rawSkill = gutsSkill ? mech.SkillGuts : mech.SkillPiloting;
            int actorSkill = gutsSkill ? SkillUtils.GetGutsModifier(mech.pilot) : SkillUtils.GetPilotingModifier(mech.pilot);
            Mod.Log.Debug?.Write($"Actor: {CombatantUtils.Label(mech)} has rawSkill: {actorSkill} normalized to {actorSkill}");

            int malus = 0;
            if (!gutsSkill)
            {
                // Piloting checks must use damage malus
                malus += mech.ActuatorDamageMalus();
            }

            float adjustedSkill = actorSkill - malus > 0f ? actorSkill - malus : 0f;
            Mod.Log.Info?.Write($"  AdjustedSkill: {adjustedSkill} = actorSkill: {actorSkill} - malus: {malus}.");

            float checkMod = adjustedSkill * multi;
            Mod.Log.Info?.Write($"  CheckMod: {checkMod} = adjustedSkill: {adjustedSkill} * multi: {multi}");
            return checkMod;
        }

        public static float ChargeAttackerDamage(this Mech mech, float targetTonnage)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePerTargetTon * targetTonnage);
            Mod.MeleeDamageLog.Debug?.Write($"Charge Attacker: {mech.DistinctId()} baseDamage: {Mod.Config.Melee.Charge.AttackerDamagePerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeAttackerDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeAttackerDamageMulti) : 1f;
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Attacker damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeTargetDamage(this Mech mech, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePerAttackerTon * mech.tonnage * hexesMoved);
            Mod.MeleeDamageLog.Debug?.Write($"Charge Target {mech.DistinctId()} baseDamage: {Mod.Config.Melee.Charge.TargetDamagePerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeTargetDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Target damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeAttackerInstability(this Mech mech, float targetTonnage, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon * targetTonnage * hexesMoved);
            Mod.MeleeDamageLog.Debug?.Write($"Charge Attacker {mech.DistinctId()} instability: {Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeAttackerInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeAttackerInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Attacker instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float ChargeTargetInstability(this Mech mech, float targetTonnage, int hexesMoved)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetInstabilityPerAttackerTon * mech.tonnage * hexesMoved);
            Mod.MeleeDamageLog.Debug?.Write($"Charge Target {mech.DistinctId()} instability: {Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} x hexesMoved: {hexesMoved} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.ChargeTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.ChargeTargetInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFAAttackerDamage(this Mech mech, float targetTonnage)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerDamagePerTargetTon * targetTonnage);
            Mod.MeleeDamageLog.Debug?.Write($"DFA Attacker {mech.DistinctId()} baseDamage: {Mod.Config.Melee.DFA.AttackerDamagePerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Attacker damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFATargetDamage(this Mech mech)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetDamagePerAttackerTon * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"DFA Target {mech.DistinctId()} baseDamage: {Mod.Config.Melee.DFA.TargetDamagePerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetDamageMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Target damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFAAttackerInstability(this Mech mech, float targetTonnage)
        {
            // Resolve attacker instability
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerInstabilityPerTargetTon * targetTonnage);
            Mod.MeleeDamageLog.Debug?.Write($"DFA Attacker {mech.DistinctId()} instability: {Mod.Config.Melee.DFA.AttackerInstabilityPerTargetTon} x " +
                $"target tonnage: {targetTonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Attacker instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float DFATargetInstability(this Mech mech)
        {
            float raw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetInstabilityPerAttackerTon * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"DFA target {mech.DistinctId()} instability: {Mod.Config.Melee.DFA.TargetInstabilityPerAttackerTon} x " +
                $"mech tonnage: {mech.tonnage} = {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetInstabilityMulti) : 1f;

            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float KickDamage(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanKick()) return 0;

            float raw = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetDamagePerAttackerTon * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"KICK {mech.DistinctId()} baseDamage: {Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon} x " +
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
            Mod.MeleeDamageLog.Debug?.Write($" - Left leg actuator damage multi is: {leftLegReductionMulti}");

            float rightLegReductionMulti = 1f;
            int damagedRightActuators = 2 - attackerCondition.RightLegActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightLegReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Right leg actuator damage multi is: {rightLegReductionMulti}");

            float actuatorMulti = leftLegReductionMulti >= rightLegReductionMulti ? leftLegReductionMulti : rightLegReductionMulti;
            Mod.MeleeDamageLog.Debug?.Write($" - Using leg actuator damage multi of: {actuatorMulti}");

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeDamageLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

        public static float KickInstability(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanKick()) return 0;

            float raw = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"KICK {mech.DistinctId()} baseStability: {Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon} x " +
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
            Mod.MeleeDamageLog.Debug?.Write($" - Left actuator damage multi is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRight = 2 - attackerCondition.RightLegActuatorsCount;
            for (int i = 0; i < damagedRight; i++) rightReductionMulti *= Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Right actuator damage multi is: {rightReductionMulti}");

            float actuatorMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeDamageLog.Debug?.Write($" - Using actuator damage multi of: {actuatorMulti}");

            // Roll up instability
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeDamageLog.Debug?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

        // PhysicalWeapon extensions
        public static float PhysicalWeaponDamage(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanUsePhysicalAttack()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamage) &&
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamage) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamage) :
                Mod.Config.Melee.PhysicalWeapon.DefaultDamagePerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"PHYSICAL WEAPON {mech.DistinctId()} damage => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamageMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static float PhysicalWeaponInstability(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanUsePhysicalAttack()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstability) &&
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstability) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstability) :
                Mod.Config.Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"PHYSICAL WEAPON {mech.DistinctId()} instability => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstabilityMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.MeleeDamageLog.Debug?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            return final;
        }

        public static bool CanMakePhysicalWeaponAttack(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);            
            return attackerCondition.CanUsePhysicalAttack();
        }

        public static float PunchDamage(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanPunch()) return 0;

            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamage) &&
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamage) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamage) :
                Mod.Config.Melee.Punch.TargetDamagePerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"PUNCH {mech.DistinctId()} damage => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamageMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PunchTargetDamageMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetDamageMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetDamageMulti) : 1f;

            // Actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftActuators = 2 - attackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftActuators; i++) leftReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Left arm actuator damage is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRightActuators = 2 - attackerCondition.RightArmActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Right arm actuator damage is: {rightReductionMulti}");

            float reductionMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeDamageLog.Debug?.Write($" - Using arm actuator damage reduction of: {reductionMulti}");

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi * reductionMulti);
            Mod.MeleeDamageLog.Debug?.Write($" - Target damage per strike => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x reductionMulti: {reductionMulti}");

            return final;
        }

        public static float PunchInstability(this Mech mech)
        {
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(mech);
            if (!attackerCondition.CanPunch()) return 0;

            // 0 is a signal that there's no divisor
            float tonnageMulti = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstability) &&
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstability) > 0 ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstability) :
                Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon;

            float raw = (float)Math.Ceiling(tonnageMulti * mech.tonnage);
            Mod.MeleeDamageLog.Debug?.Write($"PUNCH {mech.DistinctId()} instability => tonnageMulti: {tonnageMulti} x attacker tonnage: {mech.tonnage} = raw: {raw}");

            // Modifiers
            float mod = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstabilityMod) ?
                mech.StatCollection.GetValue<int>(ModStats.PunchTargetInstabilityMod) : 0f;
            float multi = mech.StatCollection.ContainsStatistic(ModStats.PunchTargetInstabilityMulti) ?
                mech.StatCollection.GetValue<float>(ModStats.PunchTargetInstabilityMulti) : 1f;

            // Leg actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftCount = 2 - attackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftCount; i++) leftReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Left actuator damage multi is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRightCount = 2 - attackerCondition.RightArmActuatorsCount;
            for (int i = 0; i < damagedRightCount; i++) rightReductionMulti *= Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.MeleeDamageLog.Debug?.Write($" - Right actuator damage multi is: {rightReductionMulti}");

            float actuatorMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.MeleeDamageLog.Debug?.Write($" - Using actuator damage multi of: {actuatorMulti}");

            // Roll up instability
            float final = (float)Math.Ceiling((raw + mod) * multi * actuatorMulti);
            Mod.MeleeDamageLog.Info?.Write($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x " +
                $"multi: {multi} x actuatorMulti: {actuatorMulti}");

            return final;
        }

        // AI Helper functions below

        // Return of MaxHeat means no limit from this effect
        public static int AcceptableHeatForAIFromVolatileAmmo(this Mech mech, float heatCheckMod, float bhvarAcceptableHeatFraction)
        {
            int acceptableHeat = Mod.Config.Heat.MaxHeat;

            Mod.AILog.Info?.Write($"-- Checking volatile ammo ");
            AmmunitionBox mostDamagingVolatile = HeatHelper.FindMostDamagingAmmoBox(mech, true);
            if (mostDamagingVolatile != null)
            {
                // We have volatile ammo, success chances will be lower because of the greater chance of an ammo explosion
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion)
                {
                    if (kvp.Value == -1f)
                    {
                        // Guaranteed explosion, return one less than this value
                        acceptableHeat = kvp.Key - 1;
                        break;
                    }

                    float rawExplosionChance = Math.Max(0f, kvp.Value - heatCheckMod);
                    Mod.AILog.Info?.Write($"  heat: {kvp.Key} has rawChance: {kvp.Value} - pilotMod: {heatCheckMod} => raw explosionChance: {rawExplosionChance}");
                    float successChance = 1.0f - rawExplosionChance;
                    float compoundChance = successChance * successChance;
                    float finalExplosionChance = 1.0f - compoundChance;
                    Mod.AILog.Info?.Write($"  1.0f - compoundChance: {compoundChance} => finalExplosionChance: {finalExplosionChance}");
                    if (finalExplosionChance <= bhvarAcceptableHeatFraction)
                        acceptableHeat = kvp.Key;
                    else
                        break;

                }
            }
            else
            {
                Mod.AILog.Info?.Write($"  No volatile ammo, skipping");
            }

            Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from volatile ammo due to behVar: {bhvarAcceptableHeatFraction}.");
            return acceptableHeat;
        }

        // Return of MaxHeat means no limit from this effect
        public static int AcceptableHeatForAIFromRegularAmmo(this Mech mech, float heatCheckMod, float bhvarAcceptableHeatFraction)
        {
            int acceptableHeat = Mod.Config.Heat.MaxHeat;

            Mod.AILog.Info?.Write($"-- Checking regular ammo ");
            AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(mech, false);
            if (mostDamaging != null)
            {
                // We have regular ammo, so success chances are as on tin
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion)
                {
                    if (kvp.Value == -1f)
                    {
                        // Guaranteed explosion, return one less than this value
                        acceptableHeat = kvp.Key - 1;
                        break;
                    }

                    float explosionChance = Math.Max(0f, kvp.Value - heatCheckMod);
                    Mod.AILog.Info?.Write($"  heat: {kvp.Key} has rawChance: {kvp.Value} - pilotMod: {heatCheckMod} => explosionChance: {explosionChance}");
                    if (explosionChance <= bhvarAcceptableHeatFraction)
                        acceptableHeat = kvp.Key;
                    else
                        break;

                }
            }
            else
            {
                Mod.AILog.Info?.Write($"  No regular ammo, skipping");
            }

            Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from regular ammo due to behVar: {bhvarAcceptableHeatFraction}.");
            return acceptableHeat;
        }

        // Return of MaxHeat means no limit from this effect
        public static int AcceptableHeatForAIFromShutdown(this Mech mech, float heatCheckMod, float bhvarAcceptableHeatFraction)
        {
            int acceptableHeat = Mod.Config.Heat.MaxHeat;
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Shutdown)
            {
                if (kvp.Value == -1f)
                {
                    // Guaranteed shutdown, return one less than this value
                    acceptableHeat = kvp.Key - 1;
                    break;
                }

                float shutdownChance = Math.Max(0f, kvp.Value - heatCheckMod);
                if (shutdownChance <= bhvarAcceptableHeatFraction)
                    acceptableHeat = kvp.Key;
                else
                    break;

            }

            Mod.AILog.Info?.Write($"Unit: {mech.DistinctId()} has acceptableHeat: {acceptableHeat} from shutdown due to behVar: {bhvarAcceptableHeatFraction}.");
            return acceptableHeat;
        }
    }

}

using BattleTech;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Objects
{
    public class PhysicalWeaponMeleeState : MeleeState
    {
        // Per BT Manual pg.38,
        //   * target takes 1 pt. each 4-10 tons of attacker, rounded up (varies by weapon)
        //   * One attack
        //   * Resolves on main table
        //   *   Optional - Can resolve on punch table 
        //   *   Optional - Can resolve on kick table 
        //   * Requires a shoulder actuator AND hand actuator
        //   *   +2 to hit if lower or upper arm actuator missing
        //   *   -2 modifier if target is prone

        public PhysicalWeaponMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target,
            HashSet<MeleeAttackType> validAnimations) : base(attacker)
        {
            Mod.Log.Info($"Buliding PHYSICAL WEAPON state for attacker: {CombatantUtils.Label(attacker)} @ attackPos: {attackPos} vs. target: {CombatantUtils.Label(target)}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Physical_Weapon];
            this.IsValid = ValidateAttack(attacker, target, validAnimations);
            if (IsValid)
            {

                CalculateDamages(attacker, target);
                CalculateInstability(attacker, target);
                CalculateModifiers(attacker, target);
                CreateDescriptions(attacker, target);

                // Damage tables 
                this.AttackerTable = DamageTable.NONE;
                this.TargetTable = DamageTable.STANDARD;
                if (attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponLocationTable))
                {
                    string tableName = attacker.StatCollection.GetValue<string>(ModStats.PhysicalWeaponLocationTable).ToUpper();
                    if (tableName.Equals("KICK")) this.TargetTable = DamageTable.KICK;
                    else if (tableName.Equals("PUNCH")) this.TargetTable = DamageTable.PUNCH;
                    else if (tableName.Equals("STANDARD")) this.TargetTable = DamageTable.STANDARD;
                }

                // Unsteady
                this.UnsteadyAttackerOnHit = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponUnsteadyAttackerOnHit) ?
                    attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnHit) : 
                    Mod.Config.Melee.PhysicalWeapon.DefaultUnsteadyAttackerOnHit;

                this.UnsteadyAttackerOnMiss = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponUnsteadyAttackerOnMiss) ?
                    attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnMiss) :
                    Mod.Config.Melee.PhysicalWeapon.DefaultUnsteadyAttackerOnMiss;

                this.UnsteadyTargetOnHit = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponUnsteadyTargetOnHit) ?
                    attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyTargetOnHit) :
                    Mod.Config.Melee.PhysicalWeapon.DefaultUnsteadyTargetOnHit;

                // Set the animation type
                this.AttackAnimation = MeleeAttackType.Punch;
            }
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // Check that unit has a physical attack
            if (!attacker.StatCollection.ContainsStatistic(ModStats.PunchIsPhysicalWeapon) ||
                !attacker.StatCollection.GetValue<bool>(ModStats.PunchIsPhysicalWeapon))
            {
                Mod.Log.Info("Unit has no physical weapon by stat; skipping.");
                return false;
            }

            // If no punch - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Punch))
            {
                Mod.Log.Info("Animations do not include a punch, cannot use physical weapon.");
                return false;
            }

            // Damage check - shoulder and hand
            bool leftArmIsFunctional = this.AttackerCondition.LeftShoulderIsFunctional && this.AttackerCondition.LeftHandIsFunctional;
            bool rightArmIsFunctional = this.AttackerCondition.RightShoulderIsFunctional && this.AttackerCondition.RightHandIsFunctional;
            if (!leftArmIsFunctional && !rightArmIsFunctional)
            {
                Mod.Log.Info("Both arms are inoperable due to shoulder and hand actuator damage. Cannot use a physical weapon!");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch
            float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
            float maxWalkSpeed = MechHelper.FinalWalkSpeed(attacker);
            if (distance > maxWalkSpeed)
            {
                Mod.Log.Info($"Attack distance of {distance} is greater than attacker walkSpeed: {maxWalkSpeed}. Cannot use physical weapon!");
                return false;
            }

            Mod.Log.Info("PHYSICAL WEAPON ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {

            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Physical_Weapon_Desc],
                new object[] {
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            // If target is prone, -2 modifier
            if (target.IsProne)
                this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

            // +2 to hit for each upper/lower actuator hit
            int leftArmMalus = (2 - this.AttackerCondition.LeftArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            int rightArmMalus = (2 - this.AttackerCondition.RightArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int bestMalus = leftArmMalus >= rightArmMalus ? leftArmMalus : rightArmMalus;
            if (bestMalus != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Acutator_Damage, bestMalus);
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating PHYSICAL WEAPON damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            // 0 is a signal that there's no divisor
            float divisor = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageTonnageDivisor) && 
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamageTonnageDivisor) > 0 ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamageTonnageDivisor) : 
                Mod.Config.Melee.PhysicalWeapon.DefaultDamagePerAttackTon;

            float raw = (float)Math.Ceiling(divisor * attacker.tonnage);
            Mod.Log.Info($" - divisor: {divisor} x attacker tonnage: {attacker.tonnage} = raw: {raw}");

            // Modifiers
            float mod = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMod) ?
                attacker.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetDamageMod) : 0f;
            float multi = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetDamageMulti) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetDamageMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.Log.Info($" - Target damage => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");

            // Target damage applies as a single modifier
            this.TargetDamageClusters = new float[] { final };
        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating PHYSICAL WEAPON instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            // 0 is a signal that there's no divisor
            float divisor = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityTonnageDivisor) &&
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstabilityTonnageDivisor) > 0 ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstabilityTonnageDivisor) :
                Mod.Config.Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon;

            float raw = (float)Math.Ceiling(divisor * attacker.tonnage);
            Mod.Log.Info($" - divisor: {divisor} x attacker tonnage: {attacker.tonnage} = raw: {raw}");

            // Modifiers
            float mod = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMod) ?
                attacker.StatCollection.GetValue<int>(ModStats.PhysicalWeaponTargetInstabilityMod) : 0f;
            float multi = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponTargetInstabilityMulti) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponTargetInstabilityMulti) : 1f;

            // Roll up final damage
            float final = (float)Math.Ceiling((raw + mod) * multi);
            Mod.Log.Info($" - Target instability => final: {final} = (raw: {raw} + mod: {mod}) x multi: {multi}");
            
            this.TargetInstability = final;
        }
    }
}

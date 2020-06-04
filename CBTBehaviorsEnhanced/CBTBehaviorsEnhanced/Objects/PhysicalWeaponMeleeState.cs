using BattleTech;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
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
                this.ForceUnsteadyOnAttacker = false;
                this.ForceUnsteadyOnTarget = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponAppliesUnsteady) ? 
                    attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponAppliesUnsteady) : Mod.Config.Melee.PhysicalWeapon.DefaultAttackAppliesUnsteady;
            }
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
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

            // Check that unit has a physical attack
            if (!attacker.StatCollection.ContainsStatistic(ModStats.PunchIsPhysicalWeapon) || 
                attacker.StatCollection.GetValue<bool>(ModStats.PunchIsPhysicalWeapon))
            {
                Mod.Log.Info("Unit has no physical weapon by stat; skipping.");
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
            int sumTargetDamage = this.TargetDamageClusters.Count() > 0 ?
                (int)Math.Ceiling(this.TargetDamageClusters.Sum()) : 0;
            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Physical_Weapon_Desc],
                new object[] {
                    sumTargetDamage, this.TargetInstability
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            // If target is prone, -2 modifier
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
            Mod.Log.Info($"Calculating physical weapon for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float divisor = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponDamageDivisor) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponDamageDivisor) : 
                Mod.Config.Melee.PhysicalWeapon.DefaultDamagePerAttackTon;

            float baseTargetDamage = (float)Math.Ceiling(divisor * attacker.tonnage);
            Mod.Log.Info($" - Target baseDamage: {divisor} x " +
                $"attacker tonnage: {attacker.tonnage} = {baseTargetDamage}");

            // Modifiers
            float damageMod = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponDamageMod) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponDamageMod) : 0f;
            float damageMulti = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponDamageMulti) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponDamageMulti) : 1f;

            // Roll up final damage
            float finalTargetDamage = (float)Math.Ceiling((baseTargetDamage + damageMod) * damageMulti);
            Mod.Log.Info($" - Target finalDamage: {finalTargetDamage} = (baseDamage: {baseTargetDamage} + damageMod: {damageMod}) x " +
                $"damageMulti: {damageMulti}");

            // Target damage applies as a single modifier
            this.TargetDamageClusters = new float[] { finalTargetDamage };
        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float divisor = attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponInstabilityDivisor) ?
                attacker.StatCollection.GetValue<float>(ModStats.PhysicalWeaponInstabilityDivisor) :
                Mod.Config.Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon;

            this.TargetInstability = (float)Math.Ceiling(divisor * attacker.tonnage);
            Mod.Log.Info($" - Target takes {divisor} instability x " +
                $"target tonnage {attacker.tonnage} = {this.TargetInstability}");

        }
    }
}

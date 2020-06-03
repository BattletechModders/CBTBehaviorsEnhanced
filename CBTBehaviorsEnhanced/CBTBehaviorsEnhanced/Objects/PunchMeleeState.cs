using BattleTech;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Objects
{
    public class PunchMeleeState : MeleeState
    {
        // Per BT Manual pg.38,
        //   * target takes 1 pt. each 10 tons of attacker, rounded up
        //   *   x0.5 damage for each missing upper & lower actuator
        //   * Resolves on punch table
        //   * Requires a shoulder actuator
        //   *   +1 to hit if hand actuator missing
        //   *   +2 to hit if lower arm actuator missing
        //   *   -2 modifier if target is prone

        public PunchMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target,
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
                this.TargetTable = DamageTable.PUNCH;

                // Unsteady
                this.ForceUnsteadyOnAttacker = false;
                this.ForceUnsteadyOnTarget = Mod.Config.Melee.Punch.AttackAppliesUnsteady;

            }
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // If we cannot punch - not a valid attack
            if (!validAnimations.Contains(MeleeAttackType.Punch) )
            {
                Mod.Log.Info("Animations do not include a punch, attacker cannot punch!");
                return false;
            }

            // Damage check - both shoulders damaged invalidate us
            if (!this.AttackerCondition.LeftShoulderIsFunctional && !this.AttackerCondition.RightShoulderIsFunctional)
            {
                Mod.Log.Info("Both shoulder actuators are damaged. Cannot punch!");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch
            float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
            float maxWalkSpeed = MechHelper.FinalWalkSpeed(attacker);
            if (distance > maxWalkSpeed)
            {
                Mod.Log.Info($"Attack distance of {distance} is greater than attacker walkSpeed: {maxWalkSpeed}. Cannot punch!");
                return false;
            }

            Mod.Log.Info("PUNCH ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            int sumAttackerDamage = this.AttackerDamageClusters.Count() > 0 ?
                (int)Math.Ceiling(this.AttackerDamageClusters.Sum()) : 0;
            int sumTargetDamage = this.TargetDamageClusters.Count() > 0 ?
                (int)Math.Ceiling(this.TargetDamageClusters.Sum()) : 0;
            string localText = new Text(
                Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Punch_Desc],
                new object[] {
                    sumAttackerDamage, this.AttackerInstability, sumTargetDamage, this.TargetInstability
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
 
            // If target is prone, -2 modifier
            string localText = new Text(
                Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Target_Prone]
                ).ToString();
            this.AttackModifiers.Add(Mod.Config.Melee.ProneTargetAttackModifier, localText);

            // Actuator damage; +1 for arm actuator, +2 to hit for each upper/lower actuator hit
            int leftArmMalus = (2 - this.AttackerCondition.LeftArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!this.AttackerCondition.LeftHandIsFunctional) leftArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int rightArmMalus = (2 - this.AttackerCondition.RightArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!this.AttackerCondition.RightHandIsFunctional) rightArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int bestMalus = leftArmMalus >= rightArmMalus ? leftArmMalus : rightArmMalus;
            if (bestMalus != 0)
            {
                localText = new Text(
                    Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Acutator_Damage]
                    ).ToString();
                this.AttackModifiers.Add(bestMalus, localText);
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating punch damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            Mod.Log.Info($" - Attacker tonnage is: {attacker.tonnage}");

            float baseTargetDamage = (float)Math.Ceiling(Mod.Config.Melee.Punch.TargetDamagePerAttackerTon * attacker.tonnage);
            Mod.Log.Info($" - Target baseDamage: {Mod.Config.Melee.Punch.TargetDamagePerAttackerTon} x " +
                $"attacker tonnage: {attacker.tonnage} = {baseTargetDamage}");

            // Modifiers
            float damageMod = attacker.StatCollection.ContainsStatistic(ModStats.PunchDamageMod) ?
                attacker.StatCollection.GetValue<float>(ModStats.PunchDamageMod) : 0f;
            float damageMulti = attacker.StatCollection.ContainsStatistic(ModStats.PunchDamageMulti) ?
                attacker.StatCollection.GetValue<float>(ModStats.PunchDamageMulti) : 1f;

            // Actuator damage
            float leftReductionMulti = 1f;
            int damagedLeftActuators = 2 - this.AttackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftActuators; i++) leftReductionMulti += Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.Log.Info($" - Left arm actuator damage is: {leftReductionMulti}");

            float rightReductionMulti = 1f;
            int damagedRightActuators = 2 - this.AttackerCondition.RightArmActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightReductionMulti += Mod.Config.Melee.Punch.ArmActuatorDamageReduction;
            Mod.Log.Info($" - Right arm actuator damage is: {rightReductionMulti}");

            float reductionMulti = leftReductionMulti >= rightReductionMulti ? leftReductionMulti : rightReductionMulti;
            Mod.Log.Info($" - Using arm actuator damage reduction of: {reductionMulti}");

            // Roll up final damage
            float finalTargetDamage = (float)Math.Ceiling((baseTargetDamage + damageMod) * damageMulti * reductionMulti);
            Mod.Log.Info($" - Target finalDamage: {finalTargetDamage} = (baseDamage: {baseTargetDamage} + damageMod: {damageMod}) x " +
                $"damageMulti: {damageMulti} x reductionMulti: {reductionMulti}");

            // Target damage applies as a single modifier
            this.TargetDamageClusters = new float[] { finalTargetDamage };
        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            this.TargetInstability = (float)Math.Ceiling(Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon * attacker.tonnage);
            Mod.Log.Info($" - Target takes {Mod.Config.Melee.Punch.TargetInstabilityPerAttackerTon} instability x " +
                $"target tonnage {attacker.tonnage} = {this.TargetInstability}");

        }
    }
}

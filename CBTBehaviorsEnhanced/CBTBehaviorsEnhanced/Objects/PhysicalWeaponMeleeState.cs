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
        //   * x0.5 damage for each missing upper & lower actuator

        public PhysicalWeaponMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target,
            HashSet<MeleeAttackType> validAnimations) : base(attacker)
        {
            this.IsValid = ValidateAttack(target, validAnimations);
            if (IsValid)
            {
                this.AttackerTable = target.IsProne ? DamageTable.REAR : DamageTable.KICK;

                CalculateDamages(attacker, target);
                CalculateInstability(attacker, target);
                CalculateModifiers(attacker, target);
                CreateDescriptions(attacker, target);
            }
        }

        private bool ValidateAttack(AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // If neither kick (mech) or stomp (vehicle) - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Kick) && !validAnimations.Contains(MeleeAttackType.Stomp))
            {
                Mod.Log.Info("Animations do not include a kick or stomp, cannot kick.");
                return false;
            }

            // Damage check - left leg
            if (!this.AttackerCondition.LeftHipIsFunctional || !this.AttackerCondition.RightFootIsFunctional)
            {
                Mod.Log.Info("One or more hip actuators are damaged. Cannot kick!");
                return false;
            }

            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            int sumAttackerDamage = this.AttackerDamageClusters.Count() > 0 ?
                (int)Math.Ceiling(this.AttackerDamageClusters.Sum()) : 0;
            int sumTargetDamage = this.TargetDamageClusters.Count() > 0 ?
                (int)Math.Ceiling(this.TargetDamageClusters.Sum()) : 0;
            string localText = new Text(
                Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Kick_Desc],
                new object[] {
                    sumAttackerDamage, this.AttackerInstability, sumTargetDamage, this.TargetInstability
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            // -2 to hit base
            string localText = new Text(
                Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_ComparativeSkill_Piloting]
                ).ToString();
            this.AttackModifiers.Add(Mod.Config.Melee.Kick.BaseAttackBonus, localText);

            // If target is prone, -2 modifier
            localText = new Text(
                Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Target_Prone]
                ).ToString();
            this.AttackModifiers.Add(Mod.Config.Melee.ProneTargetAttackModifier, localText);

            // Actuator damage; +1 for foot actuator, +2 to hit for each upper/lower actuator hit
            int leftLegMalus = (2 - this.AttackerCondition.LeftLegActuatorsCount) * Mod.Config.Melee.Kick.LegActuatorDamageMalus;
            if (!this.AttackerCondition.LeftFootIsFunctional) leftLegMalus += Mod.Config.Melee.Kick.FootActuatorDamageMalus;

            int rightLegMalus = (2 - this.AttackerCondition.RightLegActuatorsCount) * Mod.Config.Melee.Kick.LegActuatorDamageMalus;
            if (!this.AttackerCondition.RightFootIsFunctional) rightLegMalus += Mod.Config.Melee.Kick.FootActuatorDamageMalus;

            int bestLegMalus = leftLegMalus >= rightLegMalus ? leftLegMalus : rightLegMalus;
            if (bestLegMalus != 0)
            {
                localText = new Text(
                    Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Acutator_Damage]
                    ).ToString();
                this.AttackModifiers.Add(bestLegMalus, localText);
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating kick damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            Mod.Log.Info($" - Attacker tonnage is: {attacker.tonnage}");

            float baseTargetDamage = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetDamagePerAttackerTon * attacker.tonnage);
            Mod.Log.Info($" - Target baseDamage: {Mod.Config.Melee.Kick.TargetDamagePerAttackerTon} x " +
                $"attacker tonnage: {attacker.tonnage} = {baseTargetDamage}");

            // Modifiers
            float damageMod = attacker.StatCollection.ContainsStatistic(ModStats.KickDamageMod) ?
                attacker.StatCollection.GetValue<float>(ModStats.KickDamageMod) : 0f;
            float damageMulti = attacker.StatCollection.ContainsStatistic(ModStats.KickDamageMulti) ?
                attacker.StatCollection.GetValue<float>(ModStats.KickDamageMulti) : 1f;

            // Leg actuator damage
            float leftLegReductionMulti = 1f;
            int damagedLeftActuators = 2 - this.AttackerCondition.LeftArmActuatorsCount;
            for (int i = 0; i < damagedLeftActuators; i++) leftLegReductionMulti += Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.Log.Info($" - Left leg actuator damage is: {leftLegReductionMulti}");

            float rightLegReductionMulti = 1f;
            int damagedRightActuators = 2 - this.AttackerCondition.RightLegActuatorsCount;
            for (int i = 0; i < damagedRightActuators; i++) rightLegReductionMulti += Mod.Config.Melee.Kick.LegActuatorDamageReduction;
            Mod.Log.Info($" - Right leg actuator damage is: {rightLegReductionMulti}");

            float legReductionMulti = leftLegReductionMulti >= rightLegReductionMulti ? leftLegReductionMulti : rightLegReductionMulti;
            Mod.Log.Info($" - Using leg actuator damage reduction of: {legReductionMulti}");

            // Roll up final damage
            float finalTargetDamage = (float)Math.Ceiling((baseTargetDamage + damageMod) * damageMulti * legReductionMulti);
            Mod.Log.Info($" - Target finalDamage: {finalTargetDamage} = (baseDamage: {baseTargetDamage} + damageMod: {damageMod}) x " +
                $"damageMulti: {damageMulti} x legReductionMulti: {legReductionMulti}");

            // Target damage applies as a single modifier
            this.TargetDamageClusters = new float[] { finalTargetDamage };
        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            this.TargetInstability = (float)Math.Ceiling(Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon * attacker.tonnage);
            Mod.Log.Info($" - Target takes {Mod.Config.Melee.Kick.TargetInstabilityPerAttackerTon} instability x " +
                $"target tonnage {attacker.tonnage} = {this.TargetInstability}");

        }
    }
}

using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using IRBTModUtils.Extension;
using Localize;
using System.Collections.Generic;
using System.Text;
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
            Mod.Log.Info($"Building PUNCH state for attacker: {attacker.DistinctId()} @ attackPos: {attackPos} vs. target: {target.DistinctId()}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Punch];
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
                this.UnsteadyAttackerOnHit = Mod.Config.Melee.Punch.UnsteadyAttackerOnHit;
                this.UnsteadyAttackerOnMiss = Mod.Config.Melee.Punch.UnsteadyAttackerOnMiss;
                this.UnsteadyTargetOnHit = Mod.Config.Melee.Punch.UnsteadyTargetOnHit;

                // Set the animation type
                this.AttackAnimation = validAnimations.Contains(MeleeAttackType.Punch) ? MeleeAttackType.Punch : MeleeAttackType.Tackle;
            }
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // If we cannot punch - not a valid attack
            if (!validAnimations.Contains(MeleeAttackType.Punch) && !(validAnimations.Contains(MeleeAttackType.Tackle)))
            {
                Mod.Log.Info("Animations do not include a punch or tackle, attacker cannot punch!");
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
            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Punch_Desc],
                new object[] {
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
 
            // If target is prone, -2 modifier
            if (target.IsProne) this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

            // Actuator damage; +1 for arm actuator, +2 to hit for each upper/lower actuator hit
            int leftArmMalus = (2 - this.AttackerCondition.LeftArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!this.AttackerCondition.LeftHandIsFunctional) leftArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int rightArmMalus = (2 - this.AttackerCondition.RightArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!this.AttackerCondition.RightHandIsFunctional) rightArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int bestMalus = leftArmMalus >= rightArmMalus ? leftArmMalus : rightArmMalus;
            if (bestMalus != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Acutator_Damage, bestMalus);
            }

            // Check for attack modifier statistic
            if (attacker.StatCollection.ContainsStatistic(ModStats.PunchAttackMod) &&
                attacker.StatCollection.GetValue<int>(ModStats.PunchAttackMod) != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Punch_Attack_Mod, attacker.StatCollection.GetValue<int>(ModStats.PunchAttackMod));
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating PUNCH damage for attacker: {CombatantUtils.Label(attacker)} @ {attacker.tonnage} tons " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float damage = attacker.PunchDamage(this.AttackerCondition);

            // Target damage applies as a single hit
            this.TargetDamageClusters = AttackHelper.CreateDamageClustersWithExtraAttacks(attacker, damage, ModStats.PunchExtraHitsCount);
            StringBuilder sb = new StringBuilder(" - Target damage clusters: ");
            foreach (float cluster in this.TargetDamageClusters)
            {
                sb.Append(cluster);
                sb.Append(", ");
            }
            Mod.Log.Info(sb.ToString());

        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.Log.Info($"Calculating PUNCH instability for attacker: {CombatantUtils.Label(attacker)} @ {attacker.tonnage} tons " +
                $"vs. target: {CombatantUtils.Label(target)}");

            this.TargetInstability = attacker.PunchInstability(this.AttackerCondition);

        }
    }
}

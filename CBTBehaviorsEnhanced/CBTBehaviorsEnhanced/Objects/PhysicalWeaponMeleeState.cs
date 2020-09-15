using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Localize;
using System.Collections.Generic;
using System.Text;
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
            Mod.MeleeLog.Info?.Write($"Building PHYSICAL WEAPON state for attacker: {CombatantUtils.Label(attacker)} @ attackPos: {attackPos} vs. target: {CombatantUtils.Label(target)}");

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
                Mod.MeleeLog.Info?.Write("Unit has no physical weapon by stat; skipping.");
                return false;
            }

            // If no punch - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Punch))
            {
                Mod.MeleeLog.Info?.Write("Animations do not include a punch, cannot use physical weapon.");
                return false;
            }

            // Damage check - shoulder and hand
            bool leftArmIsFunctional = this.AttackerCondition.LeftShoulderIsFunctional && this.AttackerCondition.LeftHandIsFunctional;
            bool rightArmIsFunctional = this.AttackerCondition.RightShoulderIsFunctional && this.AttackerCondition.RightHandIsFunctional;
            if (!leftArmIsFunctional && !rightArmIsFunctional)
            {
                Mod.MeleeLog.Info?.Write("Both arms are inoperable due to shoulder and hand actuator damage. Cannot use a physical weapon!");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch
            float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
            float maxWalkSpeed = MechHelper.FinalWalkSpeed(attacker);
            if (distance > maxWalkSpeed)
            {
                Mod.MeleeLog.Info?.Write($"Attack distance of {distance} is greater than attacker walkSpeed: {maxWalkSpeed}. Cannot use physical weapon!");
                return false;
            }

            Mod.MeleeLog.Info?.Write("PHYSICAL WEAPON ATTACK validated");
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

            int bestMalus = leftArmMalus <= rightArmMalus ? leftArmMalus : rightArmMalus;
            if (bestMalus != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Actuator_Damage, bestMalus);
            }

            // Check for attack modifier statistic
            if (attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponAttackMod) &&
                attacker.StatCollection.GetValue<int>(ModStats.PhysicalWeaponAttackMod) != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Physical_Weapon_Attack_Mod, attacker.StatCollection.GetValue<int>(ModStats.PhysicalWeaponAttackMod));
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating PHYSICAL WEAPON damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float damage = attacker.PhysicalWeaponDamage(this.AttackerCondition);

            // Target damage applies as a single modifier
            this.TargetDamageClusters = AttackHelper.CreateDamageClustersWithExtraAttacks(attacker, damage, ModStats.PhysicalWeaponExtraHitsCount);
            StringBuilder sb = new StringBuilder(" - Target damage clusters: ");
            foreach (float cluster in this.TargetDamageClusters)
            {
                sb.Append(cluster);
                sb.Append(", ");
            }
            Mod.MeleeLog.Info?.Write(sb.ToString());

        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating PHYSICAL WEAPON instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            this.TargetInstability = attacker.PhysicalWeaponInstability(this.AttackerCondition);
        }
    }
}

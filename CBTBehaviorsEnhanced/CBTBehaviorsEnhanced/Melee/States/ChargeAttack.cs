using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CustAmmoCategories;
using CustomComponents;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public class ChargeAttack : MeleeAttack
    {
        // Per BT Manual pg.35,
        //   * attacker takes 1 pt. each 10 tons of target, rounded up
        //   * defender takes 1 pt. each 10 tons of attacker * # hexes moved, rounded up
        //   * Groupings by 5 (25) points
        //   * Resolves on regular table
        //   * Prone targets cannot be attacked
        //   * Attacker makes PSR with +4, target with +2 to avoid falling
        //	 *   Create instab damage instead
        //   * Modifier for attack is comparative skill

        public ChargeAttack(MeleeState state) : base(state)
        {
            Mod.MeleeLog.Info?.Write($"Building CHARGE state for attacker: {CombatantUtils.Label(state.attacker)} @ attackPos: {state.attackPos} vs. target: {CombatantUtils.Label(state.target)}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge];
            this.IsValid = ValidateAttack(state.attacker, state.target, state.validAnimations, state.skipValidatePathing);
            if (IsValid)
            {
                float distance = (state.attacker.CurrentPosition - state.target.CurrentPosition).magnitude;
                int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
                Mod.MeleeLog.Info?.Write($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

                this.UsePilotingDelta = Mod.Config.Melee.Charge.UsePilotingDelta;

                CalculateDamages(state.attacker, state.target, hexesMoved);
                CalculateInstability(state.attacker, state.target, hexesMoved);
                CalculateModifiers(state.attacker, state.target);
                CreateDescriptions(state.attacker, state.target);

                // Damage tables 
                this.AttackerTable = DamageTable.STANDARD;
                this.TargetTable = DamageTable.STANDARD;

                // Unsteady
                this.UnsteadyAttackerOnHit = Mod.Config.Melee.Charge.UnsteadyAttackerOnHit;
                this.UnsteadyAttackerOnMiss = Mod.Config.Melee.Charge.UnsteadyAttackerOnMiss;

                this.OnTargetMechHitForceUnsteady = Mod.Config.Melee.Charge.UnsteadyTargetOnHit;
                this.OnTargetVehicleHitEvasionPipsRemoved = Mod.Config.Melee.Charge.TargetVehicleEvasionPipsRemoved;

                // Set the animation type
                if (state.target is Vehicle) this.AttackAnimation = MeleeAttackType.Stomp;
                else this.AttackAnimation = MeleeAttackType.Tackle;
            }
        }

        public override bool IsRangedWeaponAllowed(Weapon weapon)
        {
            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_NeverMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed as it can never be used in melee");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_AlwaysMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} marked with AlwaysMelee category, force-enabling");
                return true;
            }

            return false;
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations, bool skipValidatePathing)
        {
            ActorMeleeCondition meleeCondition = ModState.GetMeleeCondition(attacker);
            if (!meleeCondition.CanCharge())
            {
                Mod.MeleeLog.Info?.Write($"Attacker cannot charge, skipping.");
                return false;
            }

            // If neither tackle (mech) or stomp (vehicle) - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Tackle) && !validAnimations.Contains(MeleeAttackType.Stomp))
            {
                Mod.MeleeLog.Info?.Write("Animations do not include a tackle or stomp, attacker cannot charge!");
                return false;
            }

            // Charges cannot target prone units
            if (target.IsProne)
            {
                Mod.MeleeLog.Info?.Write($"Attacker unable to charge prone target");
                return false;
            }

            if (target.UnaffectedPathing())
            {
                Mod.MeleeLog.Info?.Write($"Target is unaffected by pathing, likely a VTOL or LAM in flight. Cannot melee it!");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch            
            if (!skipValidatePathing && !state.HasSprintAttackNodes)
            {
                Mod.MeleeLog.Info?.Write($"No sprinting nodes found for melee attack!");
                return false;
            }

            Mod.MeleeLog.Info?.Write("CHARGE ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            float[] adjAttackerDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.AttackerDamageClusters, attacker, attacker.MeleeWeapon);
            float[] adjTargetDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.TargetDamageClusters, target, attacker.MeleeWeapon);

            string attackerDamageS = adjAttackerDamage.Count() > 1 ?
                $"{adjAttackerDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjAttackerDamage)})" :
                adjAttackerDamage[0].ToString();
            string targetDamageS = adjTargetDamage.Count() > 1 ?
                $"{adjTargetDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjTargetDamage)})" :
                adjTargetDamage[0].ToString();

            string attackTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Standard];
            
            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Charge_Desc],
                new object[] {
                    targetDamageS, this.TargetInstability, attackTable, 
                    attackerDamageS, this.AttackerInstability, attackTable
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            if (this.UsePilotingDelta)
            {
                // Build the comparative skill level
                int comparativeSkill = (attacker.SkillPiloting - target.SkillPiloting) * -1;
                Mod.MeleeLog.Info?.Write($"Comparative skill = {comparativeSkill} => attacker {CombatantUtils.Label(attacker)} @ piloting: {attacker.SkillPiloting} " +
                    $"vs. target: {CombatantUtils.Label(target)} @ piloting: {target.SkillPiloting} ");

                this.AttackModifiers.Add(ModText.LT_Label_ComparativeSkill_Piloting, comparativeSkill);
            }

            // Check for attack modifier statistic
            if (attacker.StatCollection.ContainsStatistic(ModStats.ChargeAttackMod) &&
                attacker.StatCollection.GetValue<int>(ModStats.ChargeAttackMod) != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Charge_Attack_Mod, attacker.StatCollection.GetValue<int>(ModStats.ChargeAttackMod));
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target, int hexesMoved)
        {
            Mod.MeleeLog.Info?.Write($"Calculating CHARGE damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

            float targetTonnage = 0;
            if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
            else if (target is Mech mech) targetTonnage = mech.tonnage;
            else if (target is Turret turret) targetTonnage = turret.MeleeTonnage();
            Mod.MeleeLog.Info?.Write($" - Tonnage => Attacker: {attacker.tonnage}  Target: {targetTonnage}");

            // Calculate attacker damage
            float attackerDamage = attacker.ChargeAttackerDamage(targetTonnage, hexesMoved);
            attackerDamage = attacker.ApplyChargeDamageReduction(attackerDamage);
            DamageHelper.ClusterDamage(attackerDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.AttackerDamageClusters);

            // Resolve target damage - include movement!
            float targetDamage = attacker.ChargeTargetDamage(hexesMoved);
            targetDamage = target.ApplyChargeDamageReduction(targetDamage);
            DamageHelper.ClusterDamage(targetDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.TargetDamageClusters);
        }

        private void CalculateInstability(Mech attacker, AbstractActor target, int hexesMoved)
        {
            Mod.MeleeLog.Info?.Write($"Calculating CHARGE instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

            float targetTonnage = 0;
            if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
            else if (target is Mech mech) targetTonnage = mech.tonnage;
            Mod.MeleeLog.Info?.Write($" - Target tonnage is: {targetTonnage}");

            // Resolve attacker instability
            float attackerInstab = attacker.ChargeAttackerInstability(targetTonnage, hexesMoved);
            attackerInstab = attacker.ApplyChargeInstabReduction(attackerInstab);
            this.AttackerInstability = attackerInstab;

            // Resolve target instability
            float targetInstab = attacker.ChargeTargetInstability(targetTonnage, hexesMoved);
            targetInstab = target.ApplyChargeInstabReduction(targetInstab);
            this.TargetInstability = targetInstab;

        }
    }
}

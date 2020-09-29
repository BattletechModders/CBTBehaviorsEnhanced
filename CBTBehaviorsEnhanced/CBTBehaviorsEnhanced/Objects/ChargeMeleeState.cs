using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Objects
{
    public class ChargeMeleeState : MeleeState
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

        public ChargeMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target, 
			HashSet<MeleeAttackType> validAnimations) : base(attacker)
        {
			Mod.MeleeLog.Info?.Write($"Building CHARGE state for attacker: {CombatantUtils.Label(attacker)} @ attackPos: {attackPos} vs. target: {CombatantUtils.Label(target)}");

			this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge];
            this.IsValid = ValidateAttack(attacker, target, validAnimations);
			if (IsValid)
			{
				float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
				int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
				Mod.MeleeLog.Info?.Write($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

				this.UsePilotingDelta = Mod.Config.Melee.Charge.UsePilotingDelta;

				CalculateDamages(attacker, target, hexesMoved);
				CalculateInstability(attacker, target, hexesMoved);
				CalculateModifiers(attacker, target);
				CreateDescriptions(attacker, target);

				// Damage tables 
				this.AttackerTable = DamageTable.STANDARD;
				this.TargetTable = DamageTable.STANDARD;

				// Unsteady
				this.UnsteadyAttackerOnHit = Mod.Config.Melee.Charge.UnsteadyAttackerOnHit;
				this.UnsteadyAttackerOnMiss = Mod.Config.Melee.Charge.UnsteadyAttackerOnMiss;
				this.UnsteadyTargetOnHit = Mod.Config.Melee.Charge.UnsteadyTargetOnHit;

				// Set the animation type
				if (target is Vehicle) this.AttackAnimation = MeleeAttackType.Stomp;
				else this.AttackAnimation = MeleeAttackType.Tackle;
			}
		}

		public override bool IsRangedWeaponAllowed(Weapon weapon)
		{
			return false;
		}

		private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations)
		{
			// If neither tackle (mech) or stomp (vehicle) - we're not a valid attack.
			if (!validAnimations.Contains(MeleeAttackType.Tackle) && !validAnimations.Contains(MeleeAttackType.Stomp))
			{
				Mod.MeleeLog.Info?.Write("Animations do not include a tackle or stomp, attacker cannot charge!");
				return false;
			}

			// If attacker is unsteady - cannot charge
			if (attacker.IsUnsteady)
			{
				Mod.MeleeLog.Info?.Write($"Attacker unable to charge target while unsteady.");
				return false;
			}

			// Charges cannot target prone units
			if (target.IsProne)
			{
				Mod.MeleeLog.Info?.Write($"Attacker unable to charge prone target");
				return false;
			}


			Mod.MeleeLog.Info?.Write("CHARGE ATTACK validated");
			return true;
		}

		private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
			int sumAttackerDamage = this.AttackerDamageClusters.Count() > 0 ?
				(int)Math.Ceiling(this.AttackerDamageClusters.Sum()) : 0;
			string localText = new Text(
				Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Charge_Desc], 
				new object[] { 
					sumAttackerDamage, this.AttackerInstability
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
			Mod.MeleeLog.Info?.Write($" - Tonnage => Attacker: {attacker.tonnage}  Target: {targetTonnage}");

			// Calculate attacker damage
			float attackerDamage = attacker.ChargeAttackerDamage(targetTonnage);
			DamageHelper.ClusterDamage(attackerDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.AttackerDamageClusters);

			// Resolve target damage - include movement!
			float targetDamage = attacker.ChargeTargetDamage(hexesMoved);
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
			this.AttackerInstability = attackerInstab;

			// Resolve target instability
			float targetInstab = attacker.ChargeTargetInstability(targetTonnage, hexesMoved);
			this.TargetInstability = targetInstab;

		}
	}
}

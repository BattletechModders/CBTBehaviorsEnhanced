using BattleTech;
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
			this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge];
            this.IsValid = ValidateAttack(target, validAnimations);
			if (IsValid)
			{
				float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
				int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
				Mod.Log.Info($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

				CalculateDamages(attacker, target, hexesMoved);
				CalculateInstability(attacker, target, hexesMoved);
				CalculateModifiers(attacker, target);
				CreateDescriptions(attacker, target);

				// Damage tables 
				this.AttackerTable = DamageTable.STANDARD;
				this.TargetTable = DamageTable.STANDARD;

				// Unsteady
				this.ForceUnsteadyOnAttacker = Mod.Config.Melee.Charge.AttackAppliesUnsteady;
				this.ForceUnsteadyOnTarget = Mod.Config.Melee.Charge.AttackAppliesUnsteady;

				// Set the animation type
				if (target is Vehicle) this.AttackAnimation = MeleeAttackType.Stomp;
				else this.AttackAnimation = MeleeAttackType.Tackle;
			}
		}

        private bool ValidateAttack(AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // If neither tackle (mech) or stomp (vehicle) - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Tackle) && !validAnimations.Contains(MeleeAttackType.Stomp)) return false;

			// Charges cannot target prone units
			if (target.IsProne) return false;

			Mod.Log.Info("CHARGE ATTACK validated");
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
			// Build the comparative skill level
			int comparativeSkill = (attacker.SkillPiloting - target.SkillPiloting) * -1;
			Mod.Log.Info($"Comparative skill = {comparativeSkill} => attacker {CombatantUtils.Label(attacker)} @ piloting: {attacker.SkillPiloting} " +
				$"vs. target: {CombatantUtils.Label(target)} @ piloting: {target.SkillPiloting} ");

			this.AttackModifiers.Add(ModText.LT_Label_ComparativeSkill_Piloting, comparativeSkill);
		}

		private void CalculateDamages(Mech attacker, AbstractActor target, int hexesMoved)
        {
			Mod.Log.Info($"Calculating CHARGE damage for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Tonnage => Attacker: {attacker.tonnage}  Target: {targetTonnage}");

			// Calculate attacker damage
			float attackerRaw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePerTargetTon * targetTonnage);

			// Modifiers
			float attackerMod = attacker.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.ChargeAttackerDamageMod) : 0f;
			float attackerMulti = attacker.StatCollection.ContainsStatistic(ModStats.ChargeAttackerDamageMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.ChargeAttackerDamageMulti) : 1f;
			float attackerFinal = (float)Math.Ceiling((attackerRaw + attackerMod) * attackerMulti);
			Mod.Log.Info($" - Attacker damage => final: {attackerFinal} = (raw: {attackerRaw} + mod: {attackerMod}) x multi: {attackerMulti}");

			// split attacker damage into clusters
			DamageHelper.ClusterDamage(attackerRaw, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.AttackerDamageClusters);

			// Resolve target damage - include movement!
			float targetRaw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePerAttackerTon * attacker.tonnage * hexesMoved);

			// Modifiers
			float targetMod = attacker.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.ChargeTargetDamageMod) : 0f;
			float targetMulti = attacker.StatCollection.ContainsStatistic(ModStats.ChargeTargetDamageMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.ChargeTargetDamageMulti) : 1f;

			float targetFinal = (float)Math.Ceiling((targetRaw + targetMod) * targetMulti);
			Mod.Log.Info($" - Target damage => final: {targetFinal} = (raw: {targetRaw} + mod: {targetMod}) x multi: {targetMulti}");

			// split target damage into clusters
			DamageHelper.ClusterDamage(targetFinal, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.TargetDamageClusters);
		}

		private void CalculateInstability(Mech attacker, AbstractActor target, int hexesMoved)
		{
			Mod.Log.Info($"Calculating CHARGE instability for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}");

			// Resolve attacker instability
			float attackerRaw = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon * targetTonnage * hexesMoved);

			// Modifiers
			float attackerMod = attacker.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.ChargeAttackerInstabilityMod) : 0f;
			float attackerMulti = attacker.StatCollection.ContainsStatistic(ModStats.ChargeAttackerInstabilityMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.ChargeAttackerInstabilityMulti) : 1f;

			float attackerFinal = (float)Math.Ceiling((attackerRaw + attackerMod) * attackerMulti);
			Mod.Log.Info($" - Attacker instability => final: {attackerFinal} = (raw: {attackerRaw} + mod: {attackerMod}) x multi: {attackerMulti}");
			this.AttackerInstability = attackerFinal;

			// Resolve target instability
			float targetRaw = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetInstabilityPerAttackerTon * targetTonnage * hexesMoved);

			// Modifiers
			float targetMod = target.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMod) ?
				target.StatCollection.GetValue<int>(ModStats.ChargeTargetInstabilityMod) : 0f;
			float targetMulti = target.StatCollection.ContainsStatistic(ModStats.ChargeTargetInstabilityMulti) ?
				target.StatCollection.GetValue<float>(ModStats.ChargeTargetInstabilityMulti) : 1f;

			float targetFinal = (float)Math.Ceiling((targetRaw + targetMod) * targetMulti);
			Mod.Log.Info($" - target instability => final: {targetFinal} = (raw: {targetRaw} + mod: {targetMod}) x multi: {targetMulti}");
			this.TargetInstability = targetFinal;

		}
	}
}

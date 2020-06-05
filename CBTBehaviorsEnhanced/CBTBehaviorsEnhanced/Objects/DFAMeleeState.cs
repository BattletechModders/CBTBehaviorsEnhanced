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
    public class DFAMeleeState : MeleeState
    {
		// Per BT Manual pg.36,
		//   * target takes 1 pt. each 10 tons of attacker, which is then multiplied by 3 and rounded up
		//   * attacker takes tonnage / 5, rounded up
		//   * Damage clustered in 5 (25) point groupings for both attacker & defender
		//   * Target resolves on punch table
		//   *   Prone targets resolve on rear table
		//   * Attacker resolves on kick table
		//   * Comparative attack modifier; difference in attacker and defender is applied to attack
		//   *  +3 modifier to hit for jumping
		//   *  +2 to hit if upper or lower leg actuators are hit
		//   *  -2 modifier if target is prone
		//   * Attacker makes PSR with +4, target with +2 and fall

		public DFAMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target, 
			HashSet<MeleeAttackType> validAnimations) : base(attacker)
        {
			this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_DeathFromAbove];
			this.IsValid = ValidateAttack(target, validAnimations);
			if (IsValid)
			{
				float distance = (attacker.CurrentPosition - target.CurrentPosition).magnitude;
				int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
				Mod.Log.Info($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

				this.AttackerTable = DamageTable.STANDARD;
				this.TargetTable = DamageTable.STANDARD;
				CalculateDamages(attacker, target, hexesMoved);
				CalculateInstability(attacker, target, hexesMoved);
				CalculateModifiers(attacker, target);
				CreateDescriptions(attacker, target);

				// Set the animation type
				this.AttackAnimation = MeleeAttackType.DFA;
			}
		}

        private bool ValidateAttack(AbstractActor target, HashSet<MeleeAttackType> validAnimations)
        {
            // If neither tackle (mech) or stomp (vehicle) - we're not a valid attack.
            if (!validAnimations.Contains(MeleeAttackType.Tackle) && !validAnimations.Contains(MeleeAttackType.Stomp)) return false;

			// Charges cannot target prone units
			if (target.IsProne) return false;

			// No damage check - by rules, you can charge anytime?
			Mod.Log.Info(" - Attacker can DFA");
			return true;
		}

		private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
			int sumAttackerDamage = this.AttackerDamageClusters.Count() > 0 ?
				(int)Math.Ceiling(this.AttackerDamageClusters.Sum()) : 0;
			int sumTargetDamage = this.TargetDamageClusters.Count() > 0 ?
				(int)Math.Ceiling(this.TargetDamageClusters.Sum()) : 0;
			string localText = new Text(
				Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_DFA_Desc], 
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
			Mod.Log.Info($"Calculating charge damage for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

			// Resolve attacker damage
			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}");

			float attackerDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePerTargetTon * targetTonnage);
			Mod.Log.Info($" - Attacker takes {Mod.Config.Melee.Charge.AttackerDamagePerTargetTon} damage x " +
				$"target tonnage {targetTonnage} = {attackerDamage}");

			// split attacker damage into clusters
			DamageHelper.ClusterDamage(attackerDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.AttackerDamageClusters);

			// Now resolve target damage
			Mod.Log.Info($" - Attacker tonnage is: {attacker.tonnage}");

			float baseTargetDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePerAttackerTon * attacker.tonnage * hexesMoved);
			Mod.Log.Info($" - Target baseDamage: {Mod.Config.Melee.Charge.TargetDamagePerAttackerTon} x " +
				$"attacker tonnage: {attacker.tonnage} x hexesMoved: {hexesMoved} = {baseTargetDamage}");

			// Modifiers
			float damageMod = attacker.StatCollection.ContainsStatistic(ModStats.ChargeDamageMod) ?
				attacker.StatCollection.GetValue<float>(ModStats.ChargeDamageMod) : 0f;
			float damageMulti = attacker.StatCollection.ContainsStatistic(ModStats.ChargeDamageMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.ChargeDamageMulti) : 1f;

			float finalTargetDamage = (float)Math.Ceiling((baseTargetDamage + damageMod) * damageMulti);
			Mod.Log.Info($" - Target finalDamage: {finalTargetDamage} = (baseDamage: {baseTargetDamage} + damageMod: {damageMod}) x damageMulti: {damageMulti}");

			// split target damage into clusters
			DamageHelper.ClusterDamage(finalTargetDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.TargetDamageClusters);
		}

		private void CalculateInstability(Mech attacker, AbstractActor target, int hexesMoved)
		{
			Mod.Log.Info($"Calculating instability for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)} at hexesMoved: {hexesMoved}");

			// Resolve attacker instability
			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}");

			this.AttackerInstability = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon * targetTonnage * hexesMoved);
			Mod.Log.Info($" - Attacker takes {Mod.Config.Melee.Charge.AttackerInstabilityPerTargetTon} instability x " +
				$"target tonnage {targetTonnage} = {this.AttackerInstability}");
			
			this.TargetInstability = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetInstabilityPerAttackerTon * attacker.tonnage * hexesMoved);
			Mod.Log.Info($" - Target takes {Mod.Config.Melee.Charge.TargetInstabilityPerAttackerTon} instability x " +
				$"target tonnage {attacker.tonnage} = {this.TargetInstability}");

		}
	}
}

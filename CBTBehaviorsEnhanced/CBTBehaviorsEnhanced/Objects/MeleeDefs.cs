using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using CustomComponents;
using System;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced
{
    public class MeleeAttackDef
    {
        public MeleeAttackType Type;
        public ChassisLocations Limb;
    }

	public class MeleeState
	{
		public bool CanCharge { get; set; }
		public bool CanKick { get; set; }
		public bool CanPunch { get; set; }

		// Place to store modifiers to current effects, to display in the description?
		public string ChargeDescNotes = "";
		public string KickDescNotes = "";
		public string PunchDescNotes = "";

		// Total damage 
		public float[] ChargeDamageAttacker;
		public float[] ChargeDamageTarget;
        public float[] KickDamage;
        public float[] PunchDamage;

		// Damage multipliers and effects
		public bool LeftHipIsFunctional = false;
        public int LeftLegActuatorsCount = 0;
		public bool LeftFootIsFunctional = false;

		public bool RightHipIsFunctional = false;
		public int RightLegActuatorsCount = 0;
		public bool RightFootIsFunctional = false;

		public bool LeftShoulderIsFunctional = false;
		public int LeftArmActuatorsCount = 0;
		public bool LeftHandIsFunctional = false;

		public bool RightShoulderIsFunctional = false;
        public int RightArmActuatorsCount = 0;
		public bool RightHandIsFunctional = false;

		// Assumes any modifiers are set; call after EvaluateDamage
		public void CalculateChargeDamage(Mech attackerMech, AbstractActor target, float distance)
        {
			Mod.Log.Info($"Calculating charge damage for attacker: {CombatantUtils.Label(attackerMech)} " +
				$"vs. target: {CombatantUtils.Label(target)} at distance: {distance}");
			// Per BT Manual pg.35,
			//   * attacker takes 1 pt. each 10 tons of target, rounded up
			//   * defender takes 1 pt. each 10 tons of attacker * # hexes moved, rounded up
			//   * Groupings by 5 (25) points
			//   * Resolves on regular table
			//   * Prone targets cannot be attacked
			//   * Attacker makes PSR with +4, target with +2 to avoid falling
			//   * Modifier for attack is comparative skill

			// Resolve attacker damage
			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}");
			
			float attackerDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePerTargetTon * targetTonnage);
			Mod.Log.Info($" - Attacker takes {Mod.Config.Melee.Charge.AttackerDamagePerTargetTon} damage x " +
				$"target tonnage {targetTonnage} = {attackerDamage}");

			// split attacker damage into clusters
			DamageHelper.ClusterDamage(attackerDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.ChargeDamageAttacker);

			// Now resolve target damage
			Mod.Log.Info($" - Attacker tonnage is: {attackerMech.tonnage}");

			int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
			Mod.Log.Info($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

			float baseTargetDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePerAttackerTon * attackerMech.tonnage * hexesMoved);
			Mod.Log.Info($" - Target baseDamage: {Mod.Config.Melee.Charge.TargetDamagePerAttackerTon} x " +
				$"attacker tonnage: {attackerMech.tonnage} x hexesMoved: {hexesMoved} = {baseTargetDamage}");

			// Modifiers
			float damageMod = attackerMech.StatCollection.ContainsStatistic(ModStats.ChargeDamageMod) ?
				attackerMech.StatCollection.GetValue<float>(ModStats.ChargeDamageMod) : 0f;
			float damageMulti = attackerMech.StatCollection.ContainsStatistic(ModStats.ChargeDamageMulti) ?
				attackerMech.StatCollection.GetValue<float>(ModStats.ChargeDamageMulti) : 1f;
			float finalTargetDamage = (baseTargetDamage + damageMod) * damageMulti;
			Mod.Log.Info($" - Target finalDamage: {finalTargetDamage} = (baseDamage: {baseTargetDamage} + damageMod: {damageMod}) x damageMulti: {damageMulti}");

			// split target damage into clusters
			DamageHelper.ClusterDamage(finalTargetDamage, Mod.Config.Melee.Charge.DamageClusterDivisor, out this.ChargeDamageTarget);
		}


		public void CalculateDFA(Mech attackerMech, AbstractActor target, float distance)
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
		}

		public void CalculateKick(Mech attackerMech, AbstractActor target, float distance)
        {
			// Per BT Manual pg.38,
			//   * target takes 1 pt. each 5 tons of attacker, rounded up
			//   * One attack
			//   * Normally resolves on kick table
			//   * Prone targets resolve on rear 
			//   * -2 to hit base
			//   *   +1 for foot actuator, +2 to hit for each upper/lower actuator hit
			//   *   -2 modifier if target is prone
			//   * x0.5 damage for each missing leg actuator
		}
		public void CalculatePhysicalAttack(Mech attackerMech, AbstractActor target, float distance)
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
		}

		public void CalculatePunch(Mech attackerMech, AbstractActor target, float distance)
        {
			// Per BT Manual pg.38,
			//   * target takes 1 pt. each 10 tons of attacker, rounded up
			//   * One attack per arm
			//   * Resolves on punch table
			//   *   Prone targets resolve on rear
			//   * Requires a shoulder actuator, requires a hand actuator
			//   *   +1 to hit if hand actuator missing
			//   *   +2 to hit if lower arm actuator missing
			//   *   -2 modifier if target is prone
			//   * x0.5 damage for each missing upper & lower actuator

		}

		public void EvaluateCondition(Mech attackerMech)
        {
			Mod.Log.Info($"Building possible attacks from current attacker damage state:");
			foreach (MechComponent mc in attackerMech.allComponents)
			{
				switch (mc.Location)
                {
					case (int)ChassisLocations.LeftArm:
					case (int)ChassisLocations.RightArm:
						EvaluateArmComponent(mc);
						break;
					case (int)ChassisLocations.LeftLeg:
					case (int)ChassisLocations.RightLeg:
						EvaluateLegComponent(mc);
						break;
					default:
						break;
				}
			}
		}

		private void EvaluateLegComponent(MechComponent mc)
		{
			Mod.Log.Info($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");
			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.HipActuatorCategoryId))
			{
				if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftHipIsFunctional = mc.IsFunctional;
				else this.RightHipIsFunctional = mc.IsFunctional;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.UpperLegActuatorCategoryId))
			{
				int mod = mc.IsFunctional ? 1 : 0;
				if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftLegActuatorsCount += mod;
				else this.RightLegActuatorsCount += mod;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.LowerLegActuatorCategoryId))
			{
				int mod = mc.IsFunctional ? 1 : 0;
				if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftLegActuatorsCount += mod;
				else this.RightLegActuatorsCount += mod;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.FootActuatorCategoryId))
			{
				if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftFootIsFunctional = mc.IsFunctional;
				else this.RightFootIsFunctional = mc.IsFunctional;
			}
		}

		private void EvaluateArmComponent(MechComponent mc)
		{
			Mod.Log.Info($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");
			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.ShoulderActuatorCategoryId))
			{
				if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftShoulderIsFunctional = mc.IsFunctional;
				else this.RightShoulderIsFunctional = mc.IsFunctional;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.UpperArmActuatorCategoryId))
			{
				int mod = mc.IsFunctional ? 1 : 0;
				if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftArmActuatorsCount += mod;
				else this.RightArmActuatorsCount += mod;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.LowerArmActuatorCategoryId))
			{
				int mod = mc.IsFunctional ? 1 : 0;
				if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftArmActuatorsCount += mod;
				else this.RightArmActuatorsCount += mod;
			}

			if (mc.mechComponentRef.IsCategory(Mod.Config.CustomCategories.HandActuatorCategoryId))
			{
				if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftHandIsFunctional = mc.IsFunctional;
				else this.RightHandIsFunctional = mc.IsFunctional;
			}
		}
	}
}

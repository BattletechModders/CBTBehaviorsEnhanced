using BattleTech;
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
        public bool HipsAreFunctional = true;
        public int LegActuatorsCount = 0;
        public bool LeftShoulderIsFunctional = true;
        public int LeftArmActuatorsCount = 0; 
        public bool RightShoulderIsFunctional = true;
        public int RightArmActuatorsCount = 0; 

		// Assumes any modifiers are set; call after EvaluateDamage
		public void CalculateChargeDamage(Mech attackerMech, AbstractActor target, float distance)
        {
			Mod.Log.Info($"Calculating charge damage for attacker: {CombatantUtils.Label(attackerMech)} " +
				$"vs. target: {CombatantUtils.Label(target)} at distance: {distance}");
			// Per BT Manual pg.35,
			//   * attacker takes 1 pt. each 10 tons of target 
			//   * defender takes 1 pt. each 10 tons of attacker * # hexes moved
			//   * All damage rounded up
			//   * Groupings by 5 points

			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			float targetTonnageFraction = targetTonnage / 10.0f;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}, tonnage fraction is: {targetTonnageFraction}");
			
			float attackerDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.AttackerDamagePer10TonsOfTarget * targetTonnageFraction);
			Mod.Log.Info($" - Attacker takes {Mod.Config.Melee.Charge.AttackerDamagePer10TonsOfTarget} damage x " +
				$"target tonnage fraction {targetTonnageFraction} = {attackerDamage}");

			float attackerTonnageFraction = attackerMech.tonnage / 10.0f;
			Mod.Log.Info($" - Attacker tonnage is: {attackerMech.tonnage}, tonnage fraction is: {attackerTonnageFraction}");

			int hexesMoved = (int)Math.Ceiling(distance / Mod.Config.Move.MPMetersPerHex);
			Mod.Log.Info($" - Hexes moved is {hexesMoved} = distance: {distance} / MPMetersPerHex: {Mod.Config.Move.MPMetersPerHex}");

			float targetDamage = (float)Math.Ceiling(Mod.Config.Melee.Charge.TargetDamagePer10TonsOfAttacker * attackerTonnageFraction * hexesMoved);
			Mod.Log.Info($" - Target takes {Mod.Config.Melee.Charge.TargetDamagePer10TonsOfAttacker} damage x " +
				$"attacker tonnage fraction: {attackerTonnageFraction} x hexesMoved: {hexesMoved} = {targetDamage}");

		}

		public void CalculateKickDamage()
        {

        }

		public void CalculatePunchDamage()
        {

        }

        public void EvaluateDamage(Mech attackerMech)
        {
			Mod.Log.Info($"Building possible attacks from current attacker damage state:");
			foreach (MechComponent mc in attackerMech.allComponents)
			{
				// Check hips
				if (mc.mechComponentRef.IsCategory(Mod.Config.Melee.HipActuatorCategoryId) && !mc.IsFunctional)
				{
					Mod.Log.Info($"  - Hip Actuator: {mc.Description.UIName} is damaged, cannot kick!");
					this.HipsAreFunctional = false;
				}

				// Check leg actuators
				foreach (string actuatorCatId in Mod.Config.Melee.LegActuatorDamageMultiByCategoryId.Keys)
				{
					if (mc.mechComponentRef.IsCategory(actuatorCatId) && mc.IsFunctional)
					{
						Mod.Log.Info($"  - Leg Actuator: {mc.Description.UIName} is functional.");
						this.LegActuatorsCount++;
					}
				}

				// Check shoulders
				if (mc.mechComponentRef.IsCategory(Mod.Config.Melee.ShoulderActuatorCategoryId) && !mc.IsFunctional)
				{
					Mod.Log.Info($"  - Shoulder Actuator: {mc.Description.UIName} in location: {mc.Location} is damaged, that arm cannot punch.");
					if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftShoulderIsFunctional = false;
					else if (mc.Location == (int)ChassisLocations.RightArm) this.RightShoulderIsFunctional = false;
					else
					{
						Mod.Log.Warn($"Shoulder Actuator: {mc.Description.UIName} found outside of arms... is this intended?!?");
					}
				}

				// Check arm actuators
				foreach (string actuatorCatId in Mod.Config.Melee.ArmActuatorDamageMultiByCategoryId.Keys)
				{
					if (mc.mechComponentRef.IsCategory(actuatorCatId) && mc.IsFunctional)
					{
						Mod.Log.Info($"  - Arm Actuator: {mc.Description.UIName} is functional in location: {mc.Location}");
						if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftArmActuatorsCount++;
						else if (mc.Location == (int)ChassisLocations.RightArm) this.RightArmActuatorsCount++;
					}
				}
			}
		}
    }
}

using BattleTech;
using CustomComponents;

namespace CBTBehaviorsEnhanced
{
    public class MechMeleeCondition
    {
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

		private Mech Attacker;

		public MechMeleeCondition(Mech attacker)
        {
			Mod.MeleeLog.Debug?.Write($"Building possible attacks from current attacker damage state:");
			foreach (MechComponent mc in attacker.allComponents)
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

			Attacker = attacker;
		}

		public bool CanKick()
        {
			// Can't kick with damaged hip actuators
			if (!LeftHipIsFunctional || !RightHipIsFunctional) return false;
			
			return true;
		}

		public bool CanUsePhysicalAttack()
        {
			// Check that unit has a physical attack
			if (!Attacker.StatCollection.ContainsStatistic(ModStats.PunchIsPhysicalWeapon) ||
				!Attacker.StatCollection.GetValue<bool>(ModStats.PunchIsPhysicalWeapon))
			{
				return false;
			}

			// Damage check - shoulder and hand
			bool leftArmIsFunctional = LeftShoulderIsFunctional && LeftHandIsFunctional;
			bool rightArmIsFunctional = RightShoulderIsFunctional && RightHandIsFunctional;
			if (!leftArmIsFunctional && !rightArmIsFunctional)
			{
				return false;
			}

			return true;
		}

		public bool CanPunch()
        {
			// Can't punch with damaged shoulders
			if (!LeftShoulderIsFunctional && !RightShoulderIsFunctional) return false;

			return true;
		}

		private void EvaluateLegComponent(MechComponent mc)
		{
			Mod.MeleeLog.Debug?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");
			
			foreach (string categoryId in Mod.Config.CustomCategories.HipActuatorCategoryId)
            {
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftHipIsFunctional = mc.IsFunctional;
					else this.RightHipIsFunctional = mc.IsFunctional;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.UpperLegActuatorCategoryId)
            {
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftLegActuatorsCount += mod;
					else this.RightLegActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.LowerLegActuatorCategoryId)
            {
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftLegActuatorsCount += mod;
					else this.RightLegActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.FootActuatorCategoryId)
            {
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.LeftFootIsFunctional = mc.IsFunctional;
					else this.RightFootIsFunctional = mc.IsFunctional;
					break;
				}
			}

		}

		private void EvaluateArmComponent(MechComponent mc)
		{
			Mod.MeleeLog.Debug?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");

			foreach (string categoryId in Mod.Config.CustomCategories.ShoulderActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftShoulderIsFunctional = mc.IsFunctional;
					else this.RightShoulderIsFunctional = mc.IsFunctional;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.UpperArmActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftArmActuatorsCount += mod;
					else this.RightArmActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.LowerArmActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftArmActuatorsCount += mod;
					else this.RightArmActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.HandActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftArm) this.LeftHandIsFunctional = mc.IsFunctional;
					else this.RightHandIsFunctional = mc.IsFunctional;
					break;
				}
			}
		}
	}
}

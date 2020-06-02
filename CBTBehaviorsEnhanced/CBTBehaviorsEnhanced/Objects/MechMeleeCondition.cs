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

		public MechMeleeCondition(Mech attacker)
        {
			Mod.Log.Debug($"Building possible attacks from current attacker damage state:");
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
		}

		private void EvaluateLegComponent(MechComponent mc)
		{
			Mod.Log.Debug($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");
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
			Mod.Log.Debug($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");
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

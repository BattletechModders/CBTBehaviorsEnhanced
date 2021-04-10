using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Helper
{

	public static class DamageHelper
    {
		public static void ClusterDamage(float totalDamage, float divisor, out float[] clusteredDamage)
        {
			List<float> clusters = new List<float>();
			while (totalDamage > 0)
			{
				if (totalDamage > divisor)
				{
					clusters.Add(divisor);
					totalDamage -= divisor;
				}
				else
				{
					clusters.Add(totalDamage);
					totalDamage = 0;
				}
			}
			clusteredDamage = clusters.ToArray();
		}
    }

    public static class MeleeHelper
    {

		// This assumes you're calling from a place that has already determined that we can reach the target.
		public static MeleeState GetMeleeStates(AbstractActor attacker, Vector3 attackPos, ICombatant target)
        {
			if (attacker == null || target == null)
            {
				Mod.MeleeLog.Warn?.Write("Null attacker or target - cannot melee!");
				return new MeleeState();
			}
			
			Mech attackerMech = attacker as Mech;
			if (attackerMech == null)
			{
				Mod.MeleeLog.Warn?.Write("Vehicles and buildings cannot melee!");
				return new MeleeState();
			}

			AbstractActor targetActor = target as AbstractActor;
			if (targetActor == null)
			{
				Mod.MeleeLog.Error?.Write("Target is not an abstractactor - must be building. Cannot melee!");
				return new MeleeState();
			}


			Mod.MeleeLog.Info?.Write($"Building melee state for attacker: {CombatantUtils.Label(attacker)} against target: {CombatantUtils.Label(target)}");



			MeleeState states = new MeleeState(attackerMech, attackPos, targetActor);
			Mod.MeleeLog.Info?.Write($" - valid attacks => charge: {states.Charge.IsValid}  dfa: {states.DFA.IsValid}  kick: {states.Kick.IsValid}  " +
				$"weapon: {states.PhysicalWeapon.IsValid}  punch: {states.Punch.IsValid}");

			return states;
			
        }

  

	}
}

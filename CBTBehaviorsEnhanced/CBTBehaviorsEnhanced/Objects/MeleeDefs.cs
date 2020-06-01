using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using System;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced
{
    public class MeleeAttackDef
    {
        public MeleeAttackType Type;
        public ChassisLocations Limb;
    }

	public class MeleeStateF
	{
		// Total damage 


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

		
	}
}

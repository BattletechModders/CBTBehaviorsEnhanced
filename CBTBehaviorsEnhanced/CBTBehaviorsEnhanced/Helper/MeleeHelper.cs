using CBTBehaviorsEnhanced.MeleeStates;
using IRBTModUtils.Extension;
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

        public static string ClusterDamageStringForUI(float[] clusterDamage)
        {
            if (clusterDamage == null || clusterDamage.Length == 0) return "0";
            if (clusterDamage.Length == 1) return clusterDamage[0].ToString();

            // Do this fucking complicated bullshit to deal with TT stupidity
            float remainder = clusterDamage[clusterDamage.Length - 1];
            float cluster = clusterDamage[clusterDamage.Length - 2];
            if (cluster == remainder)
                return $"{cluster}x{clusterDamage.Length}";
            else
                return $"{clusterDamage[0]}x{clusterDamage.Length - 1} + {remainder}";
        }
        public static float[] AdjustDamageByTargetTypeForUI(float[] damage, AbstractActor target, Weapon meleeWeapon)
        {
            if (damage == null || damage.Length == 0) return new float[] { };
            float[] adjusted = new float[damage.Length];

            float multi = 1f;
            if (UnitHelper.IsVehicle(target))
            {
                multi = meleeWeapon.WeaponCategoryValue.VehicleDamageMultiplier;
                Mod.UILog.Debug?.Write($"Target: {target.DistinctId()} is a vehicle, using melee damage multipler: {multi}");
            }
            else if (target is Turret turret)
            {
                multi = meleeWeapon.WeaponCategoryValue.TurretDamageMultiplier;
                Mod.UILog.Debug?.Write($"Target: {turret.DistinctId()} is a turret, using melee damage multipler: {multi}");
            }

            for (int i = 0; i < damage.Length; i++)
            {
                adjusted[i] = damage[i] * multi;
            }

            return adjusted;
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

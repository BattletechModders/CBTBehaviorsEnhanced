using IRBTModUtils.Extension;
using UnityEngine;

namespace CBTBehaviorsEnhanced.CAC
{
    public class InvalidateUnitStateOnDamage
    {
        public static float NoopDamageModifier(Weapon weapon, Vector3 attackPosition, ICombatant target,
            bool IsBreachingShot, int location, float dmg, float ap, float heat, float stab)
        {
            // Invalidate the melee state of the target, to force it to be recalculated
            if (target is AbstractActor targetActor)
            {
                Mod.Log.Debug?.Write($"Invalidating state for actor: {targetActor.DistinctId()}");
                ModState.InvalidateState(targetActor);
            }

            return 1.0f;
        }
    }
}

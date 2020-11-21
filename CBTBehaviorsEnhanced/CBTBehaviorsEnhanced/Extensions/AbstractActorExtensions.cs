using BattleTech;
using System;

namespace CBTBehaviorsEnhanced.Extensions
{
    public static class AbstractActorExtensions
    {


        public static float ApplyChargeDamageReduction(this AbstractActor actor, float rawDamage)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Charge damage:");
            return AbstractActorExtensions.AdjustDamage(actor, rawDamage, ModStats.ChargeTargetDamageReductionMulti);
        }

        public static float ApplyChargeInstabReduction(this AbstractActor actor, float rawInstab)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Charge instability:");
            return AbstractActorExtensions.AdjustDamage(actor, rawInstab, ModStats.ChargeTargetInstabReductionMulti);
        }

        public static float ApplyDFADamageReduction(this AbstractActor actor, float rawDamage)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting DFA damage:");
            return AbstractActorExtensions.AdjustDamage(actor, rawDamage, ModStats.DeathFromAboveTargetDamageReductionMulti);
        }

        public static float ApplyDFAInstabReduction(this AbstractActor actor, float rawInstab)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting DFA instability:");
            return AbstractActorExtensions.AdjustDamage(actor, rawInstab, ModStats.DeathFromAboveTargetInstabReductionMulti);
        }

        public static float ApplyKickDamageReduction(this AbstractActor actor, float rawDamage)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Kick damage:");
            return AbstractActorExtensions.AdjustDamage(actor, rawDamage, ModStats.KickTargetDamageReductionMulti);
        }

        public static float ApplyKickInstabReduction(this AbstractActor actor, float rawInstab)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Kick instability:");
            return AbstractActorExtensions.AdjustDamage(actor, rawInstab, ModStats.KickTargetInstabReductionMulti);
        }
        public static float ApplyPhysicalWeaponDamageReduction(this AbstractActor actor, float rawDamage)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting PhysicalWeapon damage:");
            return AbstractActorExtensions.AdjustDamage(actor, rawDamage, ModStats.PhysicalWeaponTargetDamageReductionMulti);
        }

        public static float ApplyPhysicalWeaponInstabReduction(this AbstractActor actor, float rawInstab)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting PhysicalWeapon instability:");
            return AbstractActorExtensions.AdjustDamage(actor, rawInstab, ModStats.PhysicalWeaponTargetInstabReductionMulti);
        }

        public static float ApplyPunchDamageReduction(this AbstractActor actor, float rawDamage)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Punch damage:");
            return AbstractActorExtensions.AdjustDamage(actor, rawDamage, ModStats.PunchTargetDamageReductionMulti);
        }

        public static float ApplyPunchInstabReduction(this AbstractActor actor, float rawInstab)
        {
            Mod.MeleeLog.Debug?.Write($"  Adjusting Punch instability:");
            return AbstractActorExtensions.AdjustDamage(actor, rawInstab, ModStats.PunchTargetInstabReductionMulti);
        }

        private static float AdjustDamage(AbstractActor actor, float rawDamage, string statName)
        {
            // Modifiers
            Statistic reduceMultiStat = actor.StatCollection.GetStatistic(statName);
            if (reduceMultiStat == null || reduceMultiStat.Value<float>() == 1f)
                return rawDamage;

            float adjusted = (float)Math.Floor(rawDamage * reduceMultiStat.Value<float>());
            Mod.MeleeLog.Debug?.Write($" - Target reduction multi: {reduceMultiStat.Value<float>()} " +
                $" x rawDamage: {rawDamage} => adjusted: {adjusted}");
            return adjusted;

        }
    }

}

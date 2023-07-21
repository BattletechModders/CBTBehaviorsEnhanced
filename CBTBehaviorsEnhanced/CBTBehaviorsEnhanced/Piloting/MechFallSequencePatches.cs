using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Piloting
{

    [HarmonyPatch(typeof(MechFallSequence), "OnAdded")]
    public class MechFallSequence_OnAdded
    {
        [HarmonyPostfix]
        public static void Postfix(MechFallSequence __instance)
        {
            Mod.Log.Trace?.Write("MFS:OnAdded - entered.");
            QuipHelper.PublishQuip(__instance.OwningMech, Mod.LocalizedText.Quips.Knockdown);
        }
    }

    // In TT mechs take damage from falling. In BTG only he pilot takes damage.
    // Create a new attack sequence and damage the mech.
    [HarmonyPatch(typeof(MechFallSequence), "OnComplete")]
    public class MechFallSequence_OnComplete
    {
        [HarmonyPrefix]
        static void Prefix(ref bool __runOriginal, MechFallSequence __instance)
        {
            if (!__runOriginal) return;

            Mod.Log.Trace?.Write("MFS:OnComplete - entered.");
            int rawDamage = (int)Math.Floor(__instance.OwningMech.tonnage * Mod.Config.Piloting.FallDamagePerTon);
            Mod.Log.Debug?.Write($"Actor: {__instance.OwningMech.DistinctId()} has fallen. Raw damage is: {rawDamage} from " +
                $"tonnage: {__instance.OwningMech.tonnage} x damagePerTon: {Mod.Config.Piloting.FallDamagePerTon}");

            float fallReductionMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.FallDamageReductionMulti);
            int ignoredDamage = (int)Math.Floor(Math.Max(0, rawDamage * fallReductionMulti));
            int fallingDamage = Math.Max(1, rawDamage - ignoredDamage);
            Mod.Log.Debug?.Write($"  - Fall damage reduced by {ignoredDamage} points to {fallingDamage}");

            // Cluster damage
            DamageHelper.ClusterDamage(fallingDamage, Mod.Config.Piloting.FallDamageClusterDivisor, out float[] fallDamageClusters);
            Mod.Log.Debug?.Write($"Actor {__instance.OwningMech.DistinctId()} has fallen - taking {fallingDamage} damage in {fallDamageClusters.Length} clusters.");

            try 
            {
                (Weapon melee, Weapon dfa) fakeWeapons = ModState.GetFakedWeapons(__instance.OwningMech);
                AttackHelper.CreateImaginaryAttack(__instance.OwningMech, fakeWeapons.melee, __instance.OwningMech, __instance.SequenceGUID, 
                    fallDamageClusters, DamageType.KnockdownSelf, MeleeAttackType.NotSet);
                Mod.Log.Debug?.Write($"  - Fall damage applied.");
            } 
            catch (Exception e) 
            {
                Mod.Log.Error?.Write(e, "FAILED TO APPLY FALL DAMAGE!");
            }
        }
    }
}

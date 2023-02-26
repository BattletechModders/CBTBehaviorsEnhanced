using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using HarmonyLib;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Piloting {

    [HarmonyPatch(typeof(MechFallSequence), "OnAdded")]
    public class MechFallSequence_OnAdded {
        public static void Postfix(MechFallSequence __instance) {
            Mod.Log.Trace?.Write("MFS:OnAdded - entered.");
            QuipHelper.PublishQuip(__instance.OwningMech, Mod.LocalizedText.Quips.Knockdown);            
        }
    }

    // In TT mechs take damage from falling. In BTG only he pilot takes damage. Create a new attack sequence and apply
    //   the TT rules for falling damage
    [HarmonyPatch(typeof(MechFallSequence), "OnComplete")]
    public class MechFallSequence_OnComplete {
        public static void Prefix(MechFallSequence __instance) {
            Mod.Log.Trace?.Write("MFS:OnComplete - entered.");
            int damagePointsTT = (int)Math.Ceiling(__instance.OwningMech.tonnage / 10f);
            Mod.Log.Debug?.Write($"Actor: {CombatantUtils.Label(__instance.OwningMech)} will suffer {damagePointsTT} TT damage points.");

            // Check for any pilot skill damage reduction
            float damageReduction = 1.0f - __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.DFAReductionMulti);
            float reducedDamage = (float)Math.Max(0f, Math.Floor(damageReduction * damagePointsTT));
            Mod.Log.Debug?.Write($" Reducing TT fall damage from: {damagePointsTT} by {damageReduction:P1} to {reducedDamage}");

            List<float> locationDamage = new List<float>();
            while (damagePointsTT >= 5) {
                locationDamage.Add(5 * Mod.Config.Piloting.FallingDamagePerTenTons);
                damagePointsTT -= 5;
            }
            if (damagePointsTT > 0) {
                locationDamage.Add(damagePointsTT * Mod.Config.Piloting.FallingDamagePerTenTons);
            }

            Mod.Log.Info?.Write($"FALLING DAMAGE: TT damage: {damagePointsTT} => {damagePointsTT * Mod.Config.Piloting.FallingDamagePerTenTons} falling damage to actor: {CombatantUtils.Label(__instance.OwningMech)}");

            try
            {
                (Weapon melee, Weapon dfa) fakeWeapons = ModState.GetFakedWeapons(__instance.OwningMech);
                AttackHelper.CreateImaginaryAttack(__instance.OwningMech, fakeWeapons.melee, __instance.OwningMech, __instance.SequenceGUID, locationDamage.ToArray(), DamageType.KnockdownSelf, MeleeAttackType.NotSet);
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "FAILED TO APPLY FALL DAMAGE");
            }
        }
    }
}

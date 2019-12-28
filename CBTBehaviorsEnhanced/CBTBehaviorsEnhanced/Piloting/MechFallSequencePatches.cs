using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Piloting {

    [HarmonyPatch(typeof(MechFallSequence), "OnComplete")]
    public class MechFallSequence_OnComplete {
        public static void Prefix(MechFallSequence __instance) {
            int damagePointsTT = (int)Math.Ceiling(__instance.OwningMech.tonnage / 10f);
            Mod.Log.Debug($"Actor: {CombatantUtils.Label(__instance.OwningMech)} will suffer {damagePointsTT} TT damage points.");

            // Check for any pilot skill damage reduction
            float damageReduction = 1.0f - __instance.OwningMech.PilotCheckMod(Mod.Config.Piloting.DFAReductionMulti);
            float reducedDamage = (float)Math.Max(0f, Math.Floor(damageReduction * damagePointsTT));
            Mod.Log.Debug($" Reducing TT fall damage from: {damagePointsTT} by {damageReduction:P1} to {reducedDamage}");

            List<int> locationDamage = new List<int>();
            while (damagePointsTT >= 5) {
                locationDamage.Add(5 * Mod.Config.Piloting.FallingDamagePerTenTons);
                damagePointsTT -= 5;
            }
            if (damagePointsTT > 0) {
                locationDamage.Add(damagePointsTT * Mod.Config.Piloting.FallingDamagePerTenTons);
            }

            Mod.Log.Debug($"Applying falling damage to actor: {CombatantUtils.Label(__instance.OwningMech)}");
            foreach (int damage in locationDamage) {
                ArmorLocation location = FallingDamageLocations[__instance.OwningMech.Combat.NetworkRandom.Int(0, FallingDamageLocations.Length)];
                Mod.Log.Debug($"  {damage} damage to location: {location}");
                __instance.OwningMech.DEBUG_DamageLocation(location, damage, __instance.OwningMech, DamageType.KnockdownSelf);
            }
        }

        private static readonly ArmorLocation[] FallingDamageLocations = new[] {
            ArmorLocation.Head,
            ArmorLocation.CenterTorso,
            ArmorLocation.CenterTorsoRear,
            ArmorLocation.LeftTorso,
            ArmorLocation.LeftTorsoRear,
            ArmorLocation.RightTorso,
            ArmorLocation.RightTorsoRear,
            ArmorLocation.LeftArm,
            ArmorLocation.RightArm,
            ArmorLocation.RightLeg,
            ArmorLocation.LeftLeg
        };
    }
}

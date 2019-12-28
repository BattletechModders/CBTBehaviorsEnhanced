
using BattleTech;
using CBTBehaviorsEnhanced.Components;
using CustomComponents;
using MechEngineer.Features.ComponentExplosions;

namespace CBTBehaviorsEnhanced {
    public static class HeatHelper {

        public static AmmunitionBox FindMostDamagingAmmoBox(Mech mech, bool isVolatile) {
            float totalDamage = 0f;
            AmmunitionBox mosDangerousBox = null;
            foreach (AmmunitionBox ammoBox in mech.ammoBoxes) {
                if (ammoBox.IsFunctional == false) {
                    Mod.Log.Debug($" AmmoBox: '{ammoBox.UIName}' is not functional, skipping."); 
                    continue; 
                }

                if (ammoBox.CurrentAmmo <= 0) {
                    Mod.Log.Debug($" AmmoBox: '{ammoBox.UIName}' has no ammo, skipping.");
                    continue; 
                }

                if (!ammoBox.mechComponentRef.Is<ComponentExplosion>(out ComponentExplosion compExp)) {
                    Mod.Log.Debug($"  AmmoBox: {ammoBox.UIName} is not configured as a ME ComponentExplosion, skipping.");
                    continue;
                }

                if (!ammoBox.mechComponentRef.Is<VolatileAmmo>(out VolatileAmmo vAmmo) && isVolatile) {
                    Mod.Log.Debug($"  AmmoBox: {ammoBox.UIName} is not a volatile ammo, skipping.");
                    continue;
                }

                float boxDamage = ammoBox.CurrentAmmo * compExp.HeatDamagePerAmmo + ammoBox.CurrentAmmo * compExp.ExplosionDamagePerAmmo + ammoBox.CurrentAmmo * compExp.StabilityDamagePerAmmo;
                // Multiply box damage by the 
                if (vAmmo != null) {
                    boxDamage *= vAmmo.damageWeighting;
                }

                Mod.Log.Debug($" AmmoBox: {ammoBox.UIName} has {ammoBox.CurrentAmmo} rounds with explosion/ammo: {compExp.ExplosionDamagePerAmmo} " +
                    $"heat/ammo: {compExp.HeatDamagePerAmmo} stab/ammo: {compExp.StabilityDamagePerAmmo} weight: {vAmmo?.damageWeighting} " +
                    $"for {boxDamage} total damage.");

                if (boxDamage > totalDamage) {
                    mosDangerousBox = ammoBox;
                    totalDamage = boxDamage;
                }
            }

            return mosDangerousBox;
        } 

    }
}

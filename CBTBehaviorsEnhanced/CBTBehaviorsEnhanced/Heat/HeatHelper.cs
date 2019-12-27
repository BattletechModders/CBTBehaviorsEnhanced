
using BattleTech;
using CustomComponents;
using MechEngineer.Features.ComponentExplosions;

namespace CBTBehaviorsEnhanced {
    public static class HeatHelper {

        public static AmmunitionBox FindMostDamagingAmmoBox(Mech mech, bool isInferno) {
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
                    Mod.Log.Info($"  AmmoBox: {ammoBox.UIName} is not configured as a ME ComponentExplosion, skipping.");
                }

                float boxDamage = isInferno ? ammoBox.CurrentAmmo * compExp.HeatDamagePerAmmo : ammoBox.CurrentAmmo * compExp.ExplosionDamagePerAmmo;
                Mod.Log.Debug($" AmmoBox: {ammoBox.UIName} has {ammoBox.CurrentAmmo} rounds with explosion/ammo: {compExp.ExplosionDamagePerAmmo} " +
                    $"heat/ammo: {compExp.HeatDamagePerAmmo} stab/ammo: {compExp.StabilityDamagePerAmmo} " +
                    $"for {boxDamage} total {(isInferno ? "inferno" : "explosion")} damage.");

                if (boxDamage > totalDamage) {
                    mosDangerousBox = ammoBox;
                    totalDamage = boxDamage;
                }
            }

            return mosDangerousBox;
        } 

    }
}

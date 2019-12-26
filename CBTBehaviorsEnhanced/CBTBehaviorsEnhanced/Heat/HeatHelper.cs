
using BattleTech;
using CustomComponents;
using Localize;
using MechEngineer.Features.ComponentExplosions;
using System;
using System.Collections.Generic;

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

        public class CBTPilotingRules {
            private readonly CombatGameState combat;

            public CBTPilotingRules(CombatGameState combat) {
                this.combat = combat;
            }

            public float GetGutsModifier(AbstractActor actor) {
                Pilot pilot = actor.GetPilot();

                float num = (pilot != null) ? ((float)pilot.Guts) : 1f;
                float gutsDivisor = Mod.Config.GutsDivisor;
                return num / gutsDivisor;
            }
        }

    }
}

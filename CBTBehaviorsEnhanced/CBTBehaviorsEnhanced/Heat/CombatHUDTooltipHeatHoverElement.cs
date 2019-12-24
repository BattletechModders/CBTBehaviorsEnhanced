using BattleTech;
using BattleTech.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat {
    public class CombatHUDSidePanelHeatHoverElement : CombatHUDSidePanelHoverElement {

        private CombatHUD CombatHUD = null;

        private int CurrentHeat = 0;
        private int ProjectedHeat = 0;
        private int TempHeat = 0;

        public new void Init(CombatHUD HUD) {
            CombatHUD = HUD;

            this.Title = new Localize.Text("HEAT LEVEL");
            this.Description = new Localize.Text($"Heat: 0 / 0");
            this.WarningText = new Localize.Text("");

            base.Init(CombatHUD);
        }

        public void UpdateText(Mech displayedMech) {

            if (displayedMech.CurrentHeat != CurrentHeat || displayedMech.TempHeat != TempHeat ||
                CombatHUD.SelectionHandler.ProjectedHeatForState != ProjectedHeat) {
                Mod.Log.Debug($"Updating heat dialog for actor: {CombatantUtils.Label(displayedMech)}");

                Mod.Log.Debug($"  current values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}");
                this.CurrentHeat = displayedMech.CurrentHeat;
                this.ProjectedHeat = CombatHUD.SelectionHandler.ProjectedHeatForState;
                this.TempHeat = displayedMech.TempHeat;

                int sinkableHeat = !displayedMech.HasAppliedHeatSinks ? displayedMech.AdjustedHeatsinkCapacity * -1 : 0;
                int futureHeat = Math.Max(0, CurrentHeat + TempHeat + ProjectedHeat + sinkableHeat);
                Mod.Log.Debug($"  currentHeat: {CurrentHeat}  tempHeat: {TempHeat}  projectedHeat: {ProjectedHeat}  sinkableHeat: {sinkableHeat}" +
                    $"  =  futureHeat: {futureHeat}");
                
                float gutsMulti = MechHelper.GetGutsMulti(displayedMech);
                float maxHeat = Mod.Config.Heat.Shutdown.Last().Key;
                StringBuilder descSB = new StringBuilder($"Heat: {futureHeat} / {maxHeat}\n");
                StringBuilder warningSB = new StringBuilder("");

                float threshold = 0f;
                // Check Ammo
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0f && threshold != -1f) { 
                    Mod.Log.Debug($"Ammo Explosion Threshold: {threshold:P1} vs. d100+{gutsMulti * 100f}");
                    descSB.Append($"Ammo Explosion on d100+{gutsMulti * 100f} < {threshold:P1}\n");
                } else if (threshold == -1f) {
                    warningSB.Append("Guaranteed Ammo Explosion!\n");
                }
                threshold = 0f;

                // Check Injury
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.PilotInjury) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0f) {
                    Mod.Log.Debug($"Injury Threshold: {threshold:P1} vs. d100+{gutsMulti * 100f}");
                    descSB.Append($"Pilot Injury on d100+{gutsMulti * 100f} < {threshold:P1}\n");
                }
                threshold = 0f;

                // Check System Failure
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.SystemFailures) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0f) {
                    Mod.Log.Debug($"System Failure Threshold: {threshold:P1} vs. d100+{gutsMulti * 100f}");
                    descSB.Append($"System Failure on d100+{gutsMulti * 100f} < {threshold:P1}\n");
                }
                threshold = 0f;

                // Check Shutdown
                foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Shutdown) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0f && threshold != -1f) {
                    Mod.Log.Debug($"Shutdown Threshold: {threshold:P1} vs. d100+{gutsMulti * 100f}");
                    descSB.Append($"Shutdown on d100+{gutsMulti * 100f} < {threshold:P1}");
                } else if (threshold == -1f) {
                    warningSB.Append("Guaranteed Shutdown!");
                }
                threshold = 0f;

                // Attack modifiers
                int modifier = 0;
                foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0) {
                    Mod.Log.Debug($"Attack Modifier: -{modifier}");
                }
                modifier = 0;

                // Movement modifier
                foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                    if (futureHeat >= kvp.Key) { threshold = kvp.Value; }
                }
                if (threshold != 0) {
                    Mod.Log.Debug($"Movement Modifier: -{modifier * 30}");
                }
                modifier = 0;

                base.SetTitleDescAndWarning("Heat", descSB.ToString(), warningSB.ToString());
                Mod.Log.Info($" Updated values: t:'{base.Title}' / d:'{base.Description}' / w:'{base.WarningText}'");
            }

        }

    }
}

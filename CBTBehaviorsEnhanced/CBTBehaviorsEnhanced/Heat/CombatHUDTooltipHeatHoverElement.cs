using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
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
        private int CACTerrainHeat = 0;
        private int CurrentPathNodes = 0;

        public new void Init(CombatHUD HUD) {
            CombatHUD = HUD;

            this.Title = new Localize.Text(new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Title]));
            this.Description = new Localize.Text($"Heat: 0 / 0");
            this.WarningText = new Localize.Text("");

            base.Init(CombatHUD);
        }

        public void UpdateText(Mech displayedMech) {
            // Calculate the heat from CAC burning 
            int cacTerrainHeat = displayedMech.CACTerrainHeat();
            int currentPathNodes = displayedMech.Pathing != null && displayedMech.Pathing.CurrentPath != null ? displayedMech.Pathing.CurrentPath.Count : 0;

            if (displayedMech.CurrentHeat == CurrentHeat && displayedMech.TempHeat == TempHeat && 
                CombatHUD.SelectionHandler.ProjectedHeatForState == ProjectedHeat && cacTerrainHeat == this.CACTerrainHeat
                && currentPathNodes == CurrentPathNodes) { return; }

            Mod.Log.Debug($"Updating heat dialog for actor: {CombatantUtils.Label(displayedMech)}");

            Mod.Log.Debug($"  current values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}  CACTerrainHeat: {cacTerrainHeat}  currentPathNodes: {currentPathNodes}");
            this.CurrentHeat = displayedMech.CurrentHeat;
            this.ProjectedHeat = CombatHUD.SelectionHandler.ProjectedHeatForState;
            this.TempHeat = displayedMech.TempHeat;
            this.CACTerrainHeat = cacTerrainHeat;
            this.CurrentPathNodes = currentPathNodes;
            Mod.Log.Debug($"  updated values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}  CACTerrainHeat: {cacTerrainHeat}  currentPathNodes: {currentPathNodes}");

            bool isProjectedHeat = this.ProjectedHeat != 0 || currentPathNodes != 0;
            int sinkableHeat = displayedMech.NormalizedAdjustedHeatSinkCapacity(isProjectedHeat, true) * -1;
            int overallSinkCapacity = displayedMech.NormalizedAdjustedHeatSinkCapacity(isProjectedHeat, false) *-1;
            Mod.Log.Debug($"  remainingCapacity: {displayedMech.HeatSinkCapacity}  rawCapacity: {overallSinkCapacity}  normedAdjustedFraction: {sinkableHeat}");

            //int futureHeat = Math.Max(0, CurrentHeat + TempHeat + ProjectedHeat + cacTerrainHeat + sinkableHeat);
            int futureHeat = Math.Max(0, CurrentHeat + TempHeat + ProjectedHeat + cacTerrainHeat);
            Mod.Log.Debug($"  currentHeat: {CurrentHeat} + tempHeat: {TempHeat} + projectedHeat: {ProjectedHeat} + cacTerrainheat: {cacTerrainHeat} + sinkableHeat: {sinkableHeat}" +
                $"  =  futureHeat: {futureHeat}");

            float heatCheck = displayedMech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
            float maxHeat = Mod.Config.Heat.Shutdown.Last().Key;

            StringBuilder descSB = new StringBuilder("");
            StringBuilder warningSB = new StringBuilder("");

            // Heat line
            float sinkCapMulti = displayedMech.DesignMaskHeatMulti(isProjectedHeat);
            string sinkCapMultiColor = sinkCapMulti >= 1f ? "00FF00" : "FF0000";
            descSB.Append(new Localize.Text(
                Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Heat], new object[] { futureHeat, maxHeat, sinkableHeat, overallSinkCapacity, sinkCapMultiColor, sinkCapMulti }
            ));

            float thresholdHeat = futureHeat + sinkableHeat;
            float threshold = 0f;
            // Check Ammo
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion) {
                if (thresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f && threshold != -1f) { 
                Mod.Log.Debug($"Ammo Explosion Threshold: {threshold:P1} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Explosion], new object[] { heatCheck * 100f, threshold }
                    ));
            } else if (threshold == -1f) {
                warningSB.Append(new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Explosion_Warning]));
            }
            threshold = 0f;

            // Check Injury
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.PilotInjury) {
                if (thresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f) {
                Mod.Log.Debug($"Injury Threshold: {threshold:P1} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Injury], new object[] { heatCheck * 100f, threshold }
                    ));
            }
            threshold = 0f;

            // Check System Failure
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.SystemFailures) {
                if (thresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f) {
                Mod.Log.Debug($"System Failure Threshold: {threshold:P1} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Sys_Failure], new object[] { heatCheck * 100f, threshold }
                    ));
            }
            threshold = 0f;

            // Check Shutdown
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Shutdown) {
                if (thresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f && threshold != -1f) {
                Mod.Log.Debug($"Shutdown Threshold: {threshold:P1} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Shutdown], new object[] { heatCheck * 100f, threshold }
                ));
            } else if (threshold == -1f) {
                warningSB.Append(new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Shutdown_Warning]));
            }
            threshold = 0f;

            // Attack modifiers
            int modifier = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                if (thresholdHeat >= kvp.Key) { modifier = kvp.Value; }
            }
            if (modifier != 0) {
                Mod.Log.Debug($"Attack Modifier: +{modifier}");
                descSB.Append(new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Attack], new object[] { modifier}));
            }
            modifier = 0;

            // Movement modifier
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing) {
                if (thresholdHeat >= kvp.Key) { modifier = kvp.Value; }
            }
            if (modifier != 0) {
                Mod.Log.Debug($"Movement Modifier: -{modifier * 30}m");
                descSB.Append(new Localize.Text(Mod.Config.LocalizedCHUDTooltips[ModConfig.CHUD_TT_Move], new object[] { modifier * 30f }));
            }
            modifier = 0;

            base.SetTitleDescAndWarning("Heat", descSB.ToString(), warningSB.ToString());
        }

    }
}

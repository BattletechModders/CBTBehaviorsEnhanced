using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
using System.Collections.Generic;
using System.Text;
using us.frostraptor.modUtils;
using static CBTBehaviorsEnhanced.HeatHelper;

namespace CBTBehaviorsEnhanced.Heat
{
    public class CombatHUDSidePanelHeatHoverElement : CombatHUDSidePanelHoverElement
    {

        private CombatHUD CombatHUD = null;

        private int CurrentHeat = 0;
        private int ProjectedHeat = 0;
        private int TempHeat = 0;
        private int CACTerrainHeat = 0;
        private int CurrentPathNodes = 0;

        public new void Init(CombatHUD HUD)
        {
            CombatHUD = HUD;

            this.Title = new Localize.Text(new Localize.Text(Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Title]));
            this.Description = new Localize.Text($"Heat: 0 / 0");
            this.WarningText = new Localize.Text("");

            base.Init(CombatHUD);
        }

        public void UpdateText(Mech displayedMech)
        {

            CalculatedHeat calculatedHeat = HeatHelper.CalculateHeat(displayedMech, CombatHUD.SelectionHandler.ProjectedHeatForState);

            // If everything has changed, skip and avoid the update
            if (calculatedHeat.CurrentHeat == CurrentHeat &&
                calculatedHeat.TempHeat == TempHeat &&
                calculatedHeat.ProjectedHeat == ProjectedHeat &&
                calculatedHeat.CACTerrainHeat == this.CACTerrainHeat &&
                calculatedHeat.CurrentPathNodes == CurrentPathNodes) { return; }

            Mod.HeatLog.Debug?.Write($"Updating heat dialog for actor: {displayedMech.DistinctId()}");
            Mod.HeatLog.Debug?.Write($"  previous values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}  CACTerrainHeat: {CACTerrainHeat}  currentPathNodes: {CurrentPathNodes}");
            this.CurrentHeat = calculatedHeat.CurrentHeat;
            this.ProjectedHeat = calculatedHeat.ProjectedHeat;
            this.TempHeat = calculatedHeat.TempHeat;
            this.CACTerrainHeat = calculatedHeat.CACTerrainHeat;
            this.CurrentPathNodes = calculatedHeat.CurrentPathNodes;
            Mod.HeatLog.Debug?.Write($"  current values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}  CACTerrainHeat: {CACTerrainHeat}  currentPathNodes: {CurrentPathNodes}");

            StringBuilder descSB = new StringBuilder("");
            StringBuilder warningSB = new StringBuilder("");

            // Future heat
            descSB.Append(new Localize.Text(
                Mod.LocalizedText.Tooltips[ModText.CHUD_TT_End_Heat], new object[] { calculatedHeat.ThresholdHeat, Mod.Config.Heat.MaxHeat }
            ));

            // Heat line
            float heatCheck = displayedMech.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);

            // Force a recalculation of the overheat warning
            if (calculatedHeat.FutureHeat > Mod.Config.Heat.WarnAtHeat)
            {
                CombatHUDStatusPanel combatHUDStatusPanel = HUD.MechTray.StatusPanel;
                combatHUDStatusPanel.ShowShutDownIndicator(displayedMech);
            }

            float sinkCapMulti = displayedMech.DesignMaskHeatMulti(calculatedHeat.IsProjectedHeat);
            string sinkCapMultiColor = sinkCapMulti >= 1f ? "00FF00" : "FF0000";
            descSB.Append(new Localize.Text(
                Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Heat], new object[] { calculatedHeat.FutureHeat, Mod.Config.Heat.MaxHeat, calculatedHeat.SinkableHeat, calculatedHeat.OverallSinkCapacity, sinkCapMultiColor, sinkCapMulti }
            ));

            float threshold = 0f;
            // Check Ammo
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Explosion)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f && threshold != -1f)
            {
                Mod.HeatLog.Debug?.Write($"Ammo Explosion Threshold: {threshold} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Explosion], new object[] { heatCheck * 100f, threshold * 100f }
                    ));
            }
            else if (threshold == -1f)
            {
                Mod.HeatLog.Debug?.Write($"Ammo Explosion Guaranteed!");
                warningSB.Append(new Localize.Text(Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Explosion_Warning]));
            }

            // Check Injury
            threshold = 0f;
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.PilotInjury)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f)
            {
                Mod.HeatLog.Debug?.Write($"Injury Threshold: {threshold} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Injury], new object[] { heatCheck * 100f, threshold * 100f }
                    ));
            }

            // Check System Failure
            threshold = 0f;
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.SystemFailures)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f)
            {
                Mod.HeatLog.Debug?.Write($"System Failure Threshold: {threshold} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Sys_Failure], new object[] { heatCheck * 100f, threshold * 100f }
                    ));
            }

            // Check Shutdown
            threshold = 0f;
            foreach (KeyValuePair<int, float> kvp in Mod.Config.Heat.Shutdown)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { threshold = kvp.Value; }
            }
            if (threshold != 0f && threshold != -1f)
            {
                Mod.HeatLog.Debug?.Write($"Shutdown Threshold: {threshold} vs. d100+{heatCheck * 100f}");
                descSB.Append(new Localize.Text(
                    Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Shutdown], new object[] { heatCheck * 100f, threshold * 100f }
                ));
            }
            else if (threshold == -1f)
            {
                Mod.HeatLog.Debug?.Write($"Shutdown Guaranteed!");
                warningSB.Append(new Localize.Text(Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Shutdown_Warning]));
            }

            // Attack modifiers
            int modifier = 0;
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { modifier = kvp.Value; }
            }
            if (modifier != 0)
            {
                Mod.HeatLog.Debug?.Write($"Attack Modifier: +{modifier}");
                descSB.Append(new Localize.Text(Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Attack], new object[] { modifier }));
            }
            modifier = 0;

            // Movement modifier
            foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing)
            {
                if (calculatedHeat.ThresholdHeat >= kvp.Key) { modifier = kvp.Value; }
            }
            if (modifier != 0)
            {
                Mod.HeatLog.Debug?.Write($"Movement Modifier: -{modifier * 30}m");
                descSB.Append(new Localize.Text(Mod.LocalizedText.Tooltips[ModText.CHUD_TT_Move], new object[] { modifier * 30f }));
            }

            base.SetTitleDescAndWarning("Heat", descSB.ToString(), warningSB.ToString());
        }

    }
}

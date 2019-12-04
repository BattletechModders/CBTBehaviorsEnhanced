using BattleTech;
using BattleTech.UI;
using CBTBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat {
    public class CombatHUDSidePanelHeatHoverElement : CombatHUDSidePanelHoverElement {

        private CombatHUD CombatHUD = null;

        private int CurrentHeat = 0;
        private int ProjectedHeat = 0;
        private int TempHeat = 0;

        private List<int> ShutdownThresholds = null;
        private int overHeatlevel = 0;
        private int maxHeatLevel = 0;

        public new void Init(CombatHUD HUD) {
            CombatHUD = HUD;

            ShutdownThresholds = Mod.Config.Heat.Shutdown.Keys.ToList().OrderBy(x => x).ToList();
            overHeatlevel = ShutdownThresholds.First();
            maxHeatLevel = ShutdownThresholds.Last();

            this.Title = new Localize.Text("HEAT LEVEL");
            this.Description = new Localize.Text($"HEAT: 0 / {maxHeatLevel}");
            this.WarningText = new Localize.Text("");

            base.Init(CombatHUD);
        }

        public void UpdateText(Mech displayedMech) {

            if (displayedMech.CurrentHeat != CurrentHeat || 
                CombatHUD.SelectionHandler.ProjectedHeatForState != ProjectedHeat ||
                displayedMech.TempHeat != TempHeat) {
                Mod.Log.Debug($"Updating heat dialog for actor: {CombatantUtils.Label(displayedMech)}");

                Mod.Log.Debug($"  current values:  CurrentHeat: {CurrentHeat}  ProjectedHeat: {ProjectedHeat}  TempHeat: {TempHeat}");
                this.CurrentHeat = displayedMech.CurrentHeat;
                this.ProjectedHeat = CombatHUD.SelectionHandler.ProjectedHeatForState;
                this.TempHeat = displayedMech.TempHeat;

                int sinkableHeat = !displayedMech.HasAppliedHeatSinks ? displayedMech.AdjustedHeatsinkCapacity * -1 : 0;
                int futureHeat = Math.Max(0, CurrentHeat + TempHeat + ProjectedHeat + sinkableHeat);
                Mod.Log.Debug($"  currentHeat: {CurrentHeat}  tempHeat: {TempHeat}  projectedHeat: {ProjectedHeat}  sinkableHeat: {sinkableHeat}" +
                    $"  =  futureHeat: {futureHeat}");

                string warningText = "";
                if (futureHeat >= overHeatlevel) {
                    int shutdownIdx = ShutdownThresholds.LastOrDefault(x => x <= futureHeat);
                    float rawShutdownChance = Mod.Config.Heat.Shutdown[shutdownIdx];
                    float pilotSkillReduction = Mod.Config.Heat.PilotSkillMulti * displayedMech.GetPilot().Piloting;
                    Mod.Log.Debug($"  rawShutdownChance: {rawShutdownChance}  pilotSkillReduction: {pilotSkillReduction}");

                    warningText = $"Shutdown: d100+{pilotSkillReduction * 100:#.#} < {rawShutdownChance:P1}";
                }

                base.SetTitleDescAndWarning("Heat", $"Heat: {futureHeat} / {maxHeatLevel}", warningText);
                Mod.Log.Info($" Updated values: t:'{base.Title}' / d:'{base.Description}' / w:'{base.WarningText}'");
            }

        }

    }
}

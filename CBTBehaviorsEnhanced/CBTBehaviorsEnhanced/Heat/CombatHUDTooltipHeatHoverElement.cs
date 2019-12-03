using BattleTech;
using BattleTech.UI;
using CBTBehaviors;

namespace CBTBehaviorsEnhanced.Heat {
    public class CombatHUDSidePanelHeatHoverElement : CombatHUDSidePanelHoverElement {

        public new void Init(CombatHUD HUD) {
            Mod.Log.Info("CHUDSPHHE:Init - entered.");
            this.Title = new Localize.Text("HEAT LEVEL 123");
            this.Description = new Localize.Text("HEAT TEST DESC 456");
            this.WarningText = new Localize.Text("HEAT WARNING");
            base.Init(HUD);
        }
    }
}

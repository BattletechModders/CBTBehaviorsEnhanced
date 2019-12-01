using BattleTech;
using BattleTech.UI;
using CBTBehaviors;
using UnityEngine.EventSystems;

namespace CBTBehaviorsEnhanced.Heat {
    class CombatHUDSidePanelHeatHoverElement : CombatHUDSidePanelHoverElement {

        CombatHUD CombatHUD = null;

        public new void Init(CombatHUD HUD) {
            CombatHUD = HUD;
            this.Title = new Localize.Text("HEAT LEVEL 123");
            this.Description = new Localize.Text("HEAT TEST DESC 456");
            this.WarningText = new Localize.Text("HEAT WARNING");
            base.Init(HUD);
        }

        public void Update() {
           // Mod.Log.Info("CHUDSPHHE update!");
        }

    }
}

using BattleTech;
using BattleTech.UI;
using CBTBehaviors;
using Harmony;
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

        public void UpdateText(string title, string description, string warning) {
            this.SetTitleDescAndWarning(title, description, warning);

            Mod.Log.Info("Updating text on tooltip.");
            Traverse sidebarT = Traverse.Create(this).Property("sidePanel");
            CombatHUDInfoSidePanel sidePanel = sidebarT.GetValue<CombatHUDInfoSidePanel>();
            sidePanel.SetNewToolTipHovering(this.Title, this.Description, this.WarningText, this);
            Mod.Log.Info("Done updating text on tooltip");
        }

    }
}

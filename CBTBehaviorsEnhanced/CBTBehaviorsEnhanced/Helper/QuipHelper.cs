using BattleTech;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils.CustomDialog;

namespace CBTBehaviorsEnhanced.Helper {
    public static class QuipHelper {

        // Generates a random quip and publishes it 
        public static void PublishQuip(AbstractActor source, List<string> quips) {

            string quip = quips[Mod.Random.Next(0, quips.Count)];
            string localizedQuip = new Localize.Text(quip).ToString();

            CastDef castDef = Coordinator.CreateCast(source);
            DialogueContent content = new DialogueContent(
                localizedQuip, Color.white, castDef.id, null, null, DialogCameraDistance.Medium, DialogCameraHeight.Default, 0
                );
            content.ContractInitialize(source.Combat);
            source.Combat.MessageCenter.PublishMessage(new CustomDialogMessage(source, content, 3));

        }
    }
}

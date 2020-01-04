using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.CustomDialog {
    public class CustomDialogMessage : MessageCenterMessage {

        public CustomDialogMessage(AbstractActor dialogueSource, DialogueContent dialogueContent) : base() {
            this.dialogueSource = dialogueSource;
            this.dialogueContent = dialogueContent;
        }

        public override MessageCenterMessageType MessageType {
            get { return (MessageCenterMessageType)MessageTypes.OnCustomDialog; }
        }

        public AbstractActor DialogueSource {
            get { return dialogueSource; }
        }

        public DialogueContent DialogueContent {
            get { return dialogueContent; }
        }

        public override void FromJSON(string json) { }

        public override string GenerateJSONTemplate() { return ""; }

        public override string ToJSON() { return ""; }

        private readonly AbstractActor dialogueSource;

        private readonly DialogueContent dialogueContent;


    }
}

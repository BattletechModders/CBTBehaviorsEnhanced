
using BattleTech;
using UnityEngine;

namespace CBTBehaviors {

    class MechEmergencyShutdownSequence : MultiSequence {
        public MechEmergencyShutdownSequence(Mech mech) : base(mech.Combat) {
            this.OwningMech = mech;
            this.setState();
        }

        private Mech OwningMech { get; set; }

        private void setState() {
            Mod.Log.Info($"Mech {this.OwningMech.DisplayName}_{this.OwningMech.GetPilot().Name} shuts down from overheating");
            this.OwningMech.IsShutDown = true;
            this.OwningMech.DumpAllEvasivePips();
        }

        public override void OnAdded() {
            base.OnAdded();
            if (this.OwningMech.GameRep != null) {
                string text = string.Format("MechOverheatSequence_{0}_{1}", base.RootSequenceGUID, base.SequenceGUID);
                AudioEventManager.CreateVOQueue(text, -1f, null, null);
                AudioEventManager.QueueVOEvent(text, VOEvents.Mech_Overheat_Shutdown, this.OwningMech);
                AudioEventManager.StartVOQueue(1f);
                this.OwningMech.GameRep.PlayVFX(1, this.OwningMech.Combat.Constants.VFXNames.heat_heatShutdown, true, Vector3.zero, false, -1f);
                this.AddChildSequence(new ShowActorInfoSequence(this.OwningMech, "Emergency Shutdown Initiated!", FloatieMessage.MessageNature.Debuff, true), this.ChildSequenceCount - 1);
                WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_overheat_alarm_3, WwiseManager.GlobalAudioObject, null, null);
                if (this.OwningMech.team.LocalPlayerControlsTeam) {
                    AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_overheating", null, null);
                }
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();
        }

        public override void OnSuspend() {
            base.OnSuspend();
        }

        public override void OnResume() {
            base.OnResume();
        }

        public override void OnComplete() {
            base.OnComplete();
        }

        public override bool IsValidMultiSequenceChild {
            get {
                return true;
            }
        }

        public override bool IsParallelInterruptable {
            get {
                return false;
            }
        }

        public override bool IsCancelable {
            get {
                return false;
            }
        }

        public override bool IsComplete {
            get {
                return base.IsComplete;
            }
        }

        public override int Size() {
            return 0;
        }

        public override bool ShouldSave() {
            return false;
        }
    }
}

using BattleTech;
using BattleTech.UI;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.CustomDialog {
    public class CustomDialogSequence : MultiSequence {

        public CustomDialogSequence(CombatGameState Combat, CombatHUDDialogSideStack sideStack, 
            DialogueContent content, AbstractActor dialogueSource, bool isCancelable = true) : base(Combat) {
            this.isCancelable = isCancelable;
            this.sideStack = sideStack;
            this.state = DialogState.None;
            this.dialogueSource = dialogueSource;
            this.content = content;
        }

        public void SetState(DialogState newState) {
            Mod.Log.Info($"DIALOGUE STATE CHANGED TO: {newState}");
            if (this.state == newState) {
                return;
            }
            this.state = newState;
            DialogState dialogState = this.state;
            if (dialogState != DialogState.Talking) {
                return;
            }
            this.PublishDialogMessages();
        }

        public override void OnUpdate() {
            base.OnUpdate();
        }

        private void PublishDialogMessages() {
            AudioEventManager.DialogSequencePlaying = true;
            if (this.dialogueSource == null || this.content.words.Length < 1) {
                this.SetState(DialogState.Finished);
                return;
            }
            this.PlayMessage();
        }

        public void PlayMessage() {
            AudioEventManager.InterruptPilotVOForTeam(base.Combat.LocalPlayerTeam, null);
            WwiseManager.PostEvent<AudioEventList_vo>(AudioEventList_vo.vo_stop_missions, WwiseManager.GlobalAudioObject, null, null);
            this.Play();
            this.SetState(DialogState.Finished);
        }

        private void Play() {
            Mod.Log.Info($"CDS - PLAY INVOKED FOR {CombatantUtils.Label(this.dialogueSource)}");
            // TODO: SOLVE THIS
            //if (this.content.cameraFocus.HasGuid) {
            //    base.FocusCamera(this.content, this.dialogSourceGUID);
            //}

            Mod.Log.Info("CDS - PanelFrame opened");
            this.sideStack.PanelFrame.gameObject.SetActive(true);
            //this.sideStack.ShowPortrait(this.content.GetPortraitSprite());
            if (this.dialogueSource.GetPilot() != null) {
                Mod.Log.Debug($"  Displaying pilot portrait");
                this.sideStack.ShowPortrait(this.dialogueSource.GetPilot().GetPortraitSpriteThumb());
            } else {
                Mod.Log.Debug($"  Displaying castDef portrait");
                this.sideStack.ShowPortrait(this.content.CastDef.defaultEmotePortrait.tempPortrait);
            }

            float showDuration = this.content.GetDialogueTime();

            // TODO: Note that audio content will be ignored
            //if (this.content.HasAudioEvent()) {
            //    Mod.Log.Info("CDS - PLAYING AUDIO");
            //    AudioEventManager.DialogSequencePlaying = true;
            //    DialogueAudioEvent.Play(base.Combat, this.content, new AkCallbackManager.EventCallback(this.AudioCallback));
            //    showDuration = 30f;
            //}

            this.activeDialog = this.sideStack.GetNextItem();
            this.activeDialog.Init(showDuration, true, new Action(this.AfterDialogShow), new Action(this.AfterDialogHide));
            this.activeDialog.SpeakerName = new BattleTech.UI.TMProWrapper.LocalizableText();

            Mod.Log.Info($"CDS - Showing dialog: words:{this.content.words} color:{this.content.wordsColor} speakerName:{this.content.SpeakerName} timeout: {this.content.GetDialogueTime()}");
            this.activeDialog.Show(this.content.words, this.content.wordsColor, this.content.SpeakerName);
            Mod.Log.Info("CDS - DONE");

            base.Combat.MessageCenter.PublishMessage(new DialogueContinueMessage(this.dialogueSource.GUID, this.content.conversationIdx));
        }

        public void AudioCallback(object in_cookie, AkCallbackType in_type, object in_info) {
            if (this.activeDialog != null) {
                this.activeDialog.FadeOut(0.5f);
            }
            this.activeDialog = null;
            this.content = null;
        }

        public void AfterDialogShow() {
            this.sideStack.AfterDialogShow();
        }

        public void AfterDialogHide() {
            this.sideStack.AfterDialogHide();
            this.PlayMessage();
        }

        public void UserRequestHide() {
            this.sideStack.HideAll();
        }

        public override void OnAdded() {
            Mod.Log.Info($"DIALOGUE ADDED TO CURRENT STACK");
            base.OnAdded();
            this.SetState(DialogState.Talking);
        }

        public override void OnComplete() {
            base.OnComplete();
            AudioEventManager.DialogSequencePlaying = false;
            this.SendCompleteMessage();
        }

        public void SendCompleteMessage() {
            base.Combat.MessageCenter.PublishMessage(new DialogComplete(this.dialogueSource.GUID));
        }

        public void SetIsCancelable(bool isCancelable) {
            this.isCancelable = isCancelable;
        }

        public override bool IsParallelInterruptable {
            get {
                return true;
            }
        }

        public override bool IsCancelable {
            get {
                return this.isCancelable;
            }
        }

        public override bool IsComplete {
            get {
                return this.state == DialogState.Finished && this.IsCameraFinished;
            }
        }

        public bool IsCameraFinished {
            get {
                return base.cameraSequence == null || base.cameraSequence.IsFinished;
            }
        }

        // Token: 0x06006726 RID: 26406 RVA: 0x001BC38F File Offset: 0x001BA58F
        public override void OnSuspend() {
            base.OnSuspend();
            this.UserRequestHide();
        }

        public override void OnResume() {
            base.OnResume();
            if (this.activeDialog != null) {
                if (this.content != null) {
                    this.Play();
                    return;
                }
            } else {
                this.PlayMessage();
            }
        }

        public override void OnCanceled() {
            base.OnCanceled();
            this.UserRequestHide();
            this.pendingMessages.Clear();
            this.content = null;
            this.SetState(DialogState.Finished);
            this.SendCompleteMessage();
        }

        private bool isCancelable;

        private DialogState state;

        private readonly AbstractActor dialogueSource;

        private readonly CombatHUDDialogSideStack sideStack;

        public List<DialogueContent> pendingMessages = new List<DialogueContent>();

        private DialogueContent content;

        private CombatHUDDialogItem activeDialog;
    }
}

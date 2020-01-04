using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using HBS.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.CustomDialog {
    // This classes liberally borrows CWolf's amazing MissionControl mod, in particular 
    //  https://github.com/CWolfs/MissionControl/blob/master/src/Core/DataManager.cs
    public static class Coordinator {

        private static CombatGameState Combat;
        private static MessageCenter MessageCenter;
        private static CombatHUDDialogSideStack SideStack;

        private static readonly List<string> Genders = 
            new List<string> { "Male", "Female", "Unspecified" };
        private static Dictionary<string, Dictionary<string, List<string>>> FirstNames = 
            new Dictionary<string, Dictionary<string, List<string>>>(); // e.g. <Male, <FactionName, [list of names]>>
        private static Dictionary<string, List<string>> LastNames = 
            new Dictionary<string, List<string>>();  // e.g. <All, [list of names]>
        private static Dictionary<string, List<string>> Ranks = 
            new Dictionary<string, List<string>>();      // e.g. <FactionName, [list of ranks]>
        private static Dictionary<string, List<string>> Portraits = 
            new Dictionary<string, List<string>>();  // e.g. <Male, [list of male portraits]

        private static bool PilotDataLoaded = false;

        public static bool CombatIsActive {
            get { return Coordinator.Combat != null && Coordinator.SideStack != null; }
        }

        public static void OnCustomDialogMessage(MessageCenterMessage message) {
            CustomDialogMessage msg = (CustomDialogMessage)message;
            if (msg == null) { return; }

            Mod.Log.Info("COORDINATOR - PUBLISHING SEQUENCE");
            MessageCenter.PublishMessage(
                new AddSequenceToStackMessage(
                    new CustomDialogSequence(Combat, SideStack, msg.DialogueContent, msg.DialogueSource, true)
                    )
                );
        }

        public static void OnCombatHUDInit(CombatGameState combat, CombatHUD combatHUD) {
            Mod.Log.Info("COORDINATOR - OCHUDI");

            Coordinator.Combat = combat;
            Coordinator.MessageCenter = combat.MessageCenter;
            Coordinator.SideStack = combatHUD.DialogSideStack;
        } 

        public static void OnCombatGameDestroyed() {
            Mod.Log.Info("COORDINATOR - OCGD");

            Combat = null;
            MessageCenter = null;
            SideStack = null;
        }

        public static string GetCastDefId(AbstractActor actor) {
            string castDefId = $"castDef_{actor.GUID}";

            string factionId = actor.team != null ? actor.team.FactionValue.ToString() : "";

            if (actor.GetPilot() != null) {
            } else {
                //castDefId = $"castDef_{actor.team{actor.DisplayName}_"
            }

            return $"castDef_";
        }

        public static CastDef CreateCast(AbstractActor actor) {
            FactionValue actorFaction = actor?.team?.FactionValue;
            bool factionExists = actorFaction.Name != "INVALID_UNSET" && actorFaction.Name != "NoFaction" && 
                actorFaction.FactionDefID != null && actorFaction.FactionDefID.Length != 0 ? true : false;

            string employerFactionName = "Military Support";
            if (factionExists) {
                string factionId = actorFaction?.FactionDefID;
                FactionDef employerFactionDef = UnityGameInstance.Instance.Game.DataManager.Factions.Get(factionId);
                if (employerFactionDef == null) { Mod.Log.Error($"Error finding FactionDef for faction with id '{factionId}'"); }
                else { employerFactionName = employerFactionDef.Name.ToUpper(); }
            } else {
                Mod.Log.Debug($"Found faction: {actorFaction}");
            }

            CastDef newCastDef = new CastDef {
                // Temp test data
                FactionValue = actorFaction,
                showRank = true,
                showFirstName = true,
                showCallsign = false,
                showLastName = true
            };

            if (actor.GetPilot() != null) {
                Mod.Log.Debug("Actor is piloted, using pilot values.");
                Pilot pilot = actor.GetPilot();

                newCastDef.internalName = $"{pilot.FirstName}{pilot.LastName}";
                newCastDef.firstName = $"{pilot.FirstName}";
                newCastDef.lastName = pilot.LastName;
                newCastDef.callsign = pilot.Callsign;
                newCastDef.rank = employerFactionName;
                newCastDef.gender = pilot.Gender;

                newCastDef.showCallsign = true;
                newCastDef.showFirstName = false;
                newCastDef.showLastName = false;
                newCastDef.showRank = false;

                newCastDef.id = $"castDef_{pilot.FirstName}{pilot.LastName}";
            } else {
                Mod.Log.Debug("Actor is not piloted, generating castDef.");
                string gender = GetRandomGender();
                Gender btGender = Gender.Male;
                if (gender == "Female") btGender = Gender.Female;
                if (gender == "Unspecified") btGender = Gender.NonBinary;

                string factionDMKey = factionExists ? "All" : actorFaction.ToString();
                string firstName = GetRandomFirstName(gender, factionDMKey);
                string lastName = GetRandomLastName(factionDMKey);
                string rank = GetRandomRank(factionDMKey);

                newCastDef.internalName = $"{rank}{firstName}{lastName}";
                newCastDef.firstName = $"{rank} {firstName}";
                newCastDef.lastName = lastName;
                newCastDef.callsign = rank;
                newCastDef.rank = employerFactionName;
                newCastDef.gender = btGender;

                string portraitPath = GetRandomPortraitPath(gender);
                newCastDef.defaultEmotePortrait.portraitAssetPath = portraitPath;

                Mod.Log.Debug($" Generated cast with DisplayName: {newCastDef.DisplayName()} using portrait: {portraitPath}");
                newCastDef.id = $"castDef_{rank}{firstName}{lastName}";
            }

            ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(newCastDef.id, newCastDef);

            return newCastDef;
        }

        // == Evertthing below taken or adapted from CWolf's Mission Control
        private static void LoadPilotData() {
            if (PilotDataLoaded) { return; }

            string firstNameJson = File.ReadAllText($"{Mod.ModDir}/cast/FirstNames.json");
            Coordinator.FirstNames = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(firstNameJson);

            string lastNameJson = File.ReadAllText($"{Mod.ModDir}/cast/LastNames.json");
            Coordinator.LastNames = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(lastNameJson);

            string rankJson = File.ReadAllText($"{Mod.ModDir}/cast/Ranks.json");
            Coordinator.Ranks = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(rankJson);

            string portraitJson = File.ReadAllText($"{Mod.ModDir}/cast/Portraits.json");
            Coordinator.Portraits = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(portraitJson);
        }

        public static string GetRandomGender() {
            LoadPilotData();
            return Coordinator.Genders[UnityEngine.Random.Range(0, Genders.Count)];
        }

        public static string GetRandomFirstName(string gender, string factionKey) {
            LoadPilotData();

            List<string> names = new List<string>();
            names.AddRange(Coordinator.FirstNames["All"]["All"]);
            if (gender == "Male" || gender == "Female") {
                Dictionary<string, List<string>> genderNames = Coordinator.FirstNames[gender];
                names.AddRange(genderNames["All"]);
                if (genderNames.ContainsKey(factionKey)) names.AddRange(genderNames[factionKey]);
            }

            return names[UnityEngine.Random.Range(0, names.Count)];
        }

        public static string GetRandomLastName(string factionKey) {
            LoadPilotData();

            float chance = UnityEngine.Random.Range(0f, 100f);
            bool useFactionName = false;
            if (chance < 75) useFactionName = true;

            List<string> names;
            if (Coordinator.LastNames.ContainsKey(factionKey) && Coordinator.LastNames[factionKey].Count > 0 && useFactionName) {
                names = Coordinator.LastNames[factionKey];
            } else {
                names = Coordinator.LastNames["All"];
            }

            return names[UnityEngine.Random.Range(0, names.Count)];
        }

        public static string GetRandomRank(string factionKey) {
            LoadPilotData();

            List<string> ranks = new List<string>();

            if (Coordinator.Ranks.ContainsKey(factionKey) && Coordinator.Ranks[factionKey].Count > 0) {
                ranks.AddRange(Coordinator.Ranks[factionKey]);
            } else {
                ranks.AddRange(Coordinator.Ranks["Fallback"]);
            }

            return ranks[UnityEngine.Random.Range(0, ranks.Count)];
        }

        public static string GetRandomPortraitPath(string gender) {
            LoadPilotData();

            List<string> portraits = new List<string>();
            portraits.AddRange(Coordinator.Portraits["All"]);
            if (Coordinator.Portraits.ContainsKey(gender)) portraits.AddRange(Coordinator.Portraits[gender]);

            return portraits[UnityEngine.Random.Range(0, portraits.Count)];
        }

    }
}

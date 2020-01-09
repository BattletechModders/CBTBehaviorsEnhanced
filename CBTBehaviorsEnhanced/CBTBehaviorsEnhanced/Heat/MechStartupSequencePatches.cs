using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat {


    [HarmonyPatch(typeof(MechStartupInvocation), "Invoke")]
    public static class MechStartupInvocation_Invoke {
        public static bool Prepare() { return Mod.Config.Features.StartupChecks; }

        public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState) {

            Mech mech = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
            if (mech == null) { return true; }

            // Check to see if we should restart automatically
            float heatCheck = mech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
            bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, mech, heatCheck, ModConfig.FT_Check_Shutdown);
            if (passedStartupCheck) { return true; } // Do the normal startup process

            Mod.Log.Debug($"Mech: {CombatantUtils.Label(mech)} failed a startup roll, venting heat but remaining offline.");

            QuipHelper.PublishQuip(mech, Mod.Config.Qips.Startup);

            MechHeatSequence mechHeatSequence = new MechHeatSequence(mech, true, true, "STARTUP");
            DoneWithActorSequence dwas = (DoneWithActorSequence)mech.GetDoneWithActorOrders();
            mechHeatSequence.AddChildSequence(dwas, mechHeatSequence.MessageIndex);

            InvocationStackSequenceCreated message = new InvocationStackSequenceCreated(mechHeatSequence, __instance);
            combatGameState.MessageCenter.PublishMessage(message);
            AddSequenceToStackMessage.Publish(combatGameState.MessageCenter, mechHeatSequence);

            //mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(mechHeatSequence));

            //mech.OnStartupComplete(mechHeatSequence.SequenceGUID);
            //mech.DoneWithActor();

            return false;
        }
    }

    // Forces the mech to make a piloting skill check, or fail to startup when shutdown
    //    [HarmonyPatch(typeof(MechStartupSequence), "OnAdded")]
    //    public static class MechStartupSequence_OnAdded {

    //        public static bool Prepare() { return Mod.Config.Features.StartupChecks; }

    //        public static bool Prefix(MechStartupSequence __instance, Mech ___OwningMech, MechHeatSequence ___heatSequence) {

    //            // Check to see if we should restart automatically
    //            float heatCheck = ___OwningMech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
    //            bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, ___OwningMech, heatCheck, ModConfig.FT_Check_Shutdown);
    //            if (passedStartupCheck) { return true; } // Do the normal startup process

    //            Mod.Log.Debug($"Mech: {CombatantUtils.Label(___OwningMech)} failed a startup roll, venting heat but remaining offline.");
    //            ___OwningMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(___heatSequence));

    //            QuipHelper.PublishQuip(___OwningMech, Mod.Config.Qips.Startup);

    //            ModState.StartupSequences.Add(__instance.SequenceGUID);

    //            return false;
    //        }

    //    }

    //    [HarmonyPatch(typeof(MechStartupSequence), "OnUpdate")]
    //    public static class MechStartupSequence_OnUpdate {
    //        static bool Prepare() { return Mod.Config.Features.StartupChecks; }

    //        public static void Prefix(MechStartupSequence __instance, Mech ___OwningMech, StartupState ___state) {
    //            if (ModState.StartupSequences.Contains(__instance.SequenceGUID)) {
    //                Mod.Log.Debug($"Marking sequence finished for Mech: {CombatantUtils.Label(___OwningMech)}.");                
    //                ModState.StartupSequences.Remove(__instance.SequenceGUID);

    //                Traverse setStateT = Traverse.Create(__instance).Method("setState");
    //                setStateT.GetValue(new object[] { StartupState.Finished });
    //                Mod.Log.Debug($"  Traverse fired!");
    //            }
    //        }

    //        public enum StartupState { None, Starting, Standing, Finished }

    //    }
}

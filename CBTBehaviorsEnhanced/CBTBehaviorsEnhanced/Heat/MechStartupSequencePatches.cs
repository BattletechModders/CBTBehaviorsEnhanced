using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat {

    // Forces the mech to make a piloting skill check, or fail to startup when shutdown
    [HarmonyPatch(typeof(MechStartupInvocation), "Invoke")]
    public static class MechStartupInvocation_Invoke {
        public static bool Prepare() { return Mod.Config.Features.StartupChecks; }

        public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState) {

            Mech mech = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
            if (mech == null) { return true; }

            Mod.Log.Info($"Processing startup for Mech: {CombatantUtils.Label(mech)}");

            // Check to see if we should restart automatically
            float heatCheck = mech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
            int futureHeat = mech.CurrentHeat - mech.AdjustedHeatsinkCapacity;
            bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, futureHeat, mech, heatCheck, ModConfig.FT_Check_Startup);

            bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(mech, -1, -1, heatCheck);
            if (failedInjuryCheck) Mod.Log.Info("  -- unit did not pass injury check!");

            bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(mech, -1, heatCheck);
            if (failedSystemFailureCheck) Mod.Log.Info("  -- unit did not pass system failure check!");

            bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(mech, -1, heatCheck);
            if (failedAmmoCheck) Mod.Log.Info("  -- unit did not pass ammo explosion check!");

            bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(mech, -1, heatCheck);
            if (failedVolatileAmmoCheck) Mod.Log.Info("  -- unit did not pass volatile ammo explosion check!");

            if (passedStartupCheck) { return true; } // Do the normal startup process

            Mod.Log.Debug($"Mech: {CombatantUtils.Label(mech)} failed a startup roll, venting heat but remaining offline.");

            DoneWithActorSequence doneWithActorSequence = (DoneWithActorSequence)mech.GetDoneWithActorOrders();
            MechHeatSequence mechHeatSequence = new MechHeatSequence(mech, true, true, "STARTUP");
            doneWithActorSequence.AddChildSequence(mechHeatSequence, mechHeatSequence.MessageIndex);
            
            QuipHelper.PublishQuip(mech, Mod.Config.Qips.Startup);

            InvocationStackSequenceCreated message = new InvocationStackSequenceCreated(doneWithActorSequence, __instance);
            combatGameState.MessageCenter.PublishMessage(message);
            AddSequenceToStackMessage.Publish(combatGameState.MessageCenter, doneWithActorSequence);

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

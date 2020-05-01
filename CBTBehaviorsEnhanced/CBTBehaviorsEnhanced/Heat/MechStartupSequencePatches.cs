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

            bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(mech, futureHeat, -1, -1, heatCheck);
            if (failedInjuryCheck) Mod.Log.Info("  -- unit did not pass injury check!");

            bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(mech, futureHeat, -1, heatCheck);
            if (failedSystemFailureCheck) Mod.Log.Info("  -- unit did not pass system failure check!");

            bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(mech, futureHeat, -1, heatCheck);
            if (failedAmmoCheck) Mod.Log.Info("  -- unit did not pass ammo explosion check!");

            bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(mech, futureHeat, -1, heatCheck);
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

            return false;
        }
    }

}

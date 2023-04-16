using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat
{

    // Forces the mech to make a piloting skill check, or fail to startup when shutdown
    [HarmonyPatch(typeof(MechStartupInvocation), "Invoke")]
    static class MechStartupInvocation_Invoke
    {
        static bool Prepare() { return Mod.Config.Features.StartupChecks; }

        static void Prefix(ref bool __runOriginal, MechStartupInvocation __instance, CombatGameState combatGameState)
        {
            if (!__runOriginal) return;

            Mech mech = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
            if (mech == null) 
                return;
            
            Mod.Log.Info?.Write($"Processing startup for Mech: {CombatantUtils.Label(mech)}");

            // Check to see if we should restart automatically
            float heatCheck = mech.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);
            int futureHeat = mech.CurrentHeat - mech.AdjustedHeatsinkCapacity;
            bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, futureHeat, mech, heatCheck, ModText.FT_Check_Startup);
            Mod.Log.Info?.Write($"  -- futureHeat: {futureHeat} = current: {mech.CurrentHeat} - HSCapacity: {mech.AdjustedHeatsinkCapacity} vs. heatCheck: {heatCheck} => passedStartup: {passedStartupCheck}");

            float injuryCheckMod = mech.StatCollection.GetValue<float>(ModStats.InjuryCheckMod);
            Mod.HeatLog.Debug?.Write($"  -- injuryCheck = skill: {heatCheck} + mod: {injuryCheckMod}");
            bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(mech, futureHeat, -1, -1, heatCheck + injuryCheckMod);
            if (failedInjuryCheck) Mod.Log.Info?.Write("  -- unit did not pass injury check!");

            float systemCheckMod = mech.StatCollection.GetValue<float>(ModStats.SystemFailureCheckMod);
            Mod.HeatLog.Debug?.Write($"  -- systemFailureCheck = skill: {heatCheck} + mod: {systemCheckMod}");
            bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(mech, futureHeat, -1, heatCheck + systemCheckMod);
            if (failedSystemFailureCheck) Mod.Log.Info?.Write("  -- unit did not pass system failure check!");

            float ammoCheckMod = mech.StatCollection.GetValue<float>(ModStats.AmmoCheckMod);
            Mod.HeatLog.Debug?.Write($"  -- ammoCheck = skill: {heatCheck} + mod: {ammoCheckMod}");
            bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(mech, futureHeat, -1, heatCheck + ammoCheckMod);
            if (failedAmmoCheck) Mod.Log.Info?.Write("  -- unit did not pass ammo explosion check!");

            bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(mech, futureHeat, -1, heatCheck + ammoCheckMod);
            if (failedVolatileAmmoCheck) Mod.Log.Info?.Write("  -- unit did not pass volatile ammo explosion check!");

            if (passedStartupCheck)
            {
                Mod.Log.Debug?.Write($" -- passed startup roll, going through regular MechStartupSequence.");
                return;
            }

            Mod.Log.Info?.Write($" -- failed startup roll, venting heat but remaining offline.");

            DoneWithActorSequence doneWithActorSequence = (DoneWithActorSequence)mech.GetDoneWithActorOrders();
            MechHeatSequence mechHeatSequence = new MechHeatSequence(OwningMech: mech, performHeatSinkStep: true, applyStartupHeatSinks: false, instigatorID: "STARTUP");
            doneWithActorSequence.AddChildSequence(mechHeatSequence, mechHeatSequence.MessageIndex);

            QuipHelper.PublishQuip(mech, Mod.LocalizedText.Quips.Startup);

            InvocationStackSequenceCreated message = new InvocationStackSequenceCreated(doneWithActorSequence, __instance);
            combatGameState.MessageCenter.PublishMessage(message);
            AddSequenceToStackMessage.Publish(combatGameState.MessageCenter, doneWithActorSequence);

            Mod.Log.Debug?.Write($" -- sent sequence to messageCenter");
            __runOriginal = false;
        }
    }

}

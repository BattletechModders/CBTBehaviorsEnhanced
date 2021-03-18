using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Heat
{

    // Forces the mech to make a piloting skill check, or fail to startup when shutdown
    [HarmonyPatch(typeof(MechStartupInvocation), "Invoke")]
    public static class MechStartupInvocation_Invoke
    {
        public static bool Prepare() { return Mod.Config.Features.StartupChecks; }

        public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState)
        {

            Mech mech = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
            if (mech == null) { return true; }

            Mod.Log.Info?.Write($"Processing startup for Mech: {CombatantUtils.Label(mech)}");

            // Check to see if we should restart automatically
            float heatCheck = mech.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);
            int futureHeat = mech.CurrentHeat - mech.AdjustedHeatsinkCapacity;
            bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, futureHeat, mech, heatCheck, ModText.FT_Check_Startup);
            Mod.Log.Info?.Write($"  -- futureHeat: {futureHeat} = current: {mech.CurrentHeat} - HSCapacity: {mech.AdjustedHeatsinkCapacity} vs. heatCheck: {heatCheck} => passedStartup: {passedStartupCheck}");

            bool failedInjuryCheck = CheckHelper.ResolvePilotInjuryCheck(mech, futureHeat, -1, -1, heatCheck);
            if (failedInjuryCheck) Mod.Log.Info?.Write("  -- unit did not pass injury check!");

            bool failedSystemFailureCheck = CheckHelper.ResolveSystemFailureCheck(mech, futureHeat, -1, heatCheck);
            if (failedSystemFailureCheck) Mod.Log.Info?.Write("  -- unit did not pass system failure check!");

            bool failedAmmoCheck = CheckHelper.ResolveRegularAmmoCheck(mech, futureHeat, -1, heatCheck);
            if (failedAmmoCheck) Mod.Log.Info?.Write("  -- unit did not pass ammo explosion check!");

            bool failedVolatileAmmoCheck = CheckHelper.ResolveVolatileAmmoCheck(mech, futureHeat, -1, heatCheck);
            if (failedVolatileAmmoCheck) Mod.Log.Info?.Write("  -- unit did not pass volatile ammo explosion check!");

            if (passedStartupCheck) 
            {
                Mod.Log.Debug?.Write($" -- passed startup roll, going through regular MechStartupSequence.");
                return true; 
            }

            Mod.Log.Info?.Write($" -- failed startup roll, venting heat but remaining offline.");

            DoneWithActorSequence doneWithActorSequence = (DoneWithActorSequence)mech.GetDoneWithActorOrders();
            MechHeatSequence mechHeatSequence = new MechHeatSequence(OwningMech: mech, performHeatSinkStep: true, applyStartupHeatSinks: false, instigatorID: "STARTUP");
            doneWithActorSequence.AddChildSequence(mechHeatSequence, mechHeatSequence.MessageIndex);

            QuipHelper.PublishQuip(mech, Mod.LocalizedText.Qips.Startup);

            InvocationStackSequenceCreated message = new InvocationStackSequenceCreated(doneWithActorSequence, __instance);
            combatGameState.MessageCenter.PublishMessage(message);
            AddSequenceToStackMessage.Publish(combatGameState.MessageCenter, doneWithActorSequence);

            Mod.Log.Debug?.Write($" -- sent sequence to messageCenter");
            return false;
        }
    }

}

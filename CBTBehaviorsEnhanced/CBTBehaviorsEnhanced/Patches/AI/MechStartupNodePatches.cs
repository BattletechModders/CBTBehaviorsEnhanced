using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using System.Reflection;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches.AI
{
    // This node is used by the behavior tree, and it expects it to always pass. Intercept and apply the explosion logic.
    [HarmonyPatch(typeof(MechStartUpNode), "Tick")]
    static class MechStartUpNode_Tick
    {
        static void Postfix(BehaviorNode __instance, ref BehaviorTreeResults __result)
        {
            AbstractActor unit = __instance.unit;
            if (unit is Mech mech)
            {

                float heatCheck = mech.HeatCheckMod(Mod.Config.SkillChecks.ModPerPointOfGuts);
                int futureHeat = mech.CurrentHeat - mech.AdjustedHeatsinkCapacity;

                // Check to see if we will shutdown
                bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, futureHeat, mech, heatCheck, ModText.FT_Check_Startup);
                Mod.Log.Info?.Write($"AI unit {CombatantUtils.Label(mech)} heatCheck: {heatCheck} vs. futureHeat: {futureHeat} " +
                    $"(from currentHeat: {mech.CurrentHeat} - sinking: {mech.AdjustedHeatsinkCapacity}) => passed: {passedStartupCheck}");

                if (!passedStartupCheck)
                {
                    Mod.Log.Info?.Write($" -- shutdown check failed, forcing it to remain shutdown.");
                    BehaviorTreeResults newResult = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    newResult.orderInfo = new OrderInfo(OrderType.Stand);
                    __result = newResult;

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

                    QuipHelper.PublishQuip(mech, Mod.LocalizedText.Quips.Startup);
                }
                else
                {
                    Mod.Log.Info?.Write($" -- shutdown check passed, starting up normally.");
                }

            }

        }

    }


}

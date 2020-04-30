using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Harmony;
using System.Reflection;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches.AI
{

    [HarmonyPatch]
    static class MechStartUpNode_Tick
    {

        // These patches target internal classes that can't be addressed with annotations
        static MethodBase TargetMethod()
        {
            return AccessTools.Method("MechStartUpNode:Tick");
        }

        static void Postfix(BehaviorNode __instance, ref BehaviorTreeResults __result)
        {
            Traverse unitT = Traverse.Create(__instance).Field("unit");
            AbstractActor unit = unitT.GetValue<AbstractActor>();
            if (unit is Mech mech)
            {
                // Check to see if we should restart automatically
                float heatCheck = mech.HeatCheckMod(Mod.Config.Piloting.SkillMulti);
                int futureHeat = mech.CurrentHeat - mech.AdjustedHeatsinkCapacity;
                bool passedStartupCheck = CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Shutdown, futureHeat, mech, heatCheck, ModConfig.FT_Check_Startup);
                Mod.Log.Info($"AI unit {CombatantUtils.Label(mech)} heatCheck: {heatCheck} vs. futureHeat: {futureHeat} => passed: {passedStartupCheck}");

                if (!passedStartupCheck)
                {
                    Mod.Log.Info($" -- shutdown check failed, forcing it to remain shutdown.");
                    BehaviorTreeResults newResult = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    newResult.orderInfo = new OrderInfo(OrderType.Stand);
                    __result = newResult;

                    QuipHelper.PublishQuip(mech, Mod.Config.Qips.Startup);
                }
                else
                {
                    Mod.Log.Info($" -- shutdown check passed, starting up normally.");
                }

                // TODO: Ammo explosion check
                // TODO: Pilot injury check
                // TODO: Component damage check
            }
            
        }

    }

   
}

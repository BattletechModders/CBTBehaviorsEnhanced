using BattleTech;
using Harmony;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
    public static class AbstractActor_InitEffectStats
    {
        static void Postfix(AbstractActor __instance)
        {
            Mod.Log.Info($"AA:IES entered- setting CanShootAfterSprinting for actor:{__instance.DisplayName}");
            __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
        }
    }
}

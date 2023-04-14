namespace CBTBehaviorsEnhanced.Patches
{

    // Override max engage distance to be sprinting
    [HarmonyPatch(typeof(Turret), "DamageLocation")]
    [HarmonyPriority(Priority.Last)]
    public static class Turret_DamageLocation
    {
        public static void Prefix(ref bool __runOriginal, Turret __instance)
        {
            if (!__runOriginal) return;

            // Invalidate any held state on damage
            ModState.InvalidateState(__instance);
        }
    }

}

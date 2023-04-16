namespace CBTBehaviorsEnhanced.Patches
{

    // Override max engage distance to be sprinting
    [HarmonyPatch(typeof(Vehicle), "DamageLocation")]
    [HarmonyPriority(Priority.Last)]
    public static class Vehicle_DamageLocation
    {
        public static void Prefix(ref bool __runOriginal, Vehicle __instance)
        {
            if (!__runOriginal) return;

            // Invalidate any held state on damage
            ModState.InvalidateState(__instance);
        }
    }

}

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed
    {

        public static void Postfix()
        {
            Mod.Log.Trace?.Write("CGS:OCGD - entered.");

            // Reset any combat state
            ModState.Reset();
        }
    }
}

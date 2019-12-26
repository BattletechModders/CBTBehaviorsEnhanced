using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee {

    [HarmonyPatch(typeof(MechDFASequence), "OnMeleeComplete")]
    public static class MechDFASequence_OnMeleeComplete {
        public static void Postfix(MechDFASequence __instance, MessageCenterMessage message) {
            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;
            if (!attackCompleteMessage.attackSequence.attackCompletelyMissed) {
                Mod.Log.Debug($" Kick attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.DFATarget)} succeeded.");

                // Check for source falling
                float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.MadeDFAFallChance, __instance.OwningMech, sourceSkillMulti, ModConfig.FT_Melee_DFA);
                if (!sourcePassed) {
                    Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check from DFA, forcing fall.");
                    MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Melee_DFA);
                }

                // Check for target falling
                if (__instance.DFATarget is Mech targetMech) {
                    float targetSkillMulti = targetMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                    bool targetPassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.HitByDFAFallChance, targetMech, targetSkillMulti, ModConfig.FT_Melee_DFA);
                    if (!targetPassed) {
                        Mod.Log.Info($"Target actor: {CombatantUtils.Label(targetMech)} failed pilot check from DFA, forcing fall.");
                        MechHelper.AddFallingSequence(targetMech, __instance, ModConfig.FT_Melee_DFA);
                    }
                } else {
                    Mod.Log.Debug($"Target {CombatantUtils.Label(__instance.DFATarget)} is not a mech, cannot fall - skipping.");
                }
            } else {
                Mod.Log.Debug($" DFA attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.DFATarget)} failed.");

                // Force the source mech to fall
                Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed DFA attack, forcing fall.");
                MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Melee_DFA);
            }
        }
    }
}

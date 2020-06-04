using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee {
  
    // Force the source or target to make a piloting check or fall down
    [HarmonyPatch(typeof(MechMeleeSequence), "OnMeleeComplete")]
    public static class MechMeleeSequence_OnMeleeComplete {
        public static void Postfix(MechMeleeSequence __instance, MessageCenterMessage message) {
            AttackCompleteMessage attackCompleteMessage = message as AttackCompleteMessage;

            if (__instance.selectedMeleeType == MeleeAttackType.Kick || __instance.selectedMeleeType == MeleeAttackType.Stomp) {
                if (attackCompleteMessage.attackSequence.attackCompletelyMissed) {
                    Mod.Log.Debug($" Kick attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} failed.");
                    // Check for source falling
                    float sourceMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.MissedKickFallChance, __instance.OwningMech, sourceMulti, ModConfig.FT_Melee_Kick);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check from missed kick, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Melee_Kick);
                    } 
                } else {
                    Mod.Log.Debug($" Kick attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} succeeded.");
                    // Check for target falling
                    if (__instance.MeleeTarget is Mech targetMech) {
                        float targetMulti = targetMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                        bool targetPassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.HitByKickFallChance, targetMech, targetMulti, ModConfig.FT_Melee_Kick);
                        if (!targetPassed) {
                            Mod.Log.Info($"Target actor: {CombatantUtils.Label(targetMech)} failed pilot check from kick, forcing fall.");
                            MechHelper.AddFallingSequence(targetMech, __instance, ModConfig.FT_Melee_Kick);
                        }
                    } else {
                        Mod.Log.Debug($"Target actor: {CombatantUtils.Label(__instance.MeleeTarget)} is not a mech, cannot fall - skipping.");
                    }
                }
            }

            if (__instance.selectedMeleeType == MeleeAttackType.Charge || __instance.selectedMeleeType == MeleeAttackType.Tackle) {
                if (!attackCompleteMessage.attackSequence.attackCompletelyMissed) {
                    Mod.Log.Debug($" Charge attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} succeeded.");

                    // Check for source falling
                    float sourceSkillMulti = __instance.OwningMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                    bool sourcePassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.MadeChargeFallChance, __instance.OwningMech, sourceSkillMulti, ModConfig.FT_Melee_Charge);
                    if (!sourcePassed) {
                        Mod.Log.Info($"Source actor: {CombatantUtils.Label(__instance.OwningMech)} failed pilot check from charge, forcing fall.");
                        MechHelper.AddFallingSequence(__instance.OwningMech, __instance, ModConfig.FT_Melee_Charge);
                    }

                    // Check for target falling
                    if (__instance.MeleeTarget is Mech targetMech) {
                        float targetSkillMulti = targetMech.PilotCheckMod(Mod.Config.Melee.SkillMulti);
                        bool targetPassed = CheckHelper.DidCheckPassThreshold(Mod.Config.Melee.HitByChargeFallChance, targetMech, targetSkillMulti, ModConfig.FT_Melee_Charge);
                        if (!targetPassed) {
                            Mod.Log.Info($"Target actor: {CombatantUtils.Label(targetMech)} failed pilot check from charge, forcing fall.");
                            MechHelper.AddFallingSequence(targetMech, __instance, ModConfig.FT_Melee_Charge);
                        }
                    } else {
                        Mod.Log.Debug($"Target actor: {CombatantUtils.Label(__instance.MeleeTarget)} is not a mech, cannot fall - skipping.");
                    }
                } else {
                    Mod.Log.Debug($" Charge attack by {CombatantUtils.Label(__instance.OwningMech)} vs. {CombatantUtils.Label(__instance.MeleeTarget)} failed.");
                }
            }

            // Reset melee state
            ModState.MeleeStates = null;
        }
    }
}

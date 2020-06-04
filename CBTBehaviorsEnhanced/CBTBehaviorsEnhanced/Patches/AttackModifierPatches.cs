using BattleTech;
using BattleTech.UI;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
    public static class ToHit_GetAllModifiers
    {
        private static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAM entered");

            if (__instance == null || weapon == null) return;

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound &&
                // Special trigger for dz's abilities
                !(ModConfig.dZ_Abilities && attacker.SkillTactics != 10))
            {
                __result = __result + (float)Mod.Config.ToHitSelfJumped;
            }

            // Check melee patches
            if (ModState.MeleeStates != null && weapon.Type == WeaponType.Melee)
            {
                if (weapon.WeaponSubType == WeaponSubType.Melee)
                {
                    int sumMod = 0;
                    foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                    {
                        string localText = new Text(Mod.Config.LocalizedAttackDescs[kvp.Key]).ToString();
                        Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");
                        sumMod += kvp.Value;
                    }

                    __result = __result + (float)sumMod;
                }   
                else if (weapon.WeaponSubType == WeaponSubType.DFA)
                {
                    // TODO: DFA - should work under above code?
                }
            }
        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
    public static class ToHit_GetAllModifiersDescription
    {
        private static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAMD entered");

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound)
            {
                __result = string.Format("{0}JUMPED {1:+#;-#}; ", __result, Mod.Config.ToHitSelfJumped);
            }

            // Check melee patches
            if (ModState.MeleeStates != null && weapon.Type == WeaponType.Melee)
            {
                if (weapon.WeaponSubType == WeaponSubType.Melee)
                {
                    foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                    {
                        string localText = new Text(Mod.Config.LocalizedAttackDescs[kvp.Key]).ToString();
                        Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");

                        __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, kvp.Value);
                    }
                }
                else if (weapon.WeaponSubType == WeaponSubType.DFA)
                {
                    // TODO: DFA - should work under above code?
                }
            }
        }
    }

    // Update the hover text in the case of a modifier
    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
    public static class CombatHUDWeaponSlot_SetHitChance
    {

        private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) return;

            Mod.Log.Trace("CHUDWS:SHC entered");

            Traverse addToolTipDetailT = Traverse.Create(__instance)
                .Method("AddToolTipDetail", new Type[] { typeof(string), typeof(int) });

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound)
            {
                string localText = new Text(Mod.Config.LocalizedAttackDescs[ModConfig.LT_AtkDesc_Attacker_Jumped]).ToString();
                Mod.Log.Trace($" Adding Attacker Jump modifier of: {Mod.Config.ToHitSelfJumped}");
                addToolTipDetailT.GetValue(new object[] { localText, Mod.Config.ToHitSelfJumped });
            }

            // Check melee patches
            if (ModState.MeleeStates != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                    {
                        string localText = new Text(Mod.Config.LocalizedAttackDescs[kvp.Key]).ToString();
                        Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");
                        addToolTipDetailT.GetValue(new object[] { localText, kvp.Value });
                    }
                }
                else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    // TODO: DFA - should work under above code?
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateMeleeWeapon")]
    [HarmonyPatch(new Type[] {})]
    public static class CombatHUDWeaponSlot_UpdateMeleeWeapon
    {

        private static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ModState.MeleeStates == null) return;

            Mod.Log.Trace("CHUDWS:UMW entered");

        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "GenerateToolTipStrings")]
    [HarmonyPatch(new Type[] { })]
    public static class CombatHUDWeaponSlot_GenerateToolTipStrings
    {

        private static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD, int ___displayedHeat)
        {

            if (__instance == null || ___displayedWeapon == null || ModState.MeleeStates == null) return;

            Mod.Log.Trace("CHUDWS:GTTS entered");

            // Check melee patches
            if (ModState.MeleeStates != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        // TODO: Localize
                        new Text("{0} dmg", targetDamage),
                        new Text("{0} stab", ModState.MeleeStates.SelectedState.TargetInstability),
                        new Text("+{0} heat", ___displayedHeat)
                    };
                    
                }
                else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    // TODO: DFA - should work under above code?
                }
            }
        }
    }
}

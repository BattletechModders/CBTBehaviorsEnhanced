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
    [HarmonyPatch(typeof(ToHit), "GetAllMeleeModifiers")]
    public static class ToHit_GetAllMeleeModifiers
    {
        private static void Postfix(ToHit __instance, ref float __result, Mech attacker, ICombatant target, Vector3 targetPosition, MeleeAttackType meleeAttackType)
        {
            Mod.Log.Trace("TH:GAMM entered");

            if (__instance == null || ModState.MeleeStates?.SelectedState == null) return;

            int sumMod = 0;
            foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
            {
                string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");
                sumMod += kvp.Value;
            }

            __result += (float)sumMod;            
            
        }
    }

    // Always return a zero for DFA modifiers; we'll handle it with our custom modifiers
    [HarmonyPatch(typeof(ToHit), "GetDFAModifier")]
    public static class ToHit_GetDFAModifier
    {
        private static void Postfix(ToHit __instance, ref float __result)
        {
            Mod.Log.Trace("TH:GDFAM entered");

            __result = 0;

        }
    }

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
                !(Mod.Config.Features.dZ_Abilities && attacker.SkillTactics != 10))
            {
                __result += (float)Mod.Config.ToHitSelfJumped;
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
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, Mod.Config.ToHitSelfJumped);
            }

            // Check melee patches
            if (ModState.MeleeStates != null && weapon.Type == WeaponType.Melee)
            {
 
                foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                {
                    string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                    Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");

                    __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, kvp.Value);
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
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                Mod.Log.Trace($" Adding Attacker Jump modifier of: {Mod.Config.ToHitSelfJumped}");
                addToolTipDetailT.GetValue(new object[] { localText, Mod.Config.ToHitSelfJumped });
            }

            // Check melee patches
            if (ModState.MeleeStates != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee ||
                    ___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                    {
                        string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                        Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}");
                        addToolTipDetailT.GetValue(new object[] { localText, kvp.Value });
                    }
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

            if (ModState.MeleeStates.SelectedState == null)
            {
                __instance.WeaponText.SetText("UNKNOWN");
                __instance.DamageText.SetText($"???");
            }
            else
            {
                string localText = new Text(ModState.MeleeStates.SelectedState.Label).ToString();
                __instance.WeaponText.SetText(localText);
                float totalDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                __instance.DamageText.SetText($"{totalDamage}");

            }
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
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee ||
                    ___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], ModState.MeleeStates.SelectedState.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };
                    
                }
                //else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                //{
                //    float targetDamage = ModState.MeleeStates.DFA.TargetDamageClusters.Sum();
                //    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                //    {
                //        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                //        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], ModState.MeleeStates.SelectedState.TargetInstability),
                //        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                //    };
                //}
            }
        }
    }
}

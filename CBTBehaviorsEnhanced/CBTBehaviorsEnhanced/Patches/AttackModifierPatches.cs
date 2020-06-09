using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using IRBTModUtils;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(ToHit), "GetAllMeleeModifiers")]
    static class ToHit_GetAllMeleeModifiers
    {
        static void Postfix(ToHit __instance, ref float __result, Mech attacker, ICombatant target, Vector3 targetPosition, MeleeAttackType meleeAttackType)
        {
            Mod.Log.Trace("TH:GAMM entered");

            if (__instance == null || ModState.MeleeStates?.SelectedState == null) return;

            Mod.Log.Info("Adding CBTBE modifiers to tohit total");
            int sumMod = 0;
            foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
            {
                string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                Mod.Log.Info($" - Found attack modifier: {localText} = {kvp.Value}, adding to sum modifier");
                sumMod += kvp.Value;
            }

            __result += (float)sumMod;

        }
    }

    // Always return a zero for DFA modifiers; we'll handle it with our custom modifiers
    [HarmonyPatch(typeof(ToHit), "GetDFAModifier")]
    static class ToHit_GetDFAModifier
    {
        static void Postfix(ToHit __instance, ref float __result)
        {
            Mod.Log.Trace("TH:GDFAM entered");

            __result = 0;

        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
    static class ToHit_GetAllModifiers
    {
        static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAM entered");

            if (__instance == null || weapon == null) return;

            Mod.Log.Info("GETTING ALL MODS");
            if (
                (attacker.HasMovedThisRound && attacker.JumpedLastRound) ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump)
                )
            {
                __result += (float)Mod.Config.ToHitSelfJumped;
            }
        }
    }

    [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
    static class ToHit_GetAllModifiersDescription
    {
        static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
        {
            Mod.Log.Trace("TH:GAMD entered");

            if (attacker.HasMovedThisRound && attacker.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
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
                    Mod.Log.Info($" - Found attack modifier for desc: {localText} = {kvp.Value}");

                    __result = string.Format("{0}{1} {2:+#;-#}; ", __result, localText, kvp.Value);
                }

            }
        }
    }

    //Update the hover text in the case of a modifier
   [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
    static class CombatHUDWeaponSlot_SetHitChance
    {

        static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) return;

            Mod.Log.Trace("CHUDWS:SHC entered");

            Traverse addToolTipDetailT = Traverse.Create(__instance)
                .Method("AddToolTipDetail", new Type[] { typeof(string), typeof(int) });

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
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
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_UpdateMeleeWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
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
                float totalDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                Mod.Log.Info($"Setting Melee Weapon to label: {localText} and damage: {totalDamage}");
                __instance.WeaponText.SetText(localText);
                __instance.DamageText.SetText($"{totalDamage}");

            }
        }
    }


    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateDFAWeapon")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_UpdateDFAWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ModState.MeleeStates == null) return;
            Mod.Log.Trace("CHUDWS:UDFAW entered");

            if (ModState.MeleeStates.SelectedState == null)
            {
                __instance.WeaponText.SetText("UNKNOWN");
                __instance.DamageText.SetText($"???");
            }
            else
            {
                string localText = new Text(ModState.MeleeStates.SelectedState.Label).ToString();
                float totalDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                Mod.Log.Info($"Setting DFA Weapon to label: {localText} and damage: {totalDamage}");
                __instance.WeaponText.SetText(localText);
                __instance.DamageText.SetText($"{totalDamage}");

            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "GenerateToolTipStrings")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_GenerateToolTipStrings
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD, int ___displayedHeat)
        {

            if (___displayedWeapon == null) Mod.Log.Warn("EMPTY WEAPON IN GENERATE TOOL TIPS");
            else Mod.Log.Info($"GENERATING TOOLTIPS FOR WEAPON: {___displayedWeapon.UIName}");

            if (__instance == null || ___displayedWeapon == null || ModState.MeleeStates == null) return;

            Mod.Log.Trace("CHUDWS:GTTS entered");

            // Check melee patches
            if (ModState.MeleeStates != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    Mod.Log.Info($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {ModState.MeleeStates.SelectedState.TargetInstability}  " +
                        $"heat: {___displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], ModState.MeleeStates.SelectedState.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };
                } else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    Mod.Log.Info($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {ModState.MeleeStates.SelectedState.TargetInstability}  " +
                        $"heat: {___displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], ModState.MeleeStates.SelectedState.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };

                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDTooltipHoverElement), "OnPointerEnter")]
    [HarmonyPatch(new Type[] { typeof(PointerEventData) } )]
    static class CombatHUDTooltipHoverElement_OnPointerEnter
    {

        static void Prefix(CombatHUDTooltipHoverElement __instance)
        {
            Mod.Log.Info($"CHUDTHE - entered!");
            Mod.Log.Info($"  BasicStrings: '{__instance.BasicString?.ToString()}'");
            Mod.Log.Info($"  ExtraStrings count: '{__instance.ExtraStrings.Count}'");
            Mod.Log.Info($"  ToggleUINode: '{LazySingletonBehavior<UIManager>.Instance.ToggleUINode}'");
            Mod.Log.Info($"  ActiveInHierachy: {__instance.gameObject.activeInHierarchy}");
        }
    }

}

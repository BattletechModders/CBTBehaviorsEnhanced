using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
using Harmony;
using IRBTModUtils;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBTBehaviorsEnhanced.Patches
{
    //Update the hover text in the case of a modifier
    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
    static class CombatHUDWeaponSlot_SetHitChance
    {

        static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) return;

            Mod.Log.Trace?.Write("CHUDWS:SHC entered");

            Traverse addToolTipDetailT = Traverse.Create(__instance)
                .Method("AddToolTipDetail", new Type[] { typeof(string), typeof(int) });

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
            {
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                Mod.Log.Debug?.Write($" Adding Attacker Jump modifier of: {Mod.Config.ToHitSelfJumped}");
                addToolTipDetailT.GetValue(new object[] { localText, Mod.Config.ToHitSelfJumped });
            }

            // Check melee patches
            if (ModState.MeleeStates?.SelectedState != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee ||
                    ___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    foreach (KeyValuePair<string, int> kvp in ModState.MeleeStates.SelectedState.AttackModifiers)
                    {
                        string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                        Mod.MeleeLog.Debug?.Write($" - SetHitChance found attack modifier: {localText} = {kvp.Value}");
                        addToolTipDetailT.GetValue(new object[] { localText, kvp.Value });
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "RefreshDisplayedWeapon", new Type[] { typeof(ICombatant), typeof(int?), typeof(bool), typeof(bool) })]
    static class CombatHUDWeaponSlot_RefreshDisplayedWeapon
    {
        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon)
        {

            if (__instance == null || ___displayedWeapon == null || !Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack) return;

            if (ModState.MeleeStates != null && ModState.MeleeStates.SelectedState != null)
            {
                // Check if the weapon can fire according to the select melee type
                bool isAllowed = ModState.MeleeStates.SelectedState.IsRangedWeaponAllowed(___displayedWeapon);
                Mod.MeleeLog.Debug?.Write($"Ranged weapon '{___displayedWeapon.UIName}' can fire in melee by type? {isAllowed}");

                if (!isAllowed)
                {
                    Mod.MeleeLog.Debug?.Write($"Disabling weapon from selection");
                    __instance.ToggleButton.isChecked = false;
                    Traverse showDisabledHexT = Traverse.Create(__instance).Method("ShowDisabledHex");
                    showDisabledHexT.GetValue();
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

            if (__instance == null || ___displayedWeapon == null) return;
            Mod.Log.Trace?.Write("CHUDWS:UMW entered");

            if (ModState.MeleeStates == null || ModState.MeleeStates.SelectedState == null || ModState.MeleeStates.SelectedState == ModState.MeleeStates.DFA)
            {
                string weaponLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_Weapon], 
                    new object[] { Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type] }
                    ).ToString();
                __instance.WeaponText.SetText(weaponLabel);

                Mech parentMech = ___displayedWeapon.parent as Mech;

                ActorMeleeCondition meleeCondition = ModState.GetMeleeCondition(parentMech);

                float kickDam = parentMech.KickDamage();               
                float punchDam = parentMech.PunchDamage();
                float weapDam = parentMech.PhysicalWeaponDamage();
                string damageText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type_Damage],
                    new object[] { weapDam, punchDam, kickDam }
                    ).ToString();
                __instance.DamageText.SetText(damageText);
            }
            else if (ModState.MeleeStates.SelectedState != ModState.MeleeStates.DFA)
            {
                string attackName = "UNKNOWN";
                if (ModState.MeleeStates.SelectedState == ModState.MeleeStates.Charge)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge];
                else if (ModState.MeleeStates.SelectedState == ModState.MeleeStates.Kick)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Kick];
                else if (ModState.MeleeStates.SelectedState == ModState.MeleeStates.PhysicalWeapon)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Physical_Weapon];
                else if (ModState.MeleeStates.SelectedState == ModState.MeleeStates.Punch)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Punch];
                
                string weaponLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_Weapon],
                    new object[] { attackName }
                    ).ToString();
                __instance.WeaponText.SetText(weaponLabel);


                float totalDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                if (ModState.MeleeStates.SelectedState.TargetDamageClusters.Length > 1)
                {
                    int avgDamage = (int)Math.Floor(totalDamage / ModState.MeleeStates.SelectedState.TargetDamageClusters.Length);
                    __instance.DamageText.SetText($"{avgDamage} <size=80%>(x{ModState.MeleeStates.SelectedState.TargetDamageClusters.Length})");
                }
                else
                {
                    __instance.DamageText.SetText($"{totalDamage}");
                }

                ___displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, totalDamage);
                ___displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, ModState.MeleeStates.SelectedState.TargetInstability);
                CustAmmoCategories.DamageModifiersCache.ClearDamageCache(___displayedWeapon);
            }
        }
    }


    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateDFAWeapon")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_UpdateDFAWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null) return;
            Mod.Log.Trace?.Write("CHUDWS:UDFAW entered");

            if (ModState.MeleeStates == null || ModState.MeleeStates.DFA == null)
            {
                Mod.MeleeLog.Debug?.Write("Defaulting DFA damage.");

                Mech parentMech = ___displayedWeapon.parent as Mech;
                float targetDamage = parentMech.DFATargetDamage();
                __instance.DamageText.SetText($"{targetDamage}");
            }
            else if (ModState.MeleeStates.DFA != null)
            {
                Mod.MeleeLog.Debug?.Write("Updating labels for DFA state.");

                float totalDamage = ModState.MeleeStates.DFA.TargetDamageClusters.Sum();
                if (ModState.MeleeStates.DFA.TargetDamageClusters.Length > 1)
                {
                    int avgDamage = (int)Math.Floor(totalDamage / ModState.MeleeStates.DFA.TargetDamageClusters.Length);
                    string damageS = $"{avgDamage} <size=80%>(x{ModState.MeleeStates.DFA.TargetDamageClusters.Length})";
                    Mod.MeleeLog.Debug?.Write($"  - damageS is: {damageS}");
                    __instance.DamageText.SetText(damageS);
                }
                else
                {
                    __instance.DamageText.SetText($"{totalDamage}");
                    Mod.MeleeLog.Debug?.Write($"  - damageS is: {totalDamage}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "GenerateToolTipStrings")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_GenerateToolTipStrings
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD, int ___displayedHeat)
        {

            if (__instance == null || ___displayedWeapon == null || ModState.MeleeStates == null) return;

            Mod.Log.Trace?.Write("CHUDWS:GTTS entered");

            // Check melee patches
            if (ModState.MeleeStates?.SelectedState != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    Mod.MeleeLog.Trace?.Write($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {ModState.MeleeStates.SelectedState.TargetInstability}  " +
                        $"heat: {___displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], ModState.MeleeStates.SelectedState.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };
                }
                else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    float targetDamage = ModState.MeleeStates.SelectedState.TargetDamageClusters.Sum();
                    Mod.MeleeLog.Trace?.Write($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
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
}

using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.MeleeStates;
using IRBTModUtils;
using IRBTModUtils.Extension;
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

        static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target)
        {

            if (__instance == null || __instance.displayedWeapon == null || __instance.HUD.SelectedActor == null || target == null) return;

            Mod.UILog.Trace?.Write("CHUDWS:SHC entered");

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
            {
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                Mod.UILog.Trace?.Write($"Adding Attacker Jump modifier of: {Mod.Config.ToHitSelfJumped}");
                __instance.AddToolTipDetail(localText, Mod.Config.ToHitSelfJumped);
            }

            // Check melee patches
            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.HUD.SelectedActor);
            if (selectedAttack != null && __instance.displayedWeapon.Type == WeaponType.Melee)
            {
                if (__instance.displayedWeapon.WeaponSubType == WeaponSubType.Melee ||
                    __instance.displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    foreach (KeyValuePair<string, int> kvp in selectedAttack.AttackModifiers)
                    {
                        string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                        Mod.UILog.Debug?.Write($" - SetHitChance found attack modifier: {localText} = {kvp.Value}");
                        __instance.AddToolTipDetail(localText, kvp.Value);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "RefreshDisplayedWeapon",
        new Type[] { typeof(ICombatant), typeof(int?), typeof(bool), typeof(bool) })]
    [HarmonyAfter("io.mission.modrepuation", "io.mission.customunits")]
    static class CombatHUDWeaponSlot_RefreshDisplayedWeapon
    {
        static void Prefix(ref bool __runOriginal, CombatHUDWeaponSlot __instance)
        {

            if (!__runOriginal) return;

            if (__instance == null || __instance.displayedWeapon == null || __instance.HUD.SelectedActor == null ||
                !Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack) return;

            if (!(__instance.weaponSlotType == CombatHUDWeaponSlot.WeaponSlotType.Normal))
            {
                Mod.UILog.Trace?.Write($"RefreshDisplayedWeapon:PRE - Updating {__instance.HUD.SelectedActor.DistinctId()} " +
                    $"melee or dfa weapon with text: {__instance.DamageText.text}");
                return; // Skip melee and dfa weapons, let normal processing handle them
            }


            if (ModState.MeleeAttackContainer?.activeInHierarchy == true || // Handle normal melee states
                SharedState.CombatHUD?.AttackModeSelector?.FireButton?.CurrentFireMode == CombatHUDFireButton.FireMode.DFA) // Handle DFA state
            {
                MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.HUD.SelectedActor);
                if (selectedAttack != null)
                {
                    Mod.UILog.Trace?.Write($"Checking ranged weapons attacker: {__instance.HUD.SelectedActor.DistinctId()} using selectedAttack: {selectedAttack.Label}");

                    // Check if the weapon can fire according to the select melee type
                    bool isAllowed = selectedAttack.IsRangedWeaponAllowed(__instance.displayedWeapon);
                    Mod.UILog.Trace?.Write($"Ranged weapon '{__instance.displayedWeapon.UIName}' can fire in melee by type? {isAllowed}");

                    if (!isAllowed)
                    {
                        Mod.UILog.Trace?.Write($"Disabling weapon from selection");
                        __instance.displayedWeapon.StatCollection.Set(ModStats.HBS_Weapon_Temporarily_Disabled, true);
                        return;
                    }
                }
            }



            __instance.displayedWeapon.StatCollection.Set(ModStats.HBS_Weapon_Temporarily_Disabled, false);
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateMeleeWeapon", new Type[] { })]
    [HarmonyAfter("io.mission.modrepuation", "io.mission.customunits")]
    static class CombatHUDWeaponSlot_UpdateMeleeWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance)
        {

            if (__instance == null || __instance.displayedWeapon == null || __instance.HUD.SelectedActor == null) return;
            Mod.UILog.Trace?.Write("CHUDWS:UMW entered");
            Mod.UILog.Debug?.Write($"UpdateMeleeWeapon called for: {__instance.HUD.SelectedActor.DistinctId()} using " +
                $"weapon: {__instance.displayedWeapon.UIName}_{__instance.displayedWeapon.uid}");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.HUD.SelectedActor);
            if (selectedAttack == null || selectedAttack is DFAAttack)
            {
                string weaponLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_Weapon],
                    new object[] { Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type] }
                    ).ToString();
                __instance.WeaponText.SetText(weaponLabel);

                Mech parentMech = __instance.displayedWeapon.parent as Mech;

                float kickDam = parentMech.KickDamage();
                float punchDam = parentMech.PunchDamage();
                float weapDam = parentMech.PhysicalWeaponDamage();
                string damageText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type_Damage],
                    new object[] { weapDam, punchDam, kickDam }
                    ).ToString();
                __instance.DamageText.SetText(damageText);
                Mod.UILog.Trace?.Write($"  -- default attackType has no damage, using damageText: {damageText}");
            }
            else if (selectedAttack is ChargeAttack || selectedAttack is KickAttack || selectedAttack is PunchAttack || selectedAttack is WeaponAttack)
            {
                string attackName = "UNKNOWN";
                if (selectedAttack is ChargeAttack)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Charge];
                else if (selectedAttack is KickAttack)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Kick];
                else if (selectedAttack is WeaponAttack)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Physical_Weapon];
                else if (selectedAttack is PunchAttack)
                    attackName = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Punch];

                string weaponLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_Weapon],
                    new object[] { attackName }
                    ).ToString();
                __instance.WeaponText.SetText(weaponLabel);

                float totalDamage = selectedAttack.TargetDamageClusters.Sum();
                Mod.UILog.Debug?.Write($" -- attackState: {attackName} has targetDamageClusters: {selectedAttack.TargetDamageClusters.Length}" +
                    $"  with totalDamage: {totalDamage}");

                __instance.displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, totalDamage);
                __instance.displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, selectedAttack.TargetInstability);
                CustAmmoCategories.DamageModifiersCache.ClearDamageCache(__instance.displayedWeapon);

                string damageTextS = $"{totalDamage}";
                Mod.UILog.Trace?.Write($"  -- using damageText: {damageTextS}");
                __instance.DamageText.SetText(damageTextS);

            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateDFAWeapon", new Type[] { })]
    [HarmonyAfter("io.mission.modrepuation", "io.mission.customunits")]
    static class CombatHUDWeaponSlot_UpdateDFAWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance)
        {

            if (__instance == null || __instance.displayedWeapon == null || __instance.HUD.SelectedActor == null) return;
            Mod.UILog.Trace?.Write("CHUDWS:UDFAW entered");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.HUD.SelectedActor);
            if (selectedAttack == null || !(selectedAttack is DFAAttack))
            {
                Mod.UILog.Trace?.Write("Defaulting DFA damage.");

                Mech parentMech = __instance.displayedWeapon.parent as Mech;
                float targetDamage = parentMech.DFATargetDamage();
                __instance.DamageText.SetText($"{targetDamage}");
            }
            else if (selectedAttack is DFAAttack)
            {
                Mod.UILog.Debug?.Write("Updating labels for DFA state.");

                float totalDamage = selectedAttack.TargetDamageClusters.Sum();
                Mod.UILog.Trace?.Write($"  - damageS is: {totalDamage}");
                __instance.DamageText.SetText($"{totalDamage}");

            }
        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "GenerateToolTipStrings")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_GenerateToolTipStrings
    {

        static void Postfix(CombatHUDWeaponSlot __instance)
        {

            if (__instance == null || __instance.displayedWeapon == null || __instance.HUD.SelectedActor == null) return;

            Mod.UILog.Trace?.Write("CHUDWS:GTTS entered");

            // Check melee patches
            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.HUD.SelectedActor);
            if (selectedAttack != null && __instance.displayedWeapon.Type == WeaponType.Melee)
            {
                if (__instance.displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    float targetDamage = selectedAttack.TargetDamageClusters.Sum();
                    Mod.UILog.Trace?.Write($" - Extra Strings for type: {__instance.displayedWeapon.Type} && {__instance.displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {selectedAttack.TargetInstability}  " +
                        $"heat: {__instance.displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], selectedAttack.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], __instance.displayedHeat)
                    };
                }
                else if (__instance.displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    float targetDamage = selectedAttack.TargetDamageClusters.Sum();
                    Mod.UILog.Trace?.Write($" - Extra Strings for type: {__instance.displayedWeapon.Type} && {__instance.displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {selectedAttack.TargetInstability}  " +
                        $"heat: {__instance.displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], selectedAttack.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], __instance.displayedHeat)
                    };

                }
            }
        }
    }
}

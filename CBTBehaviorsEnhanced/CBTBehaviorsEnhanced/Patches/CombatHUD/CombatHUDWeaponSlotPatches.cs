using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
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

        static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null || target == null) return;

            Mod.UILog.Trace?.Write("CHUDWS:SHC entered");

            Traverse addToolTipDetailT = Traverse.Create(__instance)
                .Method("AddToolTipDetail", new Type[] { typeof(string), typeof(int) });

            AbstractActor actor = __instance.DisplayedWeapon.parent;
            if (actor.HasMovedThisRound && actor.JumpedLastRound ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump))
            {
                string localText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Attacker_Jumped]).ToString();
                Mod.UILog.Debug?.Write($" Adding Attacker Jump modifier of: {Mod.Config.ToHitSelfJumped}");
                addToolTipDetailT.GetValue(new object[] { localText, Mod.Config.ToHitSelfJumped });
            }

            // Check melee patches
            MeleeAttack selectedAttack = ModState.GetSelectedAttack(___HUD.SelectedActor);
            if (selectedAttack != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee ||
                    ___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    foreach (KeyValuePair<string, int> kvp in selectedAttack.AttackModifiers)
                    {
                        string localText = new Text(Mod.LocalizedText.Labels[kvp.Key]).ToString();
                        Mod.UILog.Debug?.Write($" - SetHitChance found attack modifier: {localText} = {kvp.Value}");
                        addToolTipDetailT.GetValue(new object[] { localText, kvp.Value });
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
        static void Prefix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD, CombatHUDWeaponSlot.WeaponSlotType ___weaponSlotType)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null ||
                !Mod.Config.Melee.FilterCanUseInMeleeWeaponsByAttack) return;

            if (!(___weaponSlotType == CombatHUDWeaponSlot.WeaponSlotType.Normal))
            {
                Mod.UILog.Debug?.Write($"RefreshDisplayedWeapon:PRE - Updating {___HUD.SelectedActor.DistinctId()} " +
                    $"melee or dfa weapon with text: {__instance.DamageText.text}");
                return; // Skip melee and dfa weapons, let normal processing handle them
            }

            if (ModState.MeleeAttackContainer?.activeInHierarchy == true)
            {
                MeleeAttack selectedAttack = ModState.GetSelectedAttack(___HUD.SelectedActor);
                if (selectedAttack != null)
                {
                    Mod.UILog.Debug?.Write($"Checking ranged weapons attacker: {___HUD.SelectedActor.DistinctId()} using selectedAttack: {selectedAttack.Label}");

                    // Check if the weapon can fire according to the select melee type
                    bool isAllowed = selectedAttack.IsRangedWeaponAllowed(___displayedWeapon);
                    Mod.UILog.Debug?.Write($"Ranged weapon '{___displayedWeapon.UIName}' can fire in melee by type? {isAllowed}");

                    if (!isAllowed)
                    {
                        Mod.UILog.Trace?.Write($"Disabling weapon from selection");
                        ___displayedWeapon.StatCollection.Set(ModConsts.HBS_Weapon_Temporarily_Disabled, true);
                        return;
                    }
                }
            }

            ___displayedWeapon.StatCollection.Set(ModConsts.HBS_Weapon_Temporarily_Disabled, false);
        }

        static void Postfix(CombatHUDWeaponSlot __instance, CombatHUD ___HUD, CombatHUDWeaponSlot.WeaponSlotType ___weaponSlotType)
        {
            if (!(___weaponSlotType == CombatHUDWeaponSlot.WeaponSlotType.Normal))
            {
                Mod.UILog.Debug?.Write($"RefreshDisplayedWeapon:POST - Updating {___HUD.SelectedActor.DistinctId()} " +
                    $"melee or dfa weapon with text: {__instance.DamageText.text}");
            }

        }
    }

    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateMeleeWeapon")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_UpdateMeleeWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null) return;
            Mod.UILog.Trace?.Write("CHUDWS:UMW entered");
            Mod.UILog.Debug?.Write($"UpdateMeleeWeapon called for: {___HUD.SelectedActor.DistinctId()} using " +
                $"weapon: {___displayedWeapon.UIName}_{___displayedWeapon.uid}");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(___HUD.SelectedActor);
            if (selectedAttack == null || selectedAttack is DFAAttack)
            {
                string weaponLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_Weapon], 
                    new object[] { Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type] }
                    ).ToString();
                __instance.WeaponText.SetText(weaponLabel);

                Mech parentMech = ___displayedWeapon.parent as Mech;

                float kickDam = parentMech.KickDamage();               
                float punchDam = parentMech.PunchDamage();
                float weapDam = parentMech.PhysicalWeaponDamage();
                string damageText = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Panel_Melee_No_Attack_Type_Damage],
                    new object[] { weapDam, punchDam, kickDam }
                    ).ToString();
                __instance.DamageText.SetText(damageText);
                Mod.UILog.Debug?.Write($"  -- default attackType has no damage, using damageText: {damageText}");
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

                ___displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_DamagePerShot, totalDamage);
                ___displayedWeapon.StatCollection.Set<float>(ModStats.HBS_Weapon_Instability, selectedAttack.TargetInstability);
                CustAmmoCategories.DamageModifiersCache.ClearDamageCache(___displayedWeapon);

                string damageTextS = $"{totalDamage}";
                if (selectedAttack.TargetDamageClusters.Length > 1)
                {
                    int avgDamage = (int)Math.Floor(totalDamage / selectedAttack.TargetDamageClusters.Length);
                    damageTextS = $"{avgDamage} <size=80%>(x{selectedAttack.TargetDamageClusters.Length})";
                }
                Mod.UILog.Debug?.Write($"  -- using damageText: {damageTextS}");
                __instance.DamageText.SetText(damageTextS);

            }
        }
    }


    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "UpdateDFAWeapon")]
    [HarmonyPatch(new Type[] { })]
    static class CombatHUDWeaponSlot_UpdateDFAWeapon
    {

        static void Postfix(CombatHUDWeaponSlot __instance, Weapon ___displayedWeapon, CombatHUD ___HUD)
        {

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null) return;
            Mod.UILog.Trace?.Write("CHUDWS:UDFAW entered");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(___HUD.SelectedActor);
            if (selectedAttack == null || !(selectedAttack is DFAAttack))
            {
                Mod.UILog.Trace?.Write("Defaulting DFA damage.");

                Mech parentMech = ___displayedWeapon.parent as Mech;
                float targetDamage = parentMech.DFATargetDamage();
                __instance.DamageText.SetText($"{targetDamage}");
            }
            else if (selectedAttack is DFAAttack)
            {
                Mod.UILog.Debug?.Write("Updating labels for DFA state.");

                float totalDamage = selectedAttack.TargetDamageClusters.Sum();
                if (selectedAttack.TargetDamageClusters.Length > 1)
                {
                    int avgDamage = (int)Math.Floor(totalDamage / selectedAttack.TargetDamageClusters.Length);
                    string damageS = $"{avgDamage} <size=80%>(x{selectedAttack.TargetDamageClusters.Length})";
                    Mod.UILog.Debug?.Write($"  - damageS is: {damageS}");
                    __instance.DamageText.SetText(damageS);
                }
                else
                {
                    __instance.DamageText.SetText($"{totalDamage}");
                    Mod.UILog.Debug?.Write($"  - damageS is: {totalDamage}");
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

            if (__instance == null || ___displayedWeapon == null || ___HUD.SelectedActor == null) return;

            Mod.UILog.Trace?.Write("CHUDWS:GTTS entered");

            // Check melee patches
            MeleeAttack selectedAttack = ModState.GetSelectedAttack(___HUD.SelectedActor);
            if (selectedAttack != null && ___displayedWeapon.Type == WeaponType.Melee)
            {
                if (___displayedWeapon.WeaponSubType == WeaponSubType.Melee)
                {
                    float targetDamage = selectedAttack.TargetDamageClusters.Sum();
                    Mod.UILog.Trace?.Write($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {selectedAttack.TargetInstability}  " +
                        $"heat: {___displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], selectedAttack.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };
                }
                else if (___displayedWeapon.WeaponSubType == WeaponSubType.DFA)
                {
                    float targetDamage = selectedAttack.TargetDamageClusters.Sum();
                    Mod.UILog.Trace?.Write($" - Extra Strings for type: {___displayedWeapon.Type} && {___displayedWeapon.WeaponSubType} " +
                        $"=> Damage: {targetDamage}  instability: {selectedAttack.TargetInstability}  " +
                        $"heat: {___displayedHeat}");
                    __instance.ToolTipHoverElement.ExtraStrings = new List<Text>
                    {
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Damage], targetDamage),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Instability], selectedAttack.TargetInstability),
                        new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Weapon_Hover_Heat], ___displayedHeat)
                    };

                }
            }
        }
    }
}

using BattleTech;
using Harmony;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.Melee
{
    [HarmonyPatch(typeof(MeleeRules), "SelectRandomMeleeAttack")]
    static class MeleeRules_SelectRandomMeleeAttack
    {
        static void Postfix(MeleeRules __instance, Mech attacker, Vector3 attackPosition, ICombatant target, float rnd, ref MeleeAttackType __result)
        {
            float scale = 0f;
            List<MeleeRules.MeleeWeight> validMeleeAttackTypes = __instance.GetValidMeleeAttackTypes(attacker, attackPosition, target, out scale);
            foreach (MeleeRules.MeleeWeight weight in validMeleeAttackTypes)
            {
                if (weight.attackType == MeleeAttackType.Punch)
                {
                    __result = MeleeAttackType.Punch;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MechMeleeSequence), "BuildMeleeDirectorSequence")]
    [HarmonyBefore("io.mission.modrepuation")]
    static class MechMeleeSequence_BuildMeleeDirectorSequence
    {
        static void Prefix(MechMeleeSequence __instance)
        {
            Mod.Log.Info($"Setting current melee type to: {__instance.selectedMeleeType} and weapon to: {__instance.OwningMech.MeleeWeapon}");
            ModState.CurrentMeleeType = __instance.selectedMeleeType;
            ModState.CurrentMeleeWeapon = __instance.OwningMech.MeleeWeapon;
        }

        static void Postfix(MechMeleeSequence __instance)
        {
            ModState.CurrentMeleeType = MeleeAttackType.NotSet;
            ModState.CurrentMeleeWeapon = null;
        }
    }

    [HarmonyPatch(typeof(HitLocation), "GetMechHitTable")]
    static class HitLocation_GetMechHitTable
    {
        static void Postfix(HitLocation __instance, AttackDirection from, bool log, ref Dictionary<ArmorLocation, int> __result)
        {
            // If this attack isn't a melee attack, abort
            if (ModState.CurrentMeleeType == MeleeAttackType.NotSet || ModState.CurrentMeleeWeapon == null) return;

            if (ModState.CurrentMeleeType == MeleeAttackType.Kick)
            {
                Mod.Log.Info($"Attack was a kick, using kick dictionary.");
                __result.Clear();
                __result.Add(ArmorLocation.LeftLeg, 50);
                __result.Add(ArmorLocation.RightLeg, 50);
            }
            else if (ModState.CurrentMeleeType == MeleeAttackType.Punch || ModState.CurrentMeleeType == MeleeAttackType.DFA)
            {
                __result.Clear();
                Mod.Log.Info($"Attack was a Punch or DFA, using punch dictionary.");
                if (from == AttackDirection.FromLeft)
                {
                    __result.Add(ArmorLocation.LeftTorso, 34); // 2 locations
                    __result.Add(ArmorLocation.CenterTorso, 16);
                    __result.Add(ArmorLocation.LeftArm, 34); // 2 locations
                    __result.Add(ArmorLocation.Head, 16);
                }
                else if (from == AttackDirection.FromBack)
                {
                    __result.Add(ArmorLocation.LeftArm, 17);
                    __result.Add(ArmorLocation.LeftTorsoRear, 17);
                    __result.Add(ArmorLocation.CenterTorsoRear, 16);
                    __result.Add(ArmorLocation.RightTorsoRear, 17);
                    __result.Add(ArmorLocation.RightArm, 17);
                    __result.Add(ArmorLocation.Head, 16);
                }
                else if (from == AttackDirection.FromRight)
                {
                    __result.Add(ArmorLocation.RightTorso, 34); // 2 locations
                    __result.Add(ArmorLocation.CenterTorso, 16);
                    __result.Add(ArmorLocation.RightArm, 34); // 2 locations
                    __result.Add(ArmorLocation.Head, 16);
                }
                else
                {
                    __result.Add(ArmorLocation.LeftArm, 17);
                    __result.Add(ArmorLocation.LeftTorso, 17);
                    __result.Add(ArmorLocation.CenterTorso, 16);
                    __result.Add(ArmorLocation.RightTorso, 17);
                    __result.Add(ArmorLocation.RightArm, 17);
                    __result.Add(ArmorLocation.Head, 16);
                }
            }            

        }
    }

}

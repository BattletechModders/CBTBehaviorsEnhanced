using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced.Patches.Melee
{

    [HarmonyPatch(typeof(HitLocation), "GetMechHitTable")]
    static class HitLocation_GetMechHitTable
    {
        static void Postfix(AttackDirection from, ref Dictionary<ArmorLocation, int> __result)
        {
            // If this attack isn't a melee attack, abort
            if (ModState.MeleeType == MeleeAttackType.NotSet || ModState.MeleeWeapon == null ||
                ModState.ForceDamageTable == DamageTable.NONE || ModState.ForceDamageTable == DamageTable.STANDARD) return;

            if (ModState.ForceDamageTable == DamageTable.PUNCH)
            {
                Mod.Log.Info?.Write($"Attack will use the PUNCH damage table");
                __result = new Dictionary<ArmorLocation, int>();
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
            else if (ModState.ForceDamageTable == DamageTable.KICK)
            {
                Mod.Log.Info?.Write($"Attack will use the KICK damage table.");
                __result = new Dictionary<ArmorLocation, int>();
                __result.Add(ArmorLocation.LeftLeg, 50);
                __result.Add(ArmorLocation.RightLeg, 50);
            }
        }
    }

}

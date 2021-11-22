using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced.Patches.Melee
{

    [HarmonyPatch(typeof(HitLocation), "GetVehicleHitTable")]
    static class HitLocation_GetVehicleHitTable
    {
        static void Postfix(AttackDirection from, bool log, ref Dictionary<VehicleChassisLocations, int> __result)
        {
            if (ModState.ForceDamageTable == DamageTable.PUNCH)
            {
                Mod.Log.Info?.Write($"Attack against VEHICLE will use the PUNCH damage table");
                __result = new Dictionary<VehicleChassisLocations, int>();
                if (from == AttackDirection.FromLeft)
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Left, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Rear, 8);
                }
                else if (from == AttackDirection.FromBack)
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Rear, 40);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
                else if (from == AttackDirection.FromRight)
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Right, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Rear, 8);
                }
                else if (from == AttackDirection.FromTop)
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
                else
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Front, 40);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
            }
            else if (ModState.ForceDamageTable == DamageTable.KICK)
            {
                Mod.Log.Info?.Write($"Attack against VEHICLE will use the KICK damage table");
                __result = new Dictionary<VehicleChassisLocations, int>();
                if (from == AttackDirection.FromLeft)
                {
                    __result.Add(VehicleChassisLocations.Turret, 4);
                    __result.Add(VehicleChassisLocations.Left, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Rear, 8);
                }
                else if (from == AttackDirection.FromBack)
                {
                    __result.Add(VehicleChassisLocations.Turret, 4);
                    __result.Add(VehicleChassisLocations.Rear, 40);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
                else if (from == AttackDirection.FromRight)
                {
                    __result.Add(VehicleChassisLocations.Turret, 4);
                    __result.Add(VehicleChassisLocations.Right, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Rear, 8);
                }
                else if (from == AttackDirection.FromTop)
                {
                    __result.Add(VehicleChassisLocations.Turret, 40);
                    __result.Add(VehicleChassisLocations.Front, 8);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
                else
                {
                    __result.Add(VehicleChassisLocations.Turret, 4);
                    __result.Add(VehicleChassisLocations.Front, 40);
                    __result.Add(VehicleChassisLocations.Left, 8);
                    __result.Add(VehicleChassisLocations.Right, 8);
                }
            }
            else
            {
                return;
            }
        }
    }


    [HarmonyPatch(typeof(HitLocation), "GetMechHitTable")]
    static class HitLocation_GetMechHitTable
    {
        static void Postfix(AttackDirection from, ref Dictionary<ArmorLocation, int> __result)
        {
            if (ModState.ForceDamageTable == DamageTable.PUNCH)
            {
                Mod.Log.Info?.Write($"Attack against MECH will use the PUNCH damage table");
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
                Mod.Log.Info?.Write($"Attack against MECH will use the KICK damage table");
                __result = new Dictionary<ArmorLocation, int>();
                __result.Add(ArmorLocation.LeftLeg, 50);
                __result.Add(ArmorLocation.RightLeg, 50);
            }
            else
            {
                return;
            }
        }
    }

}

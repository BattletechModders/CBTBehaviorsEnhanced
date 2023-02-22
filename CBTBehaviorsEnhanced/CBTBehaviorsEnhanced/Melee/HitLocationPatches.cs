using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using CBTBehaviorsEnhanced.MeleeStates;
using CustAmmoCategories;
using CustomUnits;
using Harmony;
using System;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced.Patches.Melee
{
    [HarmonyPatch(typeof(CombatGameConstants))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch("OnDataLoaded")]
    [HarmonyAfter("io.mission.modrepuation")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
    public static class CombatGameConstants_OnDataLoaded {
      public static void Postfix(CombatGameConstants __instance, string id, string json) {
        try {
          CustomHitTableDef PUNCH_mech_table = new CustomHitTableDef();
          PUNCH_mech_table.HitTable = new Dictionary<AttackDirection, Dictionary<ArmorLocation, int>>() {
            { AttackDirection.FromLeft, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftTorso, 34 },
                { ArmorLocation.CenterTorso, 16 },
                { ArmorLocation.LeftArm, 34},
                { ArmorLocation.Head, 16 }
              }
            },
            { AttackDirection.FromBack, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftArm, 17 },
                { ArmorLocation.LeftTorsoRear, 17 },
                { ArmorLocation.CenterTorsoRear, 16},
                { ArmorLocation.RightTorsoRear, 17},
                { ArmorLocation.RightArm, 17},
                { ArmorLocation.Head, 16}
              }
            },
            { AttackDirection.FromRight, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.RightTorso, 34 },
                { ArmorLocation.CenterTorso, 16 },
                { ArmorLocation.RightArm, 34},
                { ArmorLocation.Head, 16 }
              }
            },
            { AttackDirection.FromFront, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftArm, 17 },
                { ArmorLocation.LeftTorso, 17 },
                { ArmorLocation.CenterTorso, 16},
                { ArmorLocation.RightTorso, 17},
                { ArmorLocation.RightArm, 17},
                { ArmorLocation.Head, 16}
              }
            },
            { AttackDirection.FromTop, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftArm, 17 },
                { ArmorLocation.LeftTorso, 17 },
                { ArmorLocation.CenterTorso, 16},
                { ArmorLocation.RightTorso, 17},
                { ArmorLocation.RightArm, 17},
                { ArmorLocation.Head, 16}
              }
            },
            { AttackDirection.FromArtillery, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftArm, 17 },
                { ArmorLocation.LeftTorso, 17 },
                { ArmorLocation.CenterTorso, 16},
                { ArmorLocation.RightTorso, 17},
                { ArmorLocation.RightArm, 17},
                { ArmorLocation.Head, 16}
              }
            },
            { AttackDirection.ToProne, new Dictionary<ArmorLocation, int>() {
                { ArmorLocation.LeftArm, 17 },
                { ArmorLocation.LeftTorso, 17 },
                { ArmorLocation.CenterTorso, 16},
                { ArmorLocation.RightTorso, 17},
                { ArmorLocation.RightArm, 17},
                { ArmorLocation.Head, 16}
              }
            }
          };
          PUNCH_mech_table.ParentStructureId = "mech";
          PUNCH_mech_table.Id = $"CBTBE_MELEE_{DamageTable.PUNCH.ToString()}";
          CustomHitTableDef.Register(PUNCH_mech_table);
          CustomHitTableDef KICK_mech_table = new CustomHitTableDef();
          KICK_mech_table.HitTable = new Dictionary<AttackDirection, Dictionary<ArmorLocation, int>>() {
              { AttackDirection.FromLeft, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 60 },
                  { ArmorLocation.RightLeg, 40 }
                }
              },
              { AttackDirection.FromBack, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 50 },
                  { ArmorLocation.RightLeg, 50 }
                }
              },
              { AttackDirection.FromRight, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 40 },
                  { ArmorLocation.RightLeg, 60 }
                }
              },
              { AttackDirection.FromFront, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 50 },
                  { ArmorLocation.RightLeg, 50 }
                }
              },
              { AttackDirection.FromTop, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 50 },
                  { ArmorLocation.RightLeg, 50 }
                }
              },
              { AttackDirection.FromArtillery, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 50 },
                  { ArmorLocation.RightLeg, 50 }
                }
              },
              { AttackDirection.ToProne, new Dictionary<ArmorLocation, int>() {
                  { ArmorLocation.LeftLeg, 50 },
                  { ArmorLocation.RightLeg, 50 }
                }
              }
            };
          KICK_mech_table.ParentStructureId = "mech";
          KICK_mech_table.Id = $"CBTBE_MELEE_{DamageTable.KICK.ToString()}";
          CustomHitTableDef.Register(KICK_mech_table);
        CustomHitTableDef PUNCH_vehcile_table = new CustomHitTableDef();
        PUNCH_vehcile_table.HitTable = new Dictionary<AttackDirection, Dictionary<ArmorLocation, int>>() {
            { AttackDirection.FromLeft, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8},
                { VehicleChassisLocations.Rear.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromBack, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromRight, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8},
                { VehicleChassisLocations.Rear.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromFront, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromTop, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 8 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromArtillery, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
              }
            },
            { AttackDirection.ToProne, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
              }
            }
          };
        PUNCH_vehcile_table.ParentStructureId = "vehicle";
        PUNCH_vehcile_table.Id = $"CBTBE_MELEE_{DamageTable.PUNCH.ToString()}";
        CustomHitTableDef.Register(PUNCH_vehcile_table);
        CustomHitTableDef KICK_vehicle_table = new CustomHitTableDef();
        KICK_vehicle_table.HitTable = new Dictionary<AttackDirection, Dictionary<ArmorLocation, int>>() {
            { AttackDirection.FromLeft, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 4 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8},
                { VehicleChassisLocations.Rear.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromBack, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 4 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromRight, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 4 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8},
                { VehicleChassisLocations.Rear.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromFront, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 4 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromTop, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 8 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 8 },
                { VehicleChassisLocations.Left.toFakeArmor(), 8},
                { VehicleChassisLocations.Right.toFakeArmor(), 8},
              }
            },
            { AttackDirection.FromArtillery, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
              }
            },
            { AttackDirection.ToProne, new Dictionary<ArmorLocation, int>() {
                { VehicleChassisLocations.Turret.toFakeArmor(), 40 },
                { VehicleChassisLocations.Front.toFakeArmor(), 40 },
                { VehicleChassisLocations.Rear.toFakeArmor(), 40 },
                { VehicleChassisLocations.Left.toFakeArmor(), 40 },
                { VehicleChassisLocations.Right.toFakeArmor(), 40 },
              }
            }
          };
        KICK_vehicle_table.ParentStructureId = "mech";
        KICK_vehicle_table.Id = $"CBTBE_MELEE_{DamageTable.KICK.ToString()}";
        CustomHitTableDef.Register(KICK_vehicle_table);
      } catch (Exception e) {
          Mod.Log.Error?.Write(e.ToString());
        }
      }
    }

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

using BattleTech;
using CustAmmoCategories;
using CustomUnits;
using HarmonyLib;
using HBS.Collections;
using IRBTModUtils.Extension;
using System;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class UnitHelper
    {

        public static bool IsQuadMech(this ICombatant combatant)
        {
            if (combatant is Mech mech)
            {
                UnitCustomInfo customInfo = mech.GetCustomInfo();
                return customInfo != null && customInfo.ArmsCountedAsLegs;
            }
            return false;
        }

        public static bool IsQuadMech(this MechDef mechDef)
        {
            if (mechDef == null) return false;

            UnitCustomInfo customInfo = mechDef.GetCustomInfo();
            if (customInfo == null) return false;

            return customInfo.ArmsCountedAsLegs;
        }

        public static bool IsTrooper(this ICombatant combatant)
        {
            if (combatant is Mech mech)
            {
                UnitCustomInfo customInfo = mech.GetCustomInfo();
                return customInfo != null && customInfo.SquadInfo != null && customInfo.SquadInfo.Troopers > 1;
            }
            return false;

        }
        public static bool IsTrooper(this MechDef mechDef)
        {
            if (mechDef == null) return false;

            UnitCustomInfo customInfo = mechDef.GetCustomInfo();
            if (customInfo == null) return false;

            return customInfo?.SquadInfo?.Troopers > 1;
        }

        public static bool IsNaval(this ICombatant combatant)
        {
            if (combatant is Mech mech)
            {
                UnitCustomInfo customInfo = mech.GetCustomInfo();
                return customInfo != null && customInfo.Naval;
            }
            return false;

        }

        public static bool IsNaval(this MechDef mechDef)
        {
            if (mechDef == null) return false;

            UnitCustomInfo customInfo = mechDef.GetCustomInfo();
            if (customInfo == null) return false;

            return customInfo.Naval;
        }

        public static bool IsVehicle(this ICombatant combatant)
        {
            if (combatant is Mech mech)
            {
                UnitCustomInfo customInfo = mech.GetCustomInfo();
                return customInfo != null && customInfo.FakeVehicle;
            }
            else if (combatant is Vehicle)
            {
                return true;
            }
            return false;

        }

        public static bool IsVehicle(this MechDef mechDef)
        {
            if (mechDef == null) return false;

            UnitCustomInfo customInfo = mechDef.GetCustomInfo();
            if (customInfo == null) return false;

            return customInfo.FakeVehicle;
        }

        //public static float GetUnitTonnage(this AbstractActor actor)
        //{

        //    Mod.Log.Debug?.Write($"Calculating unit tonnage for actor: {actor.DistinctId()}");
        //    float tonnage;
        //    if (actor is Turret)
        //    {
        //        TagSet actorTags = actor.GetTags();
        //        if (actorTags != null && actorTags.Contains("unit_light"))
        //        {
        //            tonnage = Mod.Config.Turret.LightTonnage;
        //            Mod.Log.Debug?.Write($" -- unit is a unit_light turret, using tonnage: {tonnage}");
        //        }
        //        else if (actorTags != null && actorTags.Contains("unit_medium"))
        //        {
        //            tonnage = Mod.Config.Turret.MediumTonnage;
        //            Mod.Log.Debug?.Write($" -- unit is unit_medium turret, using tonnage: {tonnage}");
        //        }
        //        else if (actorTags != null && actorTags.Contains("unit_heavy"))
        //        {
        //            tonnage = Mod.Config.Turret.HeavyTonnage;
        //            Mod.Log.Debug?.Write($" -- unit is a unit_heavy turret, using tonnage: {tonnage}");
        //        }
        //        else
        //        {
        //            tonnage = Mod.Config.Turret.DefaultTonnage;
        //            Mod.Log.Debug?.Write($" -- unit is tagless turret, using tonnage: {tonnage}");
        //        }
        //    }
        //    else if (actor is Vehicle vehicle)
        //    {
        //        tonnage = vehicle.tonnage;
        //        Mod.Log.Debug?.Write($" -- unit is a vehicle, using tonnage: {tonnage}");
        //    }
        //    else if (actor is Mech mech)
        //    {
        //        if (mech.FakeVehicle())
        //        {
        //            tonnage = mech.tonnage;
        //            Mod.Log.Debug?.Write($" -- unit is a fake vehicle, using tonnage: {tonnage}");
        //        }
        //        else if (mech.NavalUnit())
        //        {
        //            tonnage = mech.tonnage;
        //            Mod.Log.Debug?.Write($" -- unit is a naval unit, using tonnage: {tonnage}");
        //        }
        //        else if (mech.TrooperSquad())
        //        {
        //            TrooperSquad squad = mech as TrooperSquad;
        //            tonnage = (float)Math.Ceiling(squad.tonnage / squad.info.SquadInfo.Troopers);
        //            Mod.Log.Debug?.Write($" -- unit is a trooper squad, using tonnage: {tonnage}");
        //        }
        //        else
        //        {
        //            tonnage = mech.tonnage;
        //            Mod.Log.Debug?.Write($" -- unit is a mech, using tonnage: {tonnage}");
        //        }

        //    }
        //    else
        //    {
        //        UnitCfg unitConfig = actor.GetUnitConfig();
        //        tonnage = unitConfig.DefaultTonnage;
        //        Mod.Log.Debug?.Write($" -- unit tonnage is unknown, using tonnage: {tonnage}");
        //    }

        //    return tonnage;
        //}

        //public static float GetUnitTonnage(this MechDef mechDef)
        //{

        //    if (mechDef == null || mechDef.Chassis == null) return 0;

        //    Mod.Log.Debug?.Write($"Calculating unit tonnage for mechDef: {mechDef.Name}");
        //    float tonnage;

        //    UnitCustomInfo customInfo = mechDef.GetCustomInfo();
        //    float chassisTonnage = mechDef.Chassis.Tonnage;
        //    if (customInfo == null) return chassisTonnage;

        //    if (customInfo.SquadInfo != null && customInfo.SquadInfo.Troopers > 1)
        //    {
        //        tonnage = (float)Math.Ceiling(chassisTonnage / customInfo.SquadInfo.Troopers);
        //    }
        //    else
        //    {
        //        tonnage = chassisTonnage;
        //    }

        //    return tonnage;
        //}

        // TODO: EVERYTHING SHOULD CONVERT TO CACHED CALL IF POSSIBLE
        public static BehaviorVariableValue GetBehaviorVariableValue(BehaviorTree bTree, BehaviorVariableName name)
        {

            BehaviorVariableValue bhVarVal = null;
            if (ModState.RolePlayerBehaviorVarManager != null && ModState.RolePlayerGetBehaviorVar != null)
            {
                // Ask RolePlayer for the variable
                //getBehaviourVariable(AbstractActor actor, BehaviorVariableName name)
                Mod.Log.Trace?.Write($"Pulling BehaviorVariableValue from RolePlayer for unit: {bTree.unit.DistinctId()}.");
                bhVarVal = (BehaviorVariableValue)ModState.RolePlayerGetBehaviorVar.Invoke(ModState.RolePlayerBehaviorVarManager, new object[] { bTree.unit, name });
            }

            if (bhVarVal == null)
            {
                // RolePlayer does not return the vanilla value if there's no configuration for the actor. We need to check that we're null here to trap that edge case.
                // Also, if RolePlayer isn't configured we need to read the value. 
                Mod.Log.Trace?.Write($"Pulling BehaviorVariableValue from Vanilla for unit: {bTree.unit.DistinctId()}.");
                bhVarVal = GetBehaviorVariableValueDirectly(bTree, name);
            }

            Mod.Log.Trace?.Write($"  Value is: {bhVarVal}");
            return bhVarVal;

        }

        private static BehaviorVariableValue GetBehaviorVariableValueDirectly(BehaviorTree bTree, BehaviorVariableName name)
        {
            BehaviorVariableValue behaviorVariableValue = bTree.unitBehaviorVariables.GetVariable(name);
            if (behaviorVariableValue != null)
            {
                return behaviorVariableValue;
            }

            Pilot pilot = bTree.unit.GetPilot();
            if (pilot != null)
            {
                BehaviorVariableScope scopeForAIPersonality = bTree.unit.Combat.BattleTechGame.BehaviorVariableScopeManager.GetScopeForAIPersonality(pilot.pilotDef.AIPersonality);
                if (scopeForAIPersonality != null)
                {
                    behaviorVariableValue = scopeForAIPersonality.GetVariableWithMood(name, bTree.unit.BehaviorTree.mood);
                    if (behaviorVariableValue != null)
                    {
                        return behaviorVariableValue;
                    }
                }
            }

            if (bTree.unit.lance != null)
            {
                behaviorVariableValue = bTree.unit.lance.BehaviorVariables.GetVariable(name);
                if (behaviorVariableValue != null)
                {
                    return behaviorVariableValue;
                }
            }

            if (bTree.unit.team != null)
            {
                Traverse bvT = Traverse.Create(bTree.unit.team).Field("BehaviorVariables");
                BehaviorVariableScope bvs = bvT.GetValue<BehaviorVariableScope>();
                behaviorVariableValue = bvs.GetVariable(name);
                if (behaviorVariableValue != null)
                {
                    return behaviorVariableValue;
                }
            }

            UnitRole unitRole = bTree.unit.DynamicUnitRole;
            if (unitRole == UnitRole.Undefined)
            {
                unitRole = bTree.unit.StaticUnitRole;
            }

            BehaviorVariableScope scopeForRole = bTree.unit.Combat.BattleTechGame.BehaviorVariableScopeManager.GetScopeForRole(unitRole);
            if (scopeForRole != null)
            {
                behaviorVariableValue = scopeForRole.GetVariableWithMood(name, bTree.unit.BehaviorTree.mood);
                if (behaviorVariableValue != null)
                {
                    return behaviorVariableValue;
                }
            }

            if (bTree.unit.CanMoveAfterShooting)
            {
                BehaviorVariableScope scopeForAISkill = bTree.unit.Combat.BattleTechGame.BehaviorVariableScopeManager.GetScopeForAISkill(AISkillID.Reckless);
                if (scopeForAISkill != null)
                {
                    behaviorVariableValue = scopeForAISkill.GetVariableWithMood(name, bTree.unit.BehaviorTree.mood);
                    if (behaviorVariableValue != null)
                    {
                        return behaviorVariableValue;
                    }
                }
            }

            behaviorVariableValue = bTree.unit.Combat.BattleTechGame.BehaviorVariableScopeManager.GetGlobalScope().GetVariableWithMood(name, bTree.unit.BehaviorTree.mood);
            if (behaviorVariableValue != null)
            {
                return behaviorVariableValue;
            }

            return DefaultBehaviorVariableValue.GetSingleton();
        }
    }
}

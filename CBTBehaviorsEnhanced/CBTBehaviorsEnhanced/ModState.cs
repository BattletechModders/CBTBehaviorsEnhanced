using BattleTech.UI;
using CBTBehaviorsEnhanced.MeleeStates;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CBTBehaviorsEnhanced
{
    public static class ModState
    {

        public const int NO_ATTACK_SEQUENCE_ID = -1; // Attack sequences are always 0 or positive integers (see AttackDirector:44) Use -1 to signify 'no sequence'

        // Melee States
        private static Dictionary<string, Dictionary<Vector3, MeleeState>> meleeStates = new Dictionary<string, Dictionary<Vector3, MeleeState>>();
        private static Dictionary<string, MeleeAttack> selectedAttack = new Dictionary<string, MeleeAttack>();
        private static Dictionary<int, (MeleeAttack, Weapon)> sequenceMeleeState = new Dictionary<int, (MeleeAttack, Weapon)>();
        private static Dictionary<string, (Weapon, Weapon)> imaginaryWeapons = new Dictionary<string, (Weapon, Weapon)>();

        public static Vector3 MeleePreviewPos = Vector3.one;
        public static DamageTable ForceDamageTable { get; set; } = DamageTable.NONE;

        public static float CachedDFASelfDamage = 0f;
        public static float OriginalDFASelfDamage = 0f;

        // Breach state
        public static float BreachCheck = 0f;
        public static int BreachAttackId = NO_ATTACK_SEQUENCE_ID;
        public static Dictionary<ChassisLocations, int> BreachHitsMech = new Dictionary<ChassisLocations, int>();
        public static Dictionary<BuildingLocation, int> BreachHitsTurret = new Dictionary<BuildingLocation, int>();
        public static Dictionary<VehicleChassisLocations, int> BreachHitsVehicle = new Dictionary<VehicleChassisLocations, int>();

        // UI Elements
        public static GameObject MeleeAttackContainer = null;
        public static CombatHUDFireButton ChargeFB = null;
        public static CombatHUDFireButton KickFB = null;
        public static CombatHUDFireButton PhysicalWeaponFB = null;
        public static CombatHUDFireButton PunchFB = null;

        // Per Unit or Position State
        public static Dictionary<string, ActorMeleeCondition> meleeConditionCache = new Dictionary<string, ActorMeleeCondition>();

        // Roleplayer Integration
        public static object RolePlayerBehaviorVarManager;
        public static MethodInfo RolePlayerGetBehaviorVar;
        public static bool MEIsLoaded;

        public static void Reset()
        {

            Mod.Log.Info?.Write($"RESETTING MOD STATE!");

            // Melee weapon state
            meleeStates.Clear();
            selectedAttack.Clear();
            sequenceMeleeState.Clear();
            imaginaryWeapons.Clear();
            MeleePreviewPos = Vector3.one;
            CachedDFASelfDamage = 0f;
            ForceDamageTable = DamageTable.NONE;
            OriginalDFASelfDamage = 0f;

            // Breach state
            BreachCheck = 0f;
            BreachAttackId = NO_ATTACK_SEQUENCE_ID;
            BreachHitsMech.Clear();
            BreachHitsTurret.Clear();
            BreachHitsVehicle.Clear();

            // UI Elements
            MeleeAttackContainer = null;
            ChargeFB = null;
            KickFB = null;
            PhysicalWeaponFB = null;
            PunchFB = null;

            // State caches
            meleeConditionCache.Clear();
        }

        // == SEQUENCE CACHE FUNCTIONS ==
        public static void AddOrUpdateMeleeSequenceState(int sequenceID, MeleeAttack attack, Weapon weapon)
        {
            sequenceMeleeState[sequenceID] = (attack, weapon);
        }

        public static (MeleeAttack, Weapon) GetMeleeSequenceState(int sequenceId)
        {
            (MeleeAttack attack, Weapon weapon) sequenceState;
            sequenceMeleeState.TryGetValue(sequenceId, out sequenceState);
            return sequenceState;
        }


        // == IMAGINARY WEAPON FUNCTIONS ==
        public static (Weapon, Weapon) GetFakedWeapons(Mech attacker)
        {
            (Weapon melee, Weapon dfa) weapons;
            imaginaryWeapons.TryGetValue(attacker.DistinctId(), out weapons);
            if (weapons.melee == null && weapons.dfa == null)
            {
                Mod.MeleeLog.Info?.Write($"Creating faked weapons for actor: {attacker.DistinctId()}");
                if (attacker.MeleeWeapon != null)
                {
                    weapons.melee = new Weapon(attacker, SharedState.Combat, attacker.MeleeWeapon.mechComponentRef, "CBTBE_FAKE_MELEE");
                    // Initialize a game representation to prevent warnings in CAC logs
                    weapons.melee.Init();
                    weapons.melee.InitStats();
                    weapons.melee.InitGameRep(attacker.MeleeWeapon.baseComponentRef.prefabName,
                        attacker.GetAttachTransform(attacker.MeleeWeapon.mechComponentRef.MountedLocation),
                        attacker.LogDisplayName);
                    Mod.MeleeLog.Info?.Write($"  -- created melee weapon");
                }

                if (attacker.DFAWeapon != null)
                {
                    weapons.dfa = new Weapon(attacker, SharedState.Combat, attacker.DFAWeapon.mechComponentRef, "CBTBE_FAKE_DFA");
                    weapons.dfa.Init();
                    weapons.dfa.InitStats();
                    weapons.dfa.InitGameRep(attacker.DFAWeapon.baseComponentRef.prefabName,
                        attacker.GetAttachTransform(attacker.DFAWeapon.mechComponentRef.MountedLocation),
                        attacker.LogDisplayName);
                    Mod.MeleeLog.Info?.Write($"  -- created DFA weapon");
                }
                imaginaryWeapons[attacker.DistinctId()] = weapons;
            }

            return weapons;
        }

        // == SELECTED ATTACK CACHE FUNCTIONS ==
        public static MeleeAttack GetSelectedAttack(AbstractActor actor)
        {
            if (actor == null) return null;

            MeleeAttack selected;
            selectedAttack.TryGetValue(actor?.DistinctId(), out selected);
            return selected;
        }

        public static void AddOrUpdateSelectedAttack(AbstractActor actor, MeleeAttack attack)
        {
            selectedAttack[actor.DistinctId()] = attack;
        }

        // == MELEE STATE CACHE FUNCTIONS ==
        public static MeleeState GetMeleeState(AbstractActor actor, Vector3 position)
        {
            Dictionary<Vector3, MeleeState> positionDict;
            meleeStates.TryGetValue(actor?.DistinctId(), out positionDict);
            if (positionDict == null)
                return null;

            MeleeState cachedState;
            positionDict.TryGetValue(position, out cachedState);
            return cachedState;
        }

        public static MeleeState AddorUpdateMeleeState(AbstractActor attacker, Vector3 attackPos, ICombatant target, bool skipValidatePathing = false)
        {

            Mech attackerMech = attacker as Mech;
            if (attackerMech == null) return null;

            AbstractActor targetActor = target as AbstractActor;
            if (targetActor == null) return null;

            MeleeState state = new MeleeState(attackerMech, attackPos, targetActor, skipValidatePathing);
            Dictionary<Vector3, MeleeState> positionDict;
            meleeStates.TryGetValue(attacker?.DistinctId(), out positionDict);
            if (positionDict == null)
            {
                // Add workflow
                positionDict = new Dictionary<Vector3, MeleeState>();
                meleeStates[attacker.DistinctId()] = positionDict;
                Mod.MeleeLog.Info?.Write($"Created meleeState for attacker: {attacker.DistinctId()} at pos: {attackPos}");
            }
            positionDict[attackPos] = state;

            return state;
        }

        private static void InvalidateMeleeStates(AbstractActor actor)
        {
            if (actor == null) return;

            // States dictionary
            Dictionary<Vector3, MeleeState> positionDict;
            meleeStates.TryGetValue(actor?.DistinctId(), out positionDict);
            if (positionDict != null)
                positionDict.Clear();

            // Selected attack dict
            selectedAttack[actor.DistinctId()] = null;

            Mod.MeleeLog.Info?.Write($"Invalidated meleeStates for actor: {actor.DistinctId()}");
        }

        // == MELEE CONDITION CACHE FUNCTION ==
        public static ActorMeleeCondition GetMeleeCondition(AbstractActor actor)
        {
            ActorMeleeCondition condition;
            meleeConditionCache.TryGetValue(actor.DistinctId(), out condition);
            if (condition == null)
            {
                condition = new ActorMeleeCondition(actor);
                meleeConditionCache.Add(actor.DistinctId(), condition);
            }
            return condition;
        }

        // Invalidate all cache entries for the specified actor
        public static void InvalidateState(AbstractActor actor)
        {
            if (actor == null) return;

            if (actor is Mech mech)
            {

                if (meleeConditionCache.ContainsKey(mech.DistinctId()))
                    meleeConditionCache.Remove(mech.DistinctId());

                InvalidateMeleeStates(actor);
            }
        }
    }

}



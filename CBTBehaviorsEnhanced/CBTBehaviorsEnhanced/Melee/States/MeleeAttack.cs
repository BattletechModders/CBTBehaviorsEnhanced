using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using IRBTModUtils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public abstract class MeleeAttack
    {
        public bool IsValid = false;
        protected readonly MeleeState state;

        public DamageTable AttackerTable { get; set; }
        public DamageTable TargetTable { get; set; }

        public float[] AttackerDamageClusters = new float[] { };
        public float[] TargetDamageClusters = new float[] { };

        public float AttackerInstability;
        public float TargetInstability;

        public bool UnsteadyAttackerOnHit = false;
        public bool UnsteadyAttackerOnMiss = false;

        // Target modifiers
        public bool OnTargetMechHitForceUnsteady = false;
        public int OnTargetVehicleHitEvasionPipsRemoved = 0;

        public bool UsePilotingDelta = true;

        // The modifiers that display in the tooltip hover. Aggregated to form the final attack modifier
        public Dictionary<string, int> AttackModifiers = new Dictionary<string, int>();

        // Any notes that should be displayed in the description section of the UI
        public HashSet<string> DescriptionNotes = new HashSet<string>();

        // The display label to use
        public string Label = "UNKNOWN";

        // What animation to use
        public MeleeAttackType AttackAnimation = MeleeAttackType.NotSet;

        public MeleeAttack(MeleeState state) 
        {
            this.state = state;
        }

        public abstract bool IsRangedWeaponAllowed(Weapon weapon);
    }

    public enum DamageTable
    {
        NONE, 
        STANDARD, 
        REAR,
        PUNCH, 
        KICK
    }
}

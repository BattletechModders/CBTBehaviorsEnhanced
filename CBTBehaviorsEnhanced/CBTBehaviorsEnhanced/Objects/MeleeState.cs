using BattleTech;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced
{
    public abstract class MeleeState
    {
        public bool IsValid = false;

        public DamageTable AttackerTable { get; set; }
        public DamageTable TargetTable { get; set; }

        protected MechMeleeCondition AttackerCondition { get; private set; }

        public float[] AttackerDamageClusters;
        public float[] TargetDamageClusters;

        public float AttackerInstability;
        public float TargetInstability;

        public bool ForceUnsteadyOnAttacker;
        public bool ForceUnsteadyOnTarget;

        // The modifiers that display in the tooltip hover. Aggregated to form the final attack modifier
        public Dictionary<int, string> AttackModifiers = new Dictionary<int, string>();

        // Any notes that should be displayed in the description section of the UI
        public HashSet<string> DescriptionNotes = new HashSet<string>();

        public MeleeState(Mech attacker)
        {
            AttackerCondition = new MechMeleeCondition(attacker);
        }

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

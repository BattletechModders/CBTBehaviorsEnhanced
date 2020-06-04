using BattleTech;
using CBTBehaviorsEnhanced.Objects;
using System.Collections.Generic;
using System.Linq;

namespace CBTBehaviorsEnhanced
{
    public class MeleeStates
    {
        public ChargeMeleeState Charge;
        public DFAMeleeState DFA;
        public KickMeleeState Kick;
        public PhysicalWeaponMeleeState PhysicalWeapon;
        public PunchMeleeState Punch;

        public MeleeState SelectedState;

        public MeleeState HighestDamageState
        {
            get
            {
                MeleeState selectedState = null;
                float selectedDamage = 0;

                List<MeleeState> allStates = new List<MeleeState> { Charge, DFA, Kick, PhysicalWeapon, Punch };
                foreach (MeleeState state in allStates)
                {
                    if (state != null)
                    {
                        float typeDamage = state.TargetDamageClusters.Sum();
                        if (typeDamage > selectedDamage)
                        {
                            selectedDamage = typeDamage;
                            selectedState = state;
                        }
                    }
                }

                return selectedState;
            }
        }
    }

    public abstract class MeleeState
    {
        public bool IsValid = false;

        public DamageTable AttackerTable { get; set; }
        public DamageTable TargetTable { get; set; }

        protected MechMeleeCondition AttackerCondition { get; private set; }

        public float[] AttackerDamageClusters = new float[] { };
        public float[] TargetDamageClusters = new float[] { };

        public float AttackerInstability;
        public float TargetInstability;

        public bool ForceUnsteadyOnAttacker;
        public bool ForceUnsteadyOnTarget;

        // The modifiers that display in the tooltip hover. Aggregated to form the final attack modifier
        public Dictionary<string, int> AttackModifiers = new Dictionary<string, int>();

        // Any notes that should be displayed in the description section of the UI
        public HashSet<string> DescriptionNotes = new HashSet<string>();

        // The display label to use
        public string Label = "UNKNOWN";

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

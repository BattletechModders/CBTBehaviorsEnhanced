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

        public MeleeState GetHighestTargetDamageState(bool includeDFA = false)
        {
            MeleeState selectedState = null;
            float selectedDamage = 0;

            // Do not include charge by default; only include it when all other attacks are null
            List<MeleeState> allStates = new List<MeleeState> { Kick, PhysicalWeapon, Punch };
            
            // Do not use DFA here, because it's selected differently in UI.
            if (includeDFA && DFA != null) allStates.Add(DFA);

            // Check kick, phyweap, punch first
            foreach (MeleeState state in allStates)
            {
                if (state != null)
                {
                    // TODO: Include attack modifiers for EV style calculaion
                    float typeDamage = state.TargetDamageClusters.Sum();
                    if (typeDamage > selectedDamage)
                    {
                        selectedDamage = typeDamage;
                        selectedState = state;
                    }
                }
            }

            // If everything remains zero, check charge
            if (selectedDamage == 0 && Charge.IsValid)
            {
                selectedDamage = Charge.TargetDamageClusters.Sum();
                selectedState = Charge;
            }

            return selectedState;
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

        public bool UnsteadyAttackerOnHit = false;
        public bool UnsteadyAttackerOnMiss = false;
        public bool UnsteadyTargetOnHit = false;

        public bool UsePilotingDelta = true;

        // The modifiers that display in the tooltip hover. Aggregated to form the final attack modifier
        public Dictionary<string, int> AttackModifiers = new Dictionary<string, int>();

        // Any notes that should be displayed in the description section of the UI
        public HashSet<string> DescriptionNotes = new HashSet<string>();

        // The display label to use
        public string Label = "UNKNOWN";

        // What animation to use
        public MeleeAttackType AttackAnimation = MeleeAttackType.NotSet;

        public MeleeState(Mech attacker)
        {
            AttackerCondition = new MechMeleeCondition(attacker);
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

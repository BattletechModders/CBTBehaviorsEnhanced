using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CustAmmoCategories;
using CustomComponents;
using IRBTModUtils.Extension;
using Localize;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public class PunchAttack : MeleeAttack
    {
        // Per BT Manual pg.38,
        //   * target takes 1 pt. each 10 tons of attacker, rounded up
        //   *   x0.5 damage for each missing upper & lower actuator
        //   * Resolves on punch table
        //   * Requires a shoulder actuator
        //   *   +1 to hit if hand actuator missing
        //   *   +2 to hit if lower arm actuator missing
        //   *   -2 modifier if target is prone

        public PunchAttack(MeleeState state) : base(state)
        {
            Mod.MeleeLog.Info?.Write($"Building PUNCH state for attacker: {state.attacker.DistinctId()} @ attackPos: {state.attackPos} vs. target: {state.target.DistinctId()}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Punch];
            this.IsValid = ValidateAttack(state.attacker, state.target, state.validAnimations, state.skipValidatePathing);
            if (IsValid)
            {

                CalculateDamages(state.attacker, state.target);
                CalculateInstability(state.attacker, state.target);
                CalculateModifiers(state.attacker, state.target);
                CreateDescriptions(state.attacker, state.target);

                // Damage tables 
                this.AttackerTable = DamageTable.NONE;
                this.TargetTable = DamageTable.PUNCH;

                // Unsteady
                this.UnsteadyAttackerOnHit = Mod.Config.Melee.Punch.UnsteadyAttackerOnHit;
                this.UnsteadyAttackerOnMiss = Mod.Config.Melee.Punch.UnsteadyAttackerOnMiss;

                this.OnTargetMechHitForceUnsteady = Mod.Config.Melee.Punch.UnsteadyTargetOnHit;
                this.OnTargetVehicleHitEvasionPipsRemoved = Mod.Config.Melee.Punch.TargetVehicleEvasionPipsRemoved;

                // Set the animation type
                this.AttackAnimation = state.validAnimations.Contains(MeleeAttackType.Punch) ? MeleeAttackType.Punch : MeleeAttackType.Tackle;
            }
        }
        public override bool IsRangedWeaponAllowed(Weapon weapon)
        {
            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_NeverMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed as it can never be used in melee");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_HandHeld_NoArmMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed for punch as it is a handheld that requires hands");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_AlwaysMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} marked with AlwaysMelee category, force-enabling");
                return true;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_HandHeld_AlwaysArmMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} enabled for physical weapon, hand-held that should always be used");
                return true;
            }

            if (weapon.Location == (int)ChassisLocations.LeftArm || weapon.Location == (int)ChassisLocations.RightArm)
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed for punch because it is in the arms.");
                return false;
            }

            return true;
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations, bool skipValidatePathing)
        {
            ActorMeleeCondition meleeCondition = ModState.GetMeleeCondition(attacker);
            if (!meleeCondition.CanPunch())
            {
                Mod.MeleeLog.Info?.Write($"Attacker cannot punch, skipping.");
                return false;
            }

            // If we cannot punch - not a valid attack
            if (!validAnimations.Contains(MeleeAttackType.Punch) && !(validAnimations.Contains(MeleeAttackType.Tackle)))
            {
                Mod.MeleeLog.Info?.Write("Animations do not include a punch or tackle, attacker cannot punch!");
                return false;
            }

            if (target.UnaffectedPathing())
            {
                Mod.MeleeLog.Info?.Write($"Target is unaffected by pathing, likely a VTOL or LAM in flight. Cannot melee it!");
                return false;
            }

            if (attacker.IsQuadMech())
            {
                Mod.MeleeLog.Info?.Write($"Attacker is a quad, cannot punch.");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch            
            if (!skipValidatePathing && !state.HasWalkAttackNodes)
            {
                Mod.MeleeLog.Info?.Write($"No walking nodes found for melee attack!");
                return false;
            }

            Mod.MeleeLog.Info?.Write("PUNCH ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            float[] adjTargetDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.TargetDamageClusters, target, attacker.MeleeWeapon);

            string targetDamageS = adjTargetDamage.Count() > 1 ?
                $"{adjTargetDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjTargetDamage)})" :
                adjTargetDamage[0].ToString();

            string targetTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Punch];

            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Punch_Desc],
                new object[] { targetDamageS, this.TargetInstability, targetTable })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {

            // If target is prone, -2 modifier
            if (target.IsProne) this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

            // Actuator damage; +1 for arm actuator, +2 to hit for each upper/lower actuator hit
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(attacker);
            int leftArmMalus = (2 - attackerCondition.LeftArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!attackerCondition.LeftHandIsFunctional) leftArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int rightArmMalus = (2 - attackerCondition.RightArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            if (!attackerCondition.RightHandIsFunctional) rightArmMalus += Mod.Config.Melee.Punch.ArmActuatorDamageMalus;

            int bestMalus = leftArmMalus <= rightArmMalus ? leftArmMalus : rightArmMalus;
            if (bestMalus != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Actuator_Damage, bestMalus);
            }

            // Check for attack modifier statistic
            if (attacker.StatCollection.ContainsStatistic(ModStats.PunchAttackMod) &&
                attacker.StatCollection.GetValue<int>(ModStats.PunchAttackMod) != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Punch_Attack_Mod, attacker.StatCollection.GetValue<int>(ModStats.PunchAttackMod));
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating PUNCH damage for attacker: {CombatantUtils.Label(attacker)} @ {attacker.tonnage} tons " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float damage = attacker.PunchDamage();

            // Adjust damage for any target resistance
            damage = target.ApplyPunchDamageReduction(damage);

            // Target damage applies as a single hit
            this.TargetDamageClusters = AttackHelper.CreateDamageClustersWithExtraAttacks(attacker, damage, ModStats.PunchExtraHitsCount);
            StringBuilder sb = new StringBuilder(" - Target damage clusters: ");
            foreach (float cluster in this.TargetDamageClusters)
            {
                sb.Append(cluster);
                sb.Append(", ");
            }
            Mod.MeleeLog.Info?.Write(sb.ToString());

        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating PUNCH instability for attacker: {CombatantUtils.Label(attacker)} @ {attacker.tonnage} tons " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float instab = attacker.PunchInstability();

            // Adjust damage for any target resistance
            instab = target.ApplyPunchInstabReduction(instab);

            this.TargetInstability = instab;

        }
    }
}

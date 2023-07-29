using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CustAmmoCategories;
using CustomComponents;
using Localize;
using System;
using System.Linq;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public class DFAAttack : MeleeAttack
    {
        // Per BT Manual pg.36,
        //   * target takes 1 pt. each 10 tons of attacker, which is then multiplied by 3 and rounded up
        //   * attacker takes tonnage / 5, rounded up
        //   * Damage clustered in 5 (25) point groupings for both attacker & defender
        //   * Target resolves on punch table
        //   *   Prone targets resolve on rear table
        //   * Attacker resolves on kick table
        //   * Comparative attack modifier; difference in attacker and defender is applied to attack
        //   *  +3 modifier to hit for jumping
        //   *  +2 to hit if upper or lower leg actuators are hit
        //   *  -2 modifier if target is prone
        //   * Attacker makes PSR with +4, target with +2 and fall

        public DFAAttack(MeleeState state) : base(state)
        {
            Mod.MeleeLog.Info?.Write($"Building DFA state for attacker: {CombatantUtils.Label(state.attacker)} @ attackPos: {state.attackPos} vs. target: {CombatantUtils.Label(state.target)}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_DeathFromAbove];
            this.IsValid = ValidateAttack(state.attacker, state.target, state.skipValidatePathing);
            if (IsValid)
            {
                this.UsePilotingDelta = Mod.Config.Melee.DFA.UsePilotingDelta;

                CalculateDamages(state.attacker, state.target);
                CalculateInstability(state.attacker, state.target);
                CalculateModifiers(state.attacker, state.target);
                CreateDescriptions(state.attacker, state.target);

                // Damage tables
                this.AttackerTable = DamageTable.KICK;
                this.TargetTable = state.target.IsProne ? DamageTable.REAR : DamageTable.PUNCH;

                // Unsteady
                this.UnsteadyAttackerOnHit = Mod.Config.Melee.DFA.UnsteadyAttackerOnHit;
                this.UnsteadyAttackerOnMiss = Mod.Config.Melee.DFA.UnsteadyAttackerOnMiss;

                this.OnTargetMechHitForceUnsteady = Mod.Config.Melee.DFA.UnsteadyTargetOnHit;
                this.OnTargetVehicleHitEvasionPipsRemoved = Mod.Config.Melee.DFA.TargetVehicleEvasionPipsRemoved;

                // Set the animation type
                this.AttackAnimation = MeleeAttackType.DFA;
            }
        }
        public override bool IsRangedWeaponAllowed(Weapon weapon)
        {
            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_NeverMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed as it can never be used in melee");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_AlwaysMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} marked with AlwaysMelee category, force-enabling");
                return true;
            }

            return false;
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, bool skipValidatePathing)
        {
            ActorMeleeCondition meleeCondition = ModState.GetMeleeCondition(attacker);
            if (!meleeCondition.CanDFA())
            {
                Mod.MeleeLog.Info?.Write($"Attacker cannot DFA, skipping.");
                return false;
            }

            if (!skipValidatePathing && !attacker.CanDFATargetFromPosition(target, attacker.CurrentPosition))
            {
                Mod.MeleeLog.Info?.Write($"Attacker unable to DFA target from their position.");
                return false;
            }

            if (target.UnaffectedPathing())
            {
                Mod.MeleeLog.Info?.Write($"Target is unaffected by pathing, likely a VTOL or LAM in flight. Cannot melee it!");
                return false;
            }

            // No damage check - by rules, you can DFA?
            Mod.MeleeLog.Info?.Write("DFA ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            float[] adjAttackerDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.AttackerDamageClusters, attacker, attacker.MeleeWeapon);
            float[] adjTargetDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.TargetDamageClusters, target, attacker.MeleeWeapon);

            string attackerDamageS = adjAttackerDamage.Count() > 1 ?
                $"{adjAttackerDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjAttackerDamage)})" :
                adjAttackerDamage[0].ToString();
            string targetDamageS = adjTargetDamage.Count() > 1 ?
                $"{adjTargetDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjTargetDamage)})" :
                adjTargetDamage[0].ToString();

            string targetTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Punch];
            string attackerTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Legs];

            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_DFA_Desc],
                new object[] {
                    targetDamageS, this.TargetInstability, targetTable,
                    attackerDamageS, this.AttackerInstability, attackerTable
                })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            // Always add the jump modifier
            this.AttackModifiers.Add(ModText.LT_Label_Attacker_Jumped, Mod.Config.ToHitSelfJumped);

            if (this.UsePilotingDelta)
            {
                // Build the comparative skill level
                int comparativeSkill = (attacker.SkillPiloting - target.SkillPiloting) * -1;
                Mod.MeleeLog.Info?.Write($"Comparative skill = {comparativeSkill} => attacker {CombatantUtils.Label(attacker)} @ piloting: {attacker.SkillPiloting} " +
                    $"vs. target: {CombatantUtils.Label(target)} @ piloting: {target.SkillPiloting} ");
                this.AttackModifiers.Add(ModText.LT_Label_ComparativeSkill_Piloting, comparativeSkill);
            }

            // Check for attack modifier statistic
            int dfaAttackMod = attacker.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackMod);
            if (dfaAttackMod != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_DFA_Attack_Mod, dfaAttackMod);
            }

            // Check for prone targets
            if (target.IsProne) this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating DFA damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float targetTonnage = 0;
            if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
            else if (target is Mech mech) targetTonnage = mech.tonnage;
            else if (target is Turret turret) targetTonnage = turret.MeleeTonnage();
            Mod.MeleeLog.Info?.Write($" - Tonnage => Attacker: {attacker.tonnage}  Target: {targetTonnage}");

            // split attacker damage into clusters
            float attackerDamage = attacker.DFAAttackerDamage(targetTonnage);
            attackerDamage = attacker.ApplyDFADamageReduction(attackerDamage);
            DamageHelper.ClusterDamage(attackerDamage, Mod.Config.Melee.DFA.DamageClusterDivisor, out this.AttackerDamageClusters);

            // split target damage into clusters
            float targetDamage = attacker.DFATargetDamage();
            targetDamage = target.ApplyDFADamageReduction(targetDamage);
            DamageHelper.ClusterDamage(targetDamage, Mod.Config.Melee.DFA.DamageClusterDivisor, out this.TargetDamageClusters);
        }

        private void CalculateInstability(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating DFA instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float targetTonnage = 0;
            if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
            else if (target is Mech mech) targetTonnage = mech.tonnage;
            Mod.MeleeLog.Info?.Write($" - Target tonnage is: {targetTonnage}");

            float attackerInstab = attacker.DFAAttackerInstability(targetTonnage);
            attackerInstab = attacker.ApplyDFAInstabReduction(attackerInstab);
            this.AttackerInstability = attackerInstab;

            float targetInstab = attacker.DFATargetInstability();
            targetInstab = target.ApplyDFAInstabReduction(targetInstab);
            this.TargetInstability = targetInstab;
        }
    }
}

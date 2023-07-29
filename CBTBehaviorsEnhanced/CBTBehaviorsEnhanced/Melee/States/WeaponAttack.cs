using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using CustAmmoCategories;
using CustomComponents;
using Localize;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.MeleeStates
{
    public class WeaponAttack : MeleeAttack
    {
        // Per BT Manual pg.38,
        //   * target takes 1 pt. each 4-10 tons of attacker, rounded up (varies by weapon)
        //   * One attack
        //   * Resolves on main table
        //   *   Optional - Can resolve on punch table 
        //   *   Optional - Can resolve on kick table 
        //   * Requires a shoulder actuator AND hand actuator
        //   *   +2 to hit if lower or upper arm actuator missing
        //   *   -2 modifier if target is prone

        public WeaponAttack(MeleeState state) : base(state)
        {
            Mod.MeleeLog.Info?.Write($"Building PHYSICAL WEAPON state for attacker: {CombatantUtils.Label(state.attacker)} @ attackPos: {state.attackPos} vs. target: {CombatantUtils.Label(state.target)}");

            this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_Physical_Weapon];
            this.IsValid = ValidateAttack(state.attacker, state.target, state.validAnimations, state.skipValidatePathing);
            if (IsValid)
            {

                CalculateDamages(state.attacker, state.target);
                CalculateInstability(state.attacker, state.target);
                CalculateModifiers(state.attacker, state.target);
                CreateDescriptions(state.attacker, state.target);

                // Damage tables 
                this.AttackerTable = DamageTable.NONE;
                this.TargetTable = DamageTable.STANDARD;
                if (state.attacker.StatCollection.ContainsStatistic(ModStats.PhysicalWeaponLocationTable))
                {
                    string tableName = state.attacker.StatCollection.GetValue<string>(ModStats.PhysicalWeaponLocationTable).ToUpper();
                    if (tableName.Equals("KICK")) this.TargetTable = DamageTable.KICK;
                    else if (tableName.Equals("PUNCH")) this.TargetTable = DamageTable.PUNCH;
                    else if (tableName.Equals("STANDARD")) this.TargetTable = DamageTable.STANDARD;
                }

                // Unsteady
                this.UnsteadyAttackerOnHit = state.attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnHit);
                this.UnsteadyAttackerOnMiss = state.attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyAttackerOnMiss);
                this.OnTargetMechHitForceUnsteady = state.attacker.StatCollection.GetValue<bool>(ModStats.PhysicalWeaponUnsteadyTargetOnHit);

                this.OnTargetVehicleHitEvasionPipsRemoved = Mod.Config.Melee.PhysicalWeapon.TargetVehicleEvasionPipsRemoved;

                // Set the animation type
                this.AttackAnimation = state.attacker.IsQuadMech() ? MeleeAttackType.Tackle : MeleeAttackType.Punch;
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

            if (base.state.attacker.IsTrooper())
            {

                Mod.MeleeLog.Debug?.Write($"Attacker is trooper, enabling weapons.");
                return true;
            }

            // Mech only considerations
            if (weapon.Location == (int)ChassisLocations.LeftArm || weapon.Location == (int)ChassisLocations.RightArm)
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed for physical weapon because it is in the arms.");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_HandHeld_NoArmMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} disallowed for physical weapon as it is a handheld that requires hands");
                return false;
            }

            if (weapon.componentDef.IsCategory(ModConsts.CC_Category_HandHeld_AlwaysArmMelee))
            {
                Mod.MeleeLog.Debug?.Write($"Weapon: {weapon.UIName} enabled for physical weapon, hand-held that should always be used");
                return true;
            }

            return true;
        }

        private bool ValidateAttack(Mech attacker, AbstractActor target, HashSet<MeleeAttackType> validAnimations, bool skipValidatePathing)
        {

            ActorMeleeCondition meleeCondition = ModState.GetMeleeCondition(attacker);
            if (!meleeCondition.CanUsePhysicalAttack())
            {
                Mod.MeleeLog.Info?.Write($"Attacker cannot make a physical attack, skipping.");
                return false;
            }

            // Check animations
            if (target.IsQuadMech() && !validAnimations.Contains(MeleeAttackType.Tackle))
            {
                Mod.MeleeLog.Info?.Write("Animations do not include a tackle, cannot use non-biped physical weapon.");
                return false;

            }
            else if (!validAnimations.Contains(MeleeAttackType.Punch))
            {
                Mod.MeleeLog.Info?.Write("Animations do not include a punch, cannot use biped physical weapon.");
                return false;
            }


            if (target.UnaffectedPathing())
            {
                Mod.MeleeLog.Info?.Write($"Target is unaffected by pathing, likely a VTOL or LAM in flight. Cannot melee it!");
                return false;
            }

            // If distance > walkSpeed, disable kick/physical weapon/punch            
            if (!skipValidatePathing && !state.HasWalkAttackNodes)
            {
                Mod.MeleeLog.Info?.Write($"No walking nodes found for melee attack!");
                return false;
            }

            Mod.MeleeLog.Info?.Write("PHYSICAL WEAPON ATTACK validated");
            return true;
        }

        private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
            float[] adjTargetDamage = DamageHelper.AdjustDamageByTargetTypeForUI(this.TargetDamageClusters, target, attacker.MeleeWeapon);

            string targetDamageS = adjTargetDamage.Count() > 1 ?
                $"{adjTargetDamage.Sum()} ({DamageHelper.ClusterDamageStringForUI(adjTargetDamage)})" :
                adjTargetDamage[0].ToString();

            string targetTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Standard];
            if (this.AttackerTable == DamageTable.PUNCH)
                targetTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Punch];
            if (this.AttackerTable == DamageTable.KICK)
                targetTable = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Table_Legs];

            string localText = new Text(
                Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_Physical_Weapon_Desc],
                new object[] { targetDamageS, this.TargetInstability, targetTable })
                .ToString();

            this.DescriptionNotes.Add(localText);
        }

        private void CalculateModifiers(Mech attacker, AbstractActor target)
        {
            // If target is prone, -2 modifier
            if (target.IsProne)
                this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

            // +2 to hit for each upper/lower actuator hit
            ActorMeleeCondition attackerCondition = ModState.GetMeleeCondition(attacker);
            int leftArmMalus = (2 - attackerCondition.LeftArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            int rightArmMalus = (2 - attackerCondition.RightArmActuatorsCount) * Mod.Config.Melee.Punch.ArmActuatorDamageMalus;
            int bestMalus = leftArmMalus <= rightArmMalus ? leftArmMalus : rightArmMalus;

            // If the ignore actuators stat is set, set the malus to 0 regardless of damage
            Statistic ignoreActuatorsStat = attacker.StatCollection.GetStatistic(ModStats.PhysicalWeaponIgnoreActuators);
            if (ignoreActuatorsStat != null && ignoreActuatorsStat.Value<bool>())
            {
                bestMalus = 0;
            }

            // Add actuator damage if it exists
            if (bestMalus != 0)
                this.AttackModifiers.Add(ModText.LT_Label_Actuator_Damage, bestMalus);

            // Check for attack modifier statistic
            Statistic phyWeapAttackMod = attacker.StatCollection.GetStatistic(ModStats.PhysicalWeaponAttackMod);
            if (phyWeapAttackMod != null && phyWeapAttackMod.Value<int>() != 0)
            {
                this.AttackModifiers.Add(ModText.LT_Label_Physical_Weapon_Attack_Mod, attacker.StatCollection.GetValue<int>(ModStats.PhysicalWeaponAttackMod));
            }
        }

        private void CalculateDamages(Mech attacker, AbstractActor target)
        {
            Mod.MeleeLog.Info?.Write($"Calculating PHYSICAL WEAPON damage for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float damage = attacker.PhysicalWeaponDamage();
            damage = target.ApplyPhysicalWeaponDamageReduction(damage);

            // Target damage applies as a single modifier
            this.TargetDamageClusters = AttackHelper.CreateDamageClustersWithExtraAttacks(attacker, damage, ModStats.PhysicalWeaponExtraHitsCount);
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
            Mod.MeleeLog.Info?.Write($"Calculating PHYSICAL WEAPON instability for attacker: {CombatantUtils.Label(attacker)} " +
                $"vs. target: {CombatantUtils.Label(target)}");

            float instab = attacker.PhysicalWeaponInstability();
            instab = target.ApplyPhysicalWeaponInstabReduction(instab);
            this.TargetInstability = instab;
        }
    }
}

using CBTBehaviorsEnhanced.Helper;
using CustomComponents;
using CustomUnits;
using IRBTModUtils.Extension;

namespace CBTBehaviorsEnhanced
{
    public class ActorMeleeCondition
    {
        // Damage multipliers and effects
        private bool leftHipIsFunctional = false;
        private bool rightHipIsFunctional = false;

        public int LeftLegActuatorsCount { get => leftLegActuatorsCount; }
        private int leftLegActuatorsCount = 0;
        public int RightLegActuatorsCount { get => rightLegActuatorsCount; }
        private int rightLegActuatorsCount = 0;

        public bool LeftFootIsFunctional { get => leftFootIsFunctional; }
        private bool leftFootIsFunctional = false;
        public bool RightFootIsFunctional { get => rightFootIsFunctional; }
        private bool rightFootIsFunctional = false;

        private bool leftShoulderIsFunctional = false;
        private bool rightShoulderIsFunctional = false;

        public int LeftArmActuatorsCount { get => leftArmActuatorsCount; }
        private int leftArmActuatorsCount = 0;
        public int RightArmActuatorsCount { get => rightArmActuatorsCount; }
        private int rightArmActuatorsCount = 0;

        public bool LeftHandIsFunctional { get => leftHandIsFunctional; }
        private bool leftHandIsFunctional = false;
        public bool RightHandIsFunctional { get => rightHandIsFunctional; }
        private bool rightHandIsFunctional = false;

        private bool canMelee = false;
        private bool hasPhysicalAttack = false;

        AbstractActor actor;

        public ActorMeleeCondition(AbstractActor actor)
        {
            this.actor = actor;
            Mod.MeleeLog.Info?.Write($"Calculating melee condition for actor: {actor.DistinctId()}");

            if (Mod.Config.Developer.ForceInvalidateAllMeleeAttacks)
            {
                Mod.MeleeLog.Info?.Write(" -- melee invalidated by developer flag.");
                canMelee = false;
                return;
            }
            else if (actor.IsDead || actor.IsFlaggedForDeath)
            {
                Mod.MeleeLog.Info?.Write(" -- Cannot melee when dead");
                canMelee = false;
                return;
            }

            // If we're in non-interleaved, prevent combat
            if (!actor.Combat.TurnDirector.IsInterleaved)
            {
                Mod.MeleeLog.Info?.Write(" -- Cannot melee in non-interleaved mode");
                canMelee = false;
                return;
            }

            // Vehicles can charge so long as they have movement, don't look for movement crits
            if (actor.IsVehicle() || actor.IsNaval())
            {
                if (Mod.Config.Melee.DisableMeleeForVehicles)
                {
                    Mod.MeleeLog.Info?.Write(" -- melee for vehicles disabled by configuration");
                    canMelee = false;
                    return;
                }

                Mech fakeMech = actor as Mech;
                UnitCustomInfo customInfo = fakeMech?.GetCustomInfo();
                if (customInfo != null && customInfo.FlyingHeight > 2f)
                {
                    Mod.MeleeLog.Info?.Write(" -- actor is vehicle or naval with flying height > 2.0, cannot melee");
                    canMelee = false;
                    return;
                }

                if (actor.MaxSpeed > 0f)
                {
                    Mod.MeleeLog.Info?.Write(" -- actor is vehicle or naval with movement, can charge");
                    canMelee = true;
                }
                return;
            }

            // Troopers can always physweap attack, but cannot make any other attacks
            bool punchIsPhysicalWeapon = actor.StatCollection.GetValue<bool>(ModStats.PunchIsPhysicalWeapon);
            if (actor.IsTrooper())
            {
                // Check that unit has a physical attack
                if (punchIsPhysicalWeapon)
                {
                    Mod.MeleeLog.Info?.Write(" -- actor is trooper with physical weapon");
                    hasPhysicalAttack = true;
                    canMelee = true;
                }
                else
                {
                    Mod.MeleeLog.Warn?.Write(" -- actor is trooper but has no physical weapon, disabling melee!");
                    canMelee = false;
                }
                return;
            }

            // Fake vehicles should already be caught by this point
            if (actor is Mech mech)
            {
                if (mech.IsOrWillBeProne || mech.StoodUpThisRound || mech.IsFlaggedForKnockdown)
                {
                    Mod.MeleeLog.Info?.Write(" -- cannot melee when you stand up or are being knocked down");
                    canMelee = false;
                    return;
                }

                if (mech.IsQuadMech())
                {
                    Statistic nonBipedPhysicalAttack = mech.StatCollection.GetStatistic(ModStats.PhysicalWeaponNonBiped);
                    if (nonBipedPhysicalAttack.Value<bool>())
                    {
                        Mod.MeleeLog.Info?.Write(" -- unit is quad with physical weapon");
                        hasPhysicalAttack = true;
                    }
                    else
                    {
                        Mod.MeleeLog.Info?.Write(" -- unit is quad but does not have nonbiped weapon, skipping");
                    }
                }
                else
                {
                    Mod.MeleeLog.Info?.Write(" -- unit is not a quad mech");
                }

                if (ModState.MEIsLoaded)
                {
                    foreach (MechComponent mc in mech.allComponents)
                    {
                        switch (mc.Location)
                        {
                            case (int)ChassisLocations.LeftArm:
                            case (int)ChassisLocations.RightArm:
                                EvaluateArmComponent(mc);
                                break;
                            case (int)ChassisLocations.LeftLeg:
                            case (int)ChassisLocations.RightLeg:
                                EvaluateLegComponent(mc);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    // Vanilla has no concept of actuators, so mark everything as present. 
                    this.leftHipIsFunctional = true;
                    this.rightHipIsFunctional = true;
                    this.leftLegActuatorsCount = 2;
                    this.rightLegActuatorsCount = 2;
                    this.leftFootIsFunctional = true;
                    this.rightFootIsFunctional = true;
                    this.leftShoulderIsFunctional = true;
                    this.rightShoulderIsFunctional = true;
                    this.leftArmActuatorsCount = 2;
                    this.rightArmActuatorsCount = 2;
                    this.leftHandIsFunctional = true;
                    this.rightHandIsFunctional = true;
                }


                // Check that unit has a physical attack
                if (punchIsPhysicalWeapon)
                {
                    Mod.MeleeLog.Info?.Write(" -- unit has physical weapon");
                    hasPhysicalAttack = true;
                }

                Mod.MeleeLog.Info?.Write(" -- unit can melee");
                canMelee = true;
            }
            else
            {
                Mod.MeleeLog.Warn?.Write($"  - actor is not a vehicle, naval, trooper, or mech, WTF? Marking is as no melee");
            }

        }

        // Only used for testing... yeah, yeah I know
        public ActorMeleeCondition(AbstractActor actor,
            bool leftHip, bool rightHip, int leftLeg, int rightLeg,
            bool leftFoot, bool rightFoot, bool leftShoulder, bool rightShoulder,
            int leftArm, int rightArm, bool leftHand, bool rightHand, bool canMelee, bool hasPhysical)
        {
            this.actor = actor;
            this.leftHipIsFunctional = leftHip;
            this.rightHipIsFunctional = rightHip;
            this.leftLegActuatorsCount = leftLeg;
            this.rightLegActuatorsCount = rightLeg;
            this.leftFootIsFunctional = leftFoot;
            this.rightFootIsFunctional = rightFoot;
            this.leftShoulderIsFunctional = leftShoulder;
            this.rightShoulderIsFunctional = rightShoulder;
            this.leftArmActuatorsCount = leftArm;
            this.rightArmActuatorsCount = rightArm;
            this.leftHandIsFunctional = leftHand;
            this.rightHandIsFunctional = rightHand;
            this.canMelee = canMelee;
            this.hasPhysicalAttack = hasPhysical;
        }

        public bool CanCharge()
        {
            if (!canMelee) return false;

            // Troopers can only use physical attacks
            if (actor.IsTrooper()) return false;

            // Vehicles can charge if they have speed to make an attack
            if (actor.IsVehicle() || actor.IsNaval())
            {

                if (actor.MaxSpeed > 0f) return true;
                else return false;
            }

            // Cannot charge while unsteady
            if (actor.IsUnsteady) return false;

            return true;
        }

        public bool CanDFA()
        {
            if (!canMelee) return false;

            // Troopers can only use physical attacks
            if (actor.IsTrooper())
            {
                return Mod.Config.Melee.DFA.EnableTrooperDFAButSeriouslyGetOnStratOpsAlready;
            }

            // Vehicles can charge if they have speed to make an attack
            if (actor.IsVehicle() || actor.IsNaval())
            {
                // TODO: Support the Kanga doing the bongo
                return false;
            }

            if (actor is Mech mech && !mech.CanDFA) return false;

            return true;

        }

        // Public functions
        public bool CanKick()
        {
            if (!canMelee) return false;

            // Troopers can only use physical attacks. Naval and vehicles cannot punch.
            if (actor.IsVehicle() || actor.IsNaval() || actor.IsTrooper()) return false;

            // Can't kick with damaged hip actuators
            if (!leftHipIsFunctional || !rightHipIsFunctional) return false;

            return true;
        }

        public bool CanUsePhysicalAttack()
        {
            if (!canMelee) return false;

            if (!hasPhysicalAttack) return false;

            // If we have a physical weapon, and we're a trooper squad, we can melee
            if (actor.IsTrooper()) return true;

            // Even if you have a physical attack... just no.
            if (actor.IsVehicle() || actor.IsNaval()) return false;

            // If the ignore actuators stat is set, allow the attack regardless of actuator damage
            Mech mech = actor as Mech;

            // Quad mechs do not typically have weapons... but some (Kiso) can. This ignores all actuator checks entirely!
            if (actor.IsQuadMech())
            {
                Statistic nonBipedPhyiscalAttack = mech.StatCollection.GetStatistic(ModStats.PhysicalWeaponNonBiped);
                return nonBipedPhyiscalAttack.Value<bool>();
            }

            Statistic ignoreActuatorsStat = mech.StatCollection.GetStatistic(ModStats.PhysicalWeaponIgnoreActuators);
            if (ignoreActuatorsStat != null && ignoreActuatorsStat.Value<bool>())
            {
                Mod.MeleeLog.Debug?.Write($"Actor has ignoreActuators set, allowing use of physical attack");
                return true;
            }

            // Damage check - shoulder and hand
            bool leftArmIsFunctional = leftShoulderIsFunctional && leftHandIsFunctional;
            bool rightArmIsFunctional = rightShoulderIsFunctional && rightHandIsFunctional;
            if (!leftArmIsFunctional && !rightArmIsFunctional)
            {
                Mod.MeleeLog.Debug?.Write($"Both left and right shoulder & hands are non-functional, cannot use physical attack.");
                return false;
            }

            return true;
        }

        public bool CanPunch()
        {

            if (!canMelee) return false;

            // Troopers can only use physical attacks. Naval and vehicles cannot punch.
            if (actor.IsVehicle() || actor.IsNaval() || actor.IsTrooper()) return false;

            // Quad mechs cannot punch
            if (actor.IsQuadMech()) return false;

            // Check for mech
            if (actor is Mech)
            {
                // Can't punch with damaged shoulders
                if (!leftShoulderIsFunctional && !rightShoulderIsFunctional) return false;
            }

            return true;
        }

        // Private helper
        private void EvaluateLegComponent(MechComponent mc)
        {
            Mod.MeleeLog.Info?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");

            foreach (string categoryId in Mod.Config.CustomCategories.HipActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftHipIsFunctional = mc.IsFunctional;
                    else this.rightHipIsFunctional = mc.IsFunctional;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.UpperLegActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    int mod = mc.IsFunctional ? 1 : 0;
                    if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftLegActuatorsCount += mod;
                    else this.rightLegActuatorsCount += mod;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.LowerLegActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    int mod = mc.IsFunctional ? 1 : 0;
                    if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftLegActuatorsCount += mod;
                    else this.rightLegActuatorsCount += mod;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.FootActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftFootIsFunctional = mc.IsFunctional;
                    else this.rightFootIsFunctional = mc.IsFunctional;
                    break;
                }
            }

        }

        private void EvaluateArmComponent(MechComponent mc)
        {
            Mod.MeleeLog.Debug?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");

            foreach (string categoryId in Mod.Config.CustomCategories.ShoulderActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    if (mc.Location == (int)ChassisLocations.LeftArm) this.leftShoulderIsFunctional = mc.IsFunctional;
                    else this.rightShoulderIsFunctional = mc.IsFunctional;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.UpperArmActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    int mod = mc.IsFunctional ? 1 : 0;
                    if (mc.Location == (int)ChassisLocations.LeftArm) this.leftArmActuatorsCount += mod;
                    else this.rightArmActuatorsCount += mod;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.LowerArmActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    int mod = mc.IsFunctional ? 1 : 0;
                    if (mc.Location == (int)ChassisLocations.LeftArm) this.leftArmActuatorsCount += mod;
                    else this.rightArmActuatorsCount += mod;
                    break;
                }
            }

            foreach (string categoryId in Mod.Config.CustomCategories.HandActuatorCategoryId)
            {
                if (mc.mechComponentRef.IsCategory(categoryId))
                {
                    if (mc.Location == (int)ChassisLocations.LeftArm) this.leftHandIsFunctional = mc.IsFunctional;
                    else this.rightHandIsFunctional = mc.IsFunctional;
                    break;
                }
            }
        }

    }

}

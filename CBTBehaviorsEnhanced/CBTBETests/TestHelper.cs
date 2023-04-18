using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Patches;
using System;
using System.Reflection;
using UnityEngine;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace CBTBETests
{
    public static class TestHelper
    {
        public static Mech BuildTestMech(float tonnage)
        {
            Mech mech = new Mech();

            DescriptionDef descriptionDef = new DescriptionDef("foo", "bar", "raboof", "", 100, 0, true, "", "", "");
            ChassisDef chassisDef = new ChassisDef(descriptionDef, "", "", "", "", "", tonnage, tonnage, WeightClass.ASSAULT,
                0, 0, 0, 0, 0, 0, new float[] { 0 }, 0, 0, 0, 0, 0,
                true, 0, 0, 0, 0, 0, 0, 0, 0, new LocationDef[] { }, new MechComponentRef[] { },
                new HBS.Collections.TagSet());
            chassisDef.Tonnage = tonnage;

            MechDef mechDef = new MechDef();
            mechDef._chassisDef = chassisDef;
            mechDef.chassisID = descriptionDef.Id;
            mech.MechDef = mechDef;

            mech = (Mech)InitAbstractActor(mech);
            mech.isPilotable = true;

            mech.StatCollection.AddStatistic("Head.Structure", 1f);
            mech.StatCollection.AddStatistic("CenterTorso.Structure", 1f);
            mech.StatCollection.AddStatistic("LeftTorso.Structure", 1f);
            mech.StatCollection.AddStatistic("RightTorso.Structure", 1f);
            mech.StatCollection.AddStatistic("LeftArm.Structure", 1f);
            mech.StatCollection.AddStatistic("RightArm.Structure", 1f);
            mech.StatCollection.AddStatistic("LeftLeg.Structure", 1f);
            mech.StatCollection.AddStatistic("RightLeg.Structure", 1f);

            InitModStats(mech);

            mech.pilot = BuildTestPilot();
            mech.pilot._parentActor = mech;

            return mech;
        }

        public static Turret BuildTestTurret(float tonnage)
        {
            Turret turret = new Turret();
            return (Turret)InitAbstractActor(turret);
        }

        public static Vehicle BuildTestVehicle(float tonnage)
        {
            Vehicle vehicle = new Vehicle();
            vehicle.VehicleDef.Chassis.Tonnage = tonnage; ;

            return (Vehicle)InitAbstractActor(vehicle);
        }

        public static ActorMeleeCondition AllEnabledCondition(Mech mech)
        {
            ActorMeleeCondition mmc = new ActorMeleeCondition(mech,
                true, true, 2, 2, true, true,
                true, true, 2, 2, true, true,
                true, true);

            return mmc;
        }

        // Do this after initialization to allow CU to bootstrap CustomInfo
        private static void InitModStats(AbstractActor actor)
        {
        }

        private static AbstractActor InitAbstractActor(AbstractActor actor)
        {
            // Init the combat ref for constants
            ConstructorInfo constantsCI = AccessTools.Constructor(typeof(CombatGameConstants), new Type[] { });
            CombatGameConstants constants = (CombatGameConstants)constantsCI.Invoke(new object[] { });

            CombatGameState cgs = new CombatGameState();
            cgs.Constants = constants;

            actor.Combat = cgs;

            // Init any required stats
            actor.StatCollection = new StatCollection();

            // ModStats
            //actor.StatCollection.AddStatistic<int>(ModStats.PunchAttackMod, 0);

            // Vanilla
            actor.StatCollection.AddStatistic<float>("SensorSignatureModifier", 1.0f);

            return actor;
        }

        private static Pilot BuildTestPilot()
        {
            HumanDescriptionDef pilotDescDef = new HumanDescriptionDef();
            pilotDescDef.Name = "PilotFoo";

            PilotDef pilotDef = new PilotDef();
            Guid guid = new Guid();
            pilotDef.Description = pilotDescDef;

            pilotDef.PilotTags = new HBS.Collections.TagSet();

            Pilot pilot = new Pilot(pilotDef, guid.ToString(), false);

            // IRTweaks ExtendedStats integration
            pilot.StatCollection.SetValidator<int>("Piloting", new Statistic.Validator<int>(CustomPilotAttributeValidator<int>));
            pilot.StatCollection.SetValidator<int>("Gunnery", new Statistic.Validator<int>(CustomPilotAttributeValidator<int>));
            pilot.StatCollection.SetValidator<int>("Guts", new Statistic.Validator<int>(CustomPilotAttributeValidator<int>));
            pilot.StatCollection.SetValidator<int>("Tactics", new Statistic.Validator<int>(CustomPilotAttributeValidator<int>));

            return pilot;
        }

        static bool CustomPilotAttributeValidator<T>(ref int newValue)
        {
            newValue = Mathf.Clamp(newValue, 1, 20);
            return true;
        }

        public static Weapon BuildTestWeapon(float minRange = 0f, float shortRange = 0f,
            float mediumRange = 0f, float longRange = 0f, float maxRange = 0f)
        {
            Weapon weapon = new Weapon();

            StatCollection statCollection = new StatCollection();
            statCollection.AddStatistic("MinRange", minRange);
            statCollection.AddStatistic("MinRangeMultiplier", 1f);
            statCollection.AddStatistic("LongRangeModifier", 0f);
            statCollection.AddStatistic("MaxRange", maxRange);
            statCollection.AddStatistic("MaxRangeModifier", 0f);
            statCollection.AddStatistic("ShortRange", shortRange);
            statCollection.AddStatistic("MediumRange", mediumRange);
            statCollection.AddStatistic("LongRange", longRange);

            weapon.statCollection = statCollection;

            return weapon;
        }
    }
}

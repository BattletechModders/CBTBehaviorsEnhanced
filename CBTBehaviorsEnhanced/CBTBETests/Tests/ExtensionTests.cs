using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CBTBETests
{


    [TestClass]
    public class ExtensionTests
    {

        [TestInitialize]
        public void TestSetup()
        {
            // IRBTModUtils setup
            IRBTModUtils.Mod.Config = new IRBTModUtils.ModConfig();
            IRBTModUtils.Mod.Config.SkillsToModifiers.Piloting.RatingToModifier = new Dictionary<int, int>()
            {
                { 1, 0 }, { 2, 1 }, { 3, 1 }, { 4, 2 }, {5, 2 }, {6, 3 }, {7, 3 },
                { 8, 4 }, { 9, 4 }, { 10, 5 }, { 11, 6 }, { 12, 6 }, { 13, 6 }, { 14, 6 },
                { 15, 7 }, { 16, 7 }, { 17, 7 }, { 18, 7 }, { 19, 8 }, { 20, 8 }
            };
            IRBTModUtils.Mod.Config.SkillsToModifiers.Piloting.ModifierBonusAbilities =
                new List<string>() { "AbilityDefP5", "AbilityDefP8" };
            IRBTModUtils.Mod.Config.SkillsToModifiers.Piloting.BonusMultiplier = 1;
        }

        [TestMethod]
        public void TestCheckMod_Piloting()
        {
            Mech mech = TestHelper.BuildTestMech(tonnage: 100);
            mech.StatCollection.AddStatistic<int>(ModStats.ActuatorDamageMalus, 0);

            // Skill:1 => bonus +0
            mech.pilot.StatCollection.Set<int>("Piloting", 1);
            float checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.00f, checkMod);

            // Skill:3 => bonus +1
            mech.pilot.StatCollection.Set<int>("Piloting", 3);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.05f, checkMod);

            // Skill:10 => bonus +5
            mech.pilot.StatCollection.Set<int>("Piloting", 10);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.25f, checkMod);

            // Skill:20 => bonus +8
            mech.pilot.StatCollection.Set<int>("Piloting", 20);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.40f, checkMod);
        }

        [TestMethod]
        public void TestCheckMod_Piloting_ActuatorDamage()
        {
            Mech mech = TestHelper.BuildTestMech(tonnage: 100);
            mech.StatCollection.AddStatistic<int>(ModStats.ActuatorDamageMalus, 0);

            // ---------------------
            mech.StatCollection.Set<int>(ModStats.ActuatorDamageMalus, -1);

            // Skill:1 => bonus +0, -1 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 1);
            float checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.00f, checkMod);

            // Skill:3 => bonus +1, -1 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 3);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.00f, checkMod);

            // Skill:10 => bonus +5, -1 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 10);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.20f, checkMod);

            // Skill:20 => bonus +8, -1 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 20);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.35f, checkMod);


            // ---------------------
            mech.StatCollection.Set<int>(ModStats.ActuatorDamageMalus, -3);

            // Skill:1 => bonus +0, -3 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 1);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.00f, checkMod);

            // Skill:3 => bonus +1, -3 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 3);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.00f, checkMod);

            // Skill:10 => bonus +5, -3 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 10);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.10f, checkMod);

            // Skill:20 => bonus +8, -3 for actuator damage
            mech.pilot.StatCollection.Set<int>("Piloting", 20);
            checkMod = mech.PilotCheckMod(0.05f);
            Assert.AreEqual(0.25f, checkMod);
        }

    }
}

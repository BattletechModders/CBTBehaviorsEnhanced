using BattleTech;
using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CBTBETests
{
    [TestClass]
    public class PunchTests
    {
        [TestMethod]
        public void TestPunchDamage_NoStats_UndamagedAttacker()
        {
            Mech attacker20 = TestHelper.BuildTestMech(tonnage: 20);
            MechMeleeCondition attackerCondition20 = TestHelper.AllEnabledCondition(attacker20);
            Assert.AreEqual(10, attacker20.PunchDamage(attackerCondition20));

            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            MechMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);
            Assert.AreEqual(25, attacker50.PunchDamage(attackerCondition50));

            Mech attacker75 = TestHelper.BuildTestMech(tonnage: 75);
            MechMeleeCondition attackerCondition75 = TestHelper.AllEnabledCondition(attacker75);
            Assert.AreEqual(38, attacker75.PunchDamage(attackerCondition75));

            Mech attacker100 = TestHelper.BuildTestMech(tonnage: 100);
            MechMeleeCondition attackerCondition100 = TestHelper.AllEnabledCondition(attacker100);
            Assert.AreEqual(50, attacker100.PunchDamage(attackerCondition100));

            Mech attacker130 = TestHelper.BuildTestMech(tonnage: 130);
            MechMeleeCondition attackerCondition130 = TestHelper.AllEnabledCondition(attacker130);
            Assert.AreEqual(65, attacker130.PunchDamage(attackerCondition130));

        }

        [TestMethod]
        public void TestPunchDamage_TargetDamageStat_UndamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            MechMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);
            
            attacker50.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamage, 2f);
            Assert.AreEqual(100, attacker50.PunchDamage(attackerCondition50));

            // Test override stat @ 0; should default to ModValue
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 3.0f;
            attacker50.StatCollection.Set<float>(ModStats.PunchTargetDamage, 0);
            Assert.AreEqual(150, attacker50.PunchDamage(attackerCondition50));

            // Test override stat @ negatives; should default to modValue
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 4.0f;
            attacker50.StatCollection.Set<float>(ModStats.PunchTargetDamage, -20);
            Assert.AreEqual(200, attacker50.PunchDamage(attackerCondition50));

            // Reset for other tests
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 0.5f;
        }

        [TestMethod]
        public void TestPunchDamage_TargetDamageStatMods_UndamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            MechMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);

            attacker50.StatCollection.AddStatistic<int>(ModStats.PunchTargetDamageMod, 50);
            attacker50.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamageMulti, 10f);
            // RoundUP ((( 0.5 * 50) + 50) * 10 => 750
            Assert.AreEqual(750, attacker50.PunchDamage(attackerCondition50));
        }

        [TestMethod]
        public void TestPunchDamage_DamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            MechMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);

            // No penalty for hands
            attackerCondition50.LeftHandIsFunctional = false;
            attackerCondition50.RightHandIsFunctional = false;
            Assert.AreEqual(25, attacker50.PunchDamage(attackerCondition50));

            // 0.25 reduction for one missing arm actuator
            attackerCondition50.LeftArmActuatorsCount = 1;
            attackerCondition50.RightArmActuatorsCount = 1;
            Assert.AreEqual(19, attacker50.PunchDamage(attackerCondition50));

            // 0.5 reduction for both missing arm actuators
            attackerCondition50.LeftArmActuatorsCount = 0;
            attackerCondition50.RightArmActuatorsCount = 0;
            Assert.AreEqual(13, attacker50.PunchDamage(attackerCondition50));

            // 0 damage if shoulders are disabled
            attackerCondition50.LeftShoulderIsFunctional = false;
            attackerCondition50.RightShoulderIsFunctional = false;
            Assert.AreEqual(0, attacker50.PunchDamage(attackerCondition50));

            // Left arm damaged doesn't impact damage if right arm is fine
            attackerCondition50.RightHandIsFunctional = true;
            attackerCondition50.RightArmActuatorsCount = 2;
            attackerCondition50.RightShoulderIsFunctional = true;
            Assert.AreEqual(25, attacker50.PunchDamage(attackerCondition50));

            // Right arm damaged doesn't impact damage if left arm is fine
            attackerCondition50.LeftHandIsFunctional = true;
            attackerCondition50.LeftArmActuatorsCount = 2;
            attackerCondition50.LeftShoulderIsFunctional = true;
            attackerCondition50.RightHandIsFunctional = false;
            attackerCondition50.RightArmActuatorsCount = 0;
            attackerCondition50.RightShoulderIsFunctional = false;
            Assert.AreEqual(25, attacker50.PunchDamage(attackerCondition50));
        }

    }
}

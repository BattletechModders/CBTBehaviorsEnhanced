using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Extensions;
using IRBTModUtils.Extension;
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
            ActorMeleeCondition attackerCondition20 = TestHelper.AllEnabledCondition(attacker20);
            ModState.meleeConditionCache[attacker20.DistinctId()] = attackerCondition20;
            Assert.AreEqual(10, attacker20.PunchDamage());

            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            ActorMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);
            ModState.meleeConditionCache[attacker50.DistinctId()] = attackerCondition50;
            Assert.AreEqual(25, attacker50.PunchDamage());

            Mech attacker75 = TestHelper.BuildTestMech(tonnage: 75);
            ActorMeleeCondition attackerCondition75 = TestHelper.AllEnabledCondition(attacker75);
            ModState.meleeConditionCache[attacker75.DistinctId()] = attackerCondition75;
            Assert.AreEqual(38, attacker75.PunchDamage());

            Mech attacker100 = TestHelper.BuildTestMech(tonnage: 100);
            ActorMeleeCondition attackerCondition100 = TestHelper.AllEnabledCondition(attacker100);
            ModState.meleeConditionCache[attacker100.DistinctId()] = attackerCondition100;
            Assert.AreEqual(50, attacker100.PunchDamage());

            Mech attacker130 = TestHelper.BuildTestMech(tonnage: 130);
            ActorMeleeCondition attackerCondition130 = TestHelper.AllEnabledCondition(attacker130);
            ModState.meleeConditionCache[attacker130.DistinctId()] = attackerCondition130;
            Assert.AreEqual(65, attacker130.PunchDamage());

        }

        [TestMethod]
        public void TestPunchDamage_TargetDamageStat_UndamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            ActorMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);
            ModState.meleeConditionCache[attacker50.DistinctId()] = attackerCondition50;

            attacker50.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamage, 2f);
            Assert.AreEqual(100, attacker50.PunchDamage());

            // Test override stat @ 0; should default to ModValue
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 3.0f;
            attacker50.StatCollection.Set<float>(ModStats.PunchTargetDamage, 0);
            Assert.AreEqual(150, attacker50.PunchDamage());

            // Test override stat @ negatives; should default to modValue
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 4.0f;
            attacker50.StatCollection.Set<float>(ModStats.PunchTargetDamage, -20);
            Assert.AreEqual(200, attacker50.PunchDamage());

            // Reset for other tests
            Mod.Config.Melee.Punch.TargetDamagePerAttackerTon = 0.5f;
        }

        [TestMethod]
        public void TestPunchDamage_TargetDamageStatMods_UndamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            ActorMeleeCondition attackerCondition50 = TestHelper.AllEnabledCondition(attacker50);
            ModState.meleeConditionCache[attacker50.DistinctId()] = attackerCondition50;

            attacker50.StatCollection.AddStatistic<int>(ModStats.PunchTargetDamageMod, 50);
            attacker50.StatCollection.AddStatistic<float>(ModStats.PunchTargetDamageMulti, 10f);
            // RoundUP ((( 0.5 * 50) + 50) * 10 => 750
            Assert.AreEqual(750, attacker50.PunchDamage());
        }

        [TestMethod]
        public void TestPunchDamage_DamagedAttacker()
        {
            // Test override stat
            Mech attacker50 = TestHelper.BuildTestMech(tonnage: 50);
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: true, rightShoulder: true, leftArm: 2, rightArm: 2, leftHand: true, rightHand: true,
                canMelee: true, hasPhysical: true);

            // No penalty for hands
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: true, rightShoulder: true, leftArm: 2, rightArm: 2, leftHand: false, rightHand: false,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(25, attacker50.PunchDamage());

            // 0.25 reduction for one missing arm actuator
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: true, rightShoulder: true, leftArm: 1, rightArm: 1, leftHand: false, rightHand: false,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(19, attacker50.PunchDamage());

            // 0.5 reduction for both missing arm actuators
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: true, rightShoulder: true, leftArm: 0, rightArm: 0, leftHand: false, rightHand: false,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(13, attacker50.PunchDamage());

            // 0 damage if shoulders are disabled
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: false, rightShoulder: false, leftArm: 0, rightArm: 0, leftHand: false, rightHand: false,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(0, attacker50.PunchDamage());

            // Left arm damaged doesn't impact damage if right arm is fine
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: false, rightShoulder: true, leftArm: 0, rightArm: 2, leftHand: false, rightHand: true,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(25, attacker50.PunchDamage());

            // Right arm damaged doesn't impact damage if left arm is fine
            ModState.meleeConditionCache[attacker50.DistinctId()] = new ActorMeleeCondition(attacker50,
                leftHip: true, rightHip: true, leftLeg: 2, rightLeg: 2, leftFoot: true, rightFoot: true,
                leftShoulder: true, rightShoulder: false, leftArm: 2, rightArm: 0, leftHand: true, rightHand: false,
                canMelee: true, hasPhysical: true);
            Assert.AreEqual(25, attacker50.PunchDamage());
        }

    }
}

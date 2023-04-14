using CBTBehaviorsEnhanced.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CBTBETests
{
    [TestClass]
    public class ChargeTests
    {
        [TestMethod]
        public void TestChargeInstab_Attacker_NoStats()
        {
            Mech attacker20 = TestHelper.BuildTestMech(tonnage: 20);

            // instab = AttackerInstabPerTargetTon * targetTonnage * hexesMoved
            Assert.AreEqual(10, attacker20.ChargeAttackerInstability(20, 1));
            Assert.AreEqual(30, attacker20.ChargeAttackerInstability(20, 3));
            Assert.AreEqual(50, attacker20.ChargeAttackerInstability(20, 5));
            Assert.AreEqual(90, attacker20.ChargeAttackerInstability(20, 9));

            Assert.AreEqual(23, attacker20.ChargeAttackerInstability(45, 1));
            Assert.AreEqual(68, attacker20.ChargeAttackerInstability(45, 3));
            Assert.AreEqual(113, attacker20.ChargeAttackerInstability(45, 5));
            Assert.AreEqual(203, attacker20.ChargeAttackerInstability(45, 9));

            Assert.AreEqual(80, attacker20.ChargeAttackerInstability(80, 2));
            Assert.AreEqual(160, attacker20.ChargeAttackerInstability(80, 4));
            Assert.AreEqual(240, attacker20.ChargeAttackerInstability(80, 6));
            Assert.AreEqual(320, attacker20.ChargeAttackerInstability(80, 8));

            // Attacker size doesn't matter
            Mech attacker100 = TestHelper.BuildTestMech(tonnage: 100);
            Assert.AreEqual(10, attacker100.ChargeAttackerInstability(20, 1));
            Assert.AreEqual(30, attacker100.ChargeAttackerInstability(20, 3));
            Assert.AreEqual(50, attacker100.ChargeAttackerInstability(20, 5));
            Assert.AreEqual(90, attacker100.ChargeAttackerInstability(20, 9));
        }

        [TestMethod]
        public void TestChargeInstab_Target_NoStats()
        {
            Mech attacker20 = TestHelper.BuildTestMech(tonnage: 20);

            // instab = AttackerInstabPerTargetTon * attackerTonnage * hexesMoved
            Assert.AreEqual(10, attacker20.ChargeTargetInstability(20, 1));
            Assert.AreEqual(30, attacker20.ChargeTargetInstability(20, 3));
            Assert.AreEqual(50, attacker20.ChargeTargetInstability(20, 5));
            Assert.AreEqual(90, attacker20.ChargeTargetInstability(20, 9));

            Assert.AreEqual(10, attacker20.ChargeTargetInstability(45, 1));
            Assert.AreEqual(30, attacker20.ChargeTargetInstability(45, 3));
            Assert.AreEqual(50, attacker20.ChargeTargetInstability(45, 5));
            Assert.AreEqual(90, attacker20.ChargeTargetInstability(45, 9));

            Assert.AreEqual(20, attacker20.ChargeTargetInstability(80, 2));
            Assert.AreEqual(40, attacker20.ChargeTargetInstability(80, 4));
            Assert.AreEqual(60, attacker20.ChargeTargetInstability(80, 6));
            Assert.AreEqual(80, attacker20.ChargeTargetInstability(80, 8));

            // Attacker tonnage *does* matter here
            Mech attacker45 = TestHelper.BuildTestMech(tonnage: 45);
            Assert.AreEqual(23, attacker45.ChargeTargetInstability(20, 1));
            Assert.AreEqual(68, attacker45.ChargeTargetInstability(20, 3));
            Assert.AreEqual(113, attacker45.ChargeTargetInstability(20, 5));
            Assert.AreEqual(203, attacker45.ChargeTargetInstability(20, 9));

            Mech attacker60 = TestHelper.BuildTestMech(tonnage: 60);
            Assert.AreEqual(30, attacker60.ChargeTargetInstability(20, 1));
            Assert.AreEqual(90, attacker60.ChargeTargetInstability(20, 3));
            Assert.AreEqual(150, attacker60.ChargeTargetInstability(20, 5));
            Assert.AreEqual(270, attacker60.ChargeTargetInstability(20, 9));

            Mech attacker85 = TestHelper.BuildTestMech(tonnage: 85);
            Assert.AreEqual(43, attacker85.ChargeTargetInstability(20, 1));
            Assert.AreEqual(128, attacker85.ChargeTargetInstability(20, 3));
            Assert.AreEqual(213, attacker85.ChargeTargetInstability(20, 5));
            Assert.AreEqual(383, attacker85.ChargeTargetInstability(20, 9));
        }
    }
}

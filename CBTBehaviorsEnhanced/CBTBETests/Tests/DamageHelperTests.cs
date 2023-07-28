using BattleTech;
using CBTBehaviorsEnhanced;
using CBTBehaviorsEnhanced.Extensions;
using CBTBehaviorsEnhanced.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using UnityEngine;

namespace CBTBETests
{

    [TestClass]
    public class DamageHelperTests
    {
        [TestMethod]
        public void TestClusterDamageStrings()
        {
            float[] clusterDamage17 = new float[] { 17 };
            Assert.AreEqual("17", DamageHelper.ClusterDamageStringForUI(clusterDamage17));

            float[] clusterDamage56 = new float[] { 25, 25, 6 };
            Assert.AreEqual("25x2, 6", DamageHelper.ClusterDamageStringForUI(clusterDamage56));

            float[] clusterDamage150 = new float[] { 25, 25, 25, 25, 25, 25 };
            Assert.AreEqual("25x6", DamageHelper.ClusterDamageStringForUI(clusterDamage150));

        }

    }
}

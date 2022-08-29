using BattleTech;
using CBTBehaviorsEnhanced;
using IRBTModUtils.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBETests
{
    [TestClass]
    public static class TestGlobalInit
    {
        [AssemblyInitialize]
        public static void TestInitialize(TestContext testContext)
        {
            Mod.Log = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests", "CBTBETEST", true, true);
            Mod.MeleeLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_melee", "CBTBETEST", true, true);

            Mod.Config = new ModConfig();

            // Needed for a few pilot check tests
            IRBTModUtils.Mod.Log = new DeferringLogger(testContext.TestResultsDirectory,
                "IRBTModUtils", "IRBTMU", true, true);

        }

        [AssemblyCleanup]
        public static void TearDown()
        {

        }
    }
}

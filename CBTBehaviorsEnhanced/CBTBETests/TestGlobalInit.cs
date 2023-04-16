using CBTBehaviorsEnhanced;
using CustomUnits;
using IRBTModUtils.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CBTBETests
{
    [TestClass]
    public static class GlobalInitialize
    {
        [AssemblyInitialize]
        public static void TestInitialize(TestContext testContext)
        {
            Mod.Log = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests", "CBTBETEST", true, true);

            Mod.HeatLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_heat", "CBTBETEST", true, true);

            Mod.MeleeLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_melee", "CBTBETEST", true, true);
            Mod.MeleeDamageLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_melee_damage", "CBTBETEST", true, true);

            Mod.ActivationLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_activation", "CBTBETEST", true, true);
            Mod.HullBreachLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_hull_breach", "CBTBETEST", true, true);

            Mod.MoveLog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_move", "CBTBETEST", true, true);

            Mod.AILog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_ailog", "CBTBETEST", true, true);
            Mod.UILog = new DeferringLogger(testContext.TestResultsDirectory,
                "CBTBE_tests_uilog", "CBTBETEST", true, true);

            Mod.Config = new ModConfig();

            // Needed for a few pilot check tests
            IRBTModUtils.Mod.Log = new DeferringLogger(testContext.TestResultsDirectory,
                "IRBTModUtils", "IRBTMU", true, true);

            CUSettings testCUSettings = new CustomUnits.CUSettings();
            testCUSettings.PartialMovementOnlyWalkByDefault = false;
            testCUSettings.AllowRotateWhileJumpByDefault = false;
            CustomUnits.Core.Settings = testCUSettings;

            Console.WriteLine("AssemblyInitialize complete");

        }

        [AssemblyCleanup]
        public static void TearDown()
        {

        }
    }
}

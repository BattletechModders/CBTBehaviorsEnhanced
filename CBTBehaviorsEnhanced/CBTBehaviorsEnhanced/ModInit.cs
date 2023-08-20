using CBTBehaviorsEnhanced.Move;
using CBTBehaviorsEnhanced.Patches.AI.InfluenceMap;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CBTBehaviorsEnhanced
{
    public static class Mod
    {

        public const string HarmonyPackage = "us.frostraptor.CBTBehaviorsEnhanced";
        public const string LogName = "cbt_behaviors_enhanced";
        public const string LogLabel = "CBTBE";

        public static DeferringLogger Log;
        public static DeferringLogger HeatLog;
        public static DeferringLogger MeleeLog;
        public static DeferringLogger MeleeDamageLog;
        public static DeferringLogger ActivationLog;
        public static DeferringLogger HullBreachLog;
        public static DeferringLogger MoveLog;
        public static DeferringLogger AILog;
        public static DeferringLogger UILog;

        public static string ModDir;
        public static ModConfig Config;
        public static ModText LocalizedText;

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON)
        {
            ModDir = modDirectory;

            // Read the config
            Exception settingsE = null;
            try
            {
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            }
            catch (Exception e)
            {
                settingsE = e;
                Mod.Config = new ModConfig();
            }
            Mod.Config.InitUnsetValues();

            Log = new DeferringLogger(modDirectory, LogName, LogLabel, Config.Debug, Config.Trace);
            HeatLog = new DeferringLogger(modDirectory, LogName + "_heat", LogLabel, Config.Debug, Config.Trace);
            MeleeLog = new DeferringLogger(modDirectory, LogName + "_melee", LogLabel, Config.Debug, Config.Trace);
            MeleeDamageLog = new DeferringLogger(modDirectory, LogName + "_meleedamage", LogLabel, Config.Debug, Config.Trace);
            ActivationLog = new DeferringLogger(modDirectory, LogName + "_activation", LogLabel, Config.Debug, Config.Trace);
            HullBreachLog = new DeferringLogger(modDirectory, LogName + "_hullbreach", LogLabel, Config.Debug, Config.Trace);
            AILog = new DeferringLogger(modDirectory, LogName + "_ai", LogLabel, Config.Debug, Config.Trace);
            MoveLog = new DeferringLogger(modDirectory, LogName + "_move", LogLabel, Config.Debug, Config.Trace);
            UILog = new DeferringLogger(modDirectory, LogName + "_ui", LogLabel, Config.Debug, Config.Trace);

            // Read localization
            string localizationPath = Path.Combine(ModDir, "./mod_localized_text.json");
            try
            {
                string jsonS = File.ReadAllText(localizationPath);
                Mod.LocalizedText = JsonConvert.DeserializeObject<ModText>(jsonS);
            }
            catch (Exception e)
            {
                Mod.LocalizedText = new ModText();
                Log.Error?.Write(e, $"Failed to read localizations from: {localizationPath} due to error!");
            }
            Mod.LocalizedText.InitUnsetValues();

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Log.Info?.Write($"File version: {fvi.FileVersion}");

            Log.Debug?.Write($"ModDir is:{modDirectory}");
            Log.Debug?.Write($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();
            Mod.LocalizedText.LogConfig();

            if (settingsE != null)
            {
                Log.Info?.Write($"ERROR reading settings file! Error was: {settingsE}");
            }
            else
            {
                Log.Info?.Write($"INFO: No errors reading settings file.");
            }

            // Initialize custom components
            CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }

        public static void FinishedLoading(List<string> loadOrder)
        {
            Mod.Log.Info?.Write("Invoking FinishedLoading");

            // Check for RolePlayer and use it's BehaviorVar link instead
            //InitRoleplayerLink();

            foreach (string name in loadOrder)
            {
                if (name.Equals("MechEngineer"))
                {
                    Mod.Log.Info?.Write($"Enabling MechEngineer functionality");
                    ModState.MEIsLoaded = true;
                }

                if (name.Equals("IRBTModUtils", StringComparison.InvariantCultureIgnoreCase))
                {
                    Mod.Log.Info?.Write($"Initializing IRBTModUtils movement feature modifiers.");
                    List<IRBTModUtilMoveModifier> moveMods = new List<IRBTModUtilMoveModifier>()
                    {
                        new TTReset_MoveModifier(),
                        new CBTBE_RunMultiMod_MoveModifier(),
                        new Heat_MoveModifier(),
                        new Legged_MoveModifier()
                    };
                    foreach (IRBTModUtilMoveModifier moveMod in moveMods)
                    {
                        IRBTModUtils.Feature.MovementFeature.RegisterMoveDistanceModifier(moveMod.Name, moveMod.Priority, moveMod.WalkMod, moveMod.RunMod);
                    }
                }

                if (name.Equals("CleverGirl"))
                {
                    Mod.Log.Info?.Write($"Initializing CleverGirl extensions");
                    List<CustomInfluenceMapPositionFactor> customPositionFactors = new List<CustomInfluenceMapPositionFactor>()
                    {
                        new PreferAvoidMeleeWhenOutTonned(),
                        new PreferStationaryWithMeleeWeapon()

                    };
                    CustomFactors.Register("CBTBE", customPositionFactors);
                    // no ally factors to register
                    // no hostile factors to register
                }

                if (name.Equals("RolePlayer", StringComparison.InvariantCultureIgnoreCase))
                {
                    InitRoleplayerLink();
                }

            }
        }

        private static void InitRoleplayerLink()
        {
            try
            {
                Mod.Log.Info?.Write(" -- Checking for RolePlayer and MechEngineer Integration -- ");
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    Mod.Log.Trace?.Write($"  -- found assembly : {assembly.FullName}");
                    if (assembly.FullName.StartsWith("RolePlayer"))
                    {
                        // Find the manager and pull it's singleton instance
                        Type managerType = assembly.GetType("RolePlayer.BehaviorVariableManager");
                        if (managerType == null)
                        {
                            Mod.Log.Warn?.Write("  Failed to find RolePlayer.BehaviorVariableManager.getBehaviourVariable - RP behavior variables will be ignored!");
                            continue;
                        }

                        PropertyInfo instancePropertyType = managerType.GetProperty("Instance");
                        ModState.RolePlayerBehaviorVarManager = instancePropertyType.GetValue(null);
                        if (ModState.RolePlayerBehaviorVarManager == null)
                        {
                            Mod.Log.Warn?.Write("  Failed to get RolePlayer.BehaviorVariableManager instance!");
                            continue;
                        }

                        // Find the method
                        ModState.RolePlayerGetBehaviorVar = managerType.GetMethod("getBehaviourVariable", new Type[] { typeof(AbstractActor), typeof(BehaviorVariableName) });

                        if (ModState.RolePlayerGetBehaviorVar != null)
                            Mod.Log.Info?.Write("  Successfully linked with RolePlayer");
                        else
                            Mod.Log.Warn?.Write("  Failed to find RolePlayer.BehaviorVariableManager.getBehaviourVariable - RP behavior variables will be ignored!");

                    }
                }
                Mod.Log.Info?.Write(" -- Done");
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Error trying to find RolePlayer and ME types!");
            }
        }
    }
}

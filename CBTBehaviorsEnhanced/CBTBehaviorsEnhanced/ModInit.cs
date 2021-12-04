using BattleTech;
using CBTBehaviorsEnhanced.CAC;
using CustAmmoCategories;
using Harmony;
using IRBTModUtils.Logging;
using Newtonsoft.Json;
using System;
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
        public static DeferringLogger ActivationLog;
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
            ActivationLog = new DeferringLogger(modDirectory, LogName + "_activation", LogLabel, Config.Debug, Config.Trace);
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
            Log.Info?.Write($"Assembly version: {fvi.ProductVersion}");

            Log.Debug?.Write($"ModDir is:{modDirectory}");
            Log.Debug?.Write($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

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

            //HarmonyInstance.DEBUG = true;
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void FinishedLoading()
        {
            // Check for RolePlayer and use it's BehaviorVar link instead
            InitRoleplayerLink();
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
                    else if (assembly.FullName.StartsWith("MechEngineer"))
                    {
                        // Find the ComponentExplosion type 
                        Type explosionType = assembly.GetType("MechEngineer.Features.ComponentExplosions.ComponentExplosion");
                        if (explosionType != null)
                        {
                            ModState.MEIsLoaded = true;
                            Mod.Log.Info?.Write("  Successfully linked with MechEngineer");
                        }
                    }
                }
                Mod.Log.Info?.Write(" -- Done");
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Error trying to find RolePlayer and ME types!");
            }

            if (ModState.MEIsLoaded == false)
            {
                Mod.Log.Warn?.Write("Failed to link with MechEngineer, skipping ME component explosions");
            }            
        }
    }
}

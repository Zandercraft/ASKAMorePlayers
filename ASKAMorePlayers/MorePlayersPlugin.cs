using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Fusion;
using Il2CppSystem;
using SSSGame;
using SSSGame.Network;

namespace ASKAMorePlayers
{
    /// <summary>
    /// The main BepInEx plugin class for the More Players mod.
    /// Initializes the configuration and applies Harmony patches.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class MorePlayersPlugin : BasePlugin
    {
        /// <summary>
        /// A static instance of the plugin, allowing access to its members from patches.
        /// </summary>
        public static MorePlayersPlugin Instance { get; private set; }
        
        /// <summary>
        /// The configuration entry for setting the maximum number of players.
        /// </summary>
        public static ConfigEntry<int> MaxPlayersConfig;

        /// <summary>
        /// The entry point of the plugin, called by BepInEx during game startup.
        /// </summary>
        public override void Load()
        {
            Instance = this;
            
            // Binds the configuration setting to a file.
            MaxPlayersConfig = Config.Bind("General", "MaxPlayers", 10, "The desired maximum number of players for the server.");

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loaded. Desired Max Players: {MaxPlayersConfig.Value}");

            // Creates a new Harmony instance and patches all annotated methods in the assembly.
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }
    }

    /// <summary>
    /// Patches the <see cref="NetworkRunner.StartGame"/> method to increase the session's player limit.
    /// </summary>
    [HarmonyPatch(typeof(NetworkRunner), nameof(NetworkRunner.StartGame))]
    public static class PlayerCountPatch
    {
        private static int GetMaxPlayersFromConfigFile()
        {
            const int defaultValue = 10;
            try
            {
                var configPath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");
                if (!File.Exists(configPath))
                {
                    MorePlayersPlugin.Instance.Log.LogWarning($"Config file not found at {configPath}. Using default value: {defaultValue}");
                    return defaultValue;
                }

                var line = File.ReadLines(configPath).FirstOrDefault(l => l.Trim().StartsWith("MaxPlayers"));
                if (line == null)
                {
                    MorePlayersPlugin.Instance.Log.LogWarning($"'MaxPlayers' not found in config file. Using default value: {defaultValue}");
                    return defaultValue;
                }

                var valueString = line.Split('=').Last().Trim();
                if (int.TryParse(valueString, out var parsedValue))
                {
                    return parsedValue;
                }
                
                MorePlayersPlugin.Instance.Log.LogWarning($"Failed to parse '{valueString}' as an integer. Using default value: {defaultValue}");
                return defaultValue;
            }
            catch (System.Exception e)
            {
                MorePlayersPlugin.Instance.Log.LogError($"Error reading config file manually: {e}. Using default value: {defaultValue}");
                return defaultValue;
            }
        }

        /// <summary>
        /// A Harmony prefix patch that modifies the <see cref="StartGameArgs"/> before the original method is executed.
        /// </summary>
        /// <param name="args">The arguments for the StartGame method, passed by reference.</param>
        [HarmonyPrefix]
        public static void Prefix(ref StartGameArgs args)
        {
            // Manually parse the config file to win the race condition on server startup.
            var maxPlayers = GetMaxPlayersFromConfigFile();
            
            // The PlayerCount property includes the host. The server browser displays client slots.
            // We add 1 to our desired value to ensure the correct number of client slots are shown.
            var sessionCapacity = maxPlayers + 1;

            args.PlayerCount = new Nullable<int>(sessionCapacity);
            MorePlayersPlugin.Instance.Log.LogInfo($"Patching StartGame. Setting session capacity to {sessionCapacity} ({maxPlayers} clients + 1 host).");
        }
    }

    /// <summary>
    /// Patches the <see cref="PlayerManager.MaxPlayerCount"/> property to return the true configured max player count.
    /// This is critical for the server's internal logic.
    /// </summary>
    [HarmonyPatch(typeof(PlayerManager), "get_MaxPlayerCount")]
    public static class PlayerManager_MaxPlayerCount_Patch
    {
        /// <summary>
        /// A Harmony postfix patch that modifies the return value of the original method.
        /// </summary>
        /// <param name="__result">The original return value, passed by reference. Harmony requires this specific name.</param>
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref int __result)
        {
            __result = MorePlayersPlugin.MaxPlayersConfig.Value;
        }
    }

    /// <summary>
    /// Patches the <see cref="NetworkCuller.Spawned"/> method to update its internal player count.
    /// </summary>
    [HarmonyPatch(typeof(NetworkCuller), nameof(NetworkCuller.Spawned))]
    public static class NetworkCuller_Spawned_Patch
    {
        /// <summary>
        /// A Harmony postfix patch that runs after the original <see cref="NetworkCuller.Spawned"/> method.
        /// </summary>
        /// <param name="__instance">The instance of the NetworkCuller class. Harmony requires this specific name.</param>
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(NetworkCuller __instance)
        {
            MorePlayersPlugin.Instance.Log.LogInfo($"Patching NetworkCuller _playerMaxCount from {__instance._playerMaxCount} to {MorePlayersPlugin.MaxPlayersConfig.Value}");
            __instance._playerMaxCount = MorePlayersPlugin.MaxPlayersConfig.Value;
        }
    }
    
    /// <summary>
    /// Patches the <see cref="NetworkHittable.Spawned"/> method to update its internal player count.
    /// </summary>
    [HarmonyPatch(typeof(NetworkHittable), nameof(NetworkHittable.Spawned))]
    public static class NetworkHittable_Spawned_Patch
    {
        /// <summary>
        /// A Harmony postfix patch that runs after the original <see cref="NetworkHittable.Spawned"/> method.
        /// </summary>
        /// <param name="__instance">The instance of the NetworkHittable class. Harmony requires this specific name.</param>
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(NetworkHittable __instance)
        {
            MorePlayersPlugin.Instance.Log.LogInfo($"Patching NetworkHittable _playerMaxCount from {__instance._playerMaxCount} to {MorePlayersPlugin.MaxPlayersConfig.Value}");
            __instance._playerMaxCount = MorePlayersPlugin.MaxPlayersConfig.Value;
        }
    }

    /// <summary>
    /// Contains constant string values for the plugin's metadata.
    /// </summary>
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ca.zandercraft.askamoreplayers";
        public const string PLUGIN_NAME = "ASKA More Players";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
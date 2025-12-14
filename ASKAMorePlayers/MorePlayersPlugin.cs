using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ASKAMorePlayers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ASKAMorePlayersPlugin : BasePlugin
    {

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo(PluginInfo.PLUGIN_NAME + " is loaded!");

            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "ca.zandercraft.askamoreplayers";
        public const string PLUGIN_NAME = "ASKA More Players";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
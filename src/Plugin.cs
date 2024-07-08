using BepInEx;

namespace WOLAP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "lucasvdm.westofloathing.aprandomizer";
        internal const string PluginName = "West of Loathing Archipelago Randomizer";
        internal const string PluginVersion = "0.1.0";

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}

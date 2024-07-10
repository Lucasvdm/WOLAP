using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace WOLAP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class WolapPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "lucasvdm.westofloathing.aprandomizer";
        internal const string PluginName = "West of Loathing Archipelago Randomizer";
        internal const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log; //For access from other classes

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin {PluginGuid} is loaded!");
            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

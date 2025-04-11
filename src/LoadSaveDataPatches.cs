using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;

namespace WOLAP
{
    [HarmonyPatch]
    internal class LoadSaveDataPatches
    {
        [HarmonyPatch(typeof(SavedGame), "NewGame")]
        [HarmonyPostfix]
        private static void NewGamePatch(string strNameFirst, string strNameLast, string strNameFull, bool fIsCowgirl, int meatReward, string strAnimal, ref string __result)
        {
            WolapPlugin.Log.LogInfo("Created new modded Archipelago save file.");
            MPlayer.instance.AddProperty(Constants.ModdedSaveProperty, "1");

            Traverse traverse = Traverse.Create(typeof(SavedGame));
            traverse = traverse.Method("SaveInternal", [typeof(string), typeof(string)]);
            __result = traverse.GetValue<string>(["create_character", null]);
        }

        //Patching handled in WolapPlugin rather than with attributes because of LoadInternal's weird signature (overloaded with a private enum parameter type)
        private static void LoadInternalPatch(string strLoadName, object lflags, ref string strWaaTeleport)
        {
            if (WestOfLoathing.instance.state_machine.IsState(GameplayState.NAME))
            {
                ProcessSaveLoaded();
            }
        }

        private static void ProcessSaveLoaded()
        {
            bool isModdedSave = MPlayer.instance.data.TryGetValue(Constants.ModdedSaveProperty, out _);
            WolapPlugin.Log.LogInfo((isModdedSave ? "Modded" : "Unmodded") + " save loaded.");

            var trackingLogMethod = ((Action<string, int>)Tracking.Log).Method;
            var trackingLogPrefix = ((Func<string, int, bool>)DisableTrackingCommandWebCallsPatch).Method;

            if (isModdedSave)
            {
                if (!WolapPlugin.ModDataLoaded)
                {
                    LoadWOLAPData();

                    WolapPlugin.Harmony.Patch(trackingLogMethod, prefix: new HarmonyMethod(trackingLogPrefix));
                    WolapPlugin.Log.LogInfo("Patched Tracking.Log to disable web calls for remote event logging during modded play.");
                }

                //Forcing the inventory to be initialized once to allow for other related hacks
                WestOfLoathing.instance.state_machine.Push(new InventoryState());
                WestOfLoathing.instance.state_machine.Pop(InventoryState.NAME);

                //Loading the basic archipelago item icon once through Inventory's SpriteForItem as a hack to force it to initialize (without this, calling SpriteLoader.SpriteForName for the icon crashes the game)
                //TODO: Figure out why this is needed.  It seems like it should just call SpriteForName internally, I don't know why this fixes anything.
                Traverse invTraverse = Traverse.Create(Resources.FindObjectsOfTypeAll<Inventory>().First());
                invTraverse = invTraverse.Method("SpriteForItem", [typeof(MItem), typeof(Sprite)]);
                invTraverse.GetValue([ModelManager.GetItem("archipelago_shopitem"), null]);

                if (WolapPlugin.Archipelago.IsConnected) WolapPlugin.Archipelago.AddMissingInitialChecksToShops();

                WolapPlugin.Archipelago.SlotDataFlagsSet = false; //Will set slot data flags on next update
            }
            else
            {
                if (WolapPlugin.Archipelago.IsConnected) WolapPlugin.Archipelago.Disconnect();

                if (WolapPlugin.ModDataLoaded)
                {
                    ModelLoader.Instance.LoadLocal();
                    WolapPlugin.ModDataLoaded = false;
                    WolapPlugin.Log.LogInfo("Reloaded original unmodded JSON data.");

                    WolapPlugin.Harmony.Unpatch(trackingLogMethod, trackingLogPrefix);
                    WolapPlugin.Log.LogInfo("Unpatched Tracking.Log to re-enable web calls for remote event logging during unmodded play.");
                }
            }
        }

        private static async void LoadWOLAPData()
        {
            byte[] fileData;
            using (var dataStream = typeof(WolapPlugin).Assembly.GetManifestResourceStream("WOLAP.src.custom_data.json"))
            {
                fileData = new byte[dataStream.Length];
                await dataStream.ReadAsync(fileData, 0, (int)dataStream.Length);
            }

            if (fileData == null || fileData.Length == 0) WolapPlugin.Log.LogError("Mod data file failed to load!");
            else
            {
                string fileText = Encoding.UTF8.GetString(fileData);
                WolapPlugin.Log.LogInfo("Parsing custom JSON data...");
                ModelManager.Instance.Parse(fileText, true);
                WolapPlugin.ModDataLoaded = true;
                WolapPlugin.Log.LogInfo("Custom JSON data parsed and merged.");
            }
        }

        [HarmonyPatch(typeof(ModelManager), "ParseScripts")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ParseScriptsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var insts = new List<CodeInstruction>(instructions);

            //Potentially brittle search conditions, but I doubt this game's code will ever be updated significantly, and compatibility with other mods is not a priority for this project
            int ifOverwriteIdx = insts.FindIndex(inst => inst.Is(OpCodes.Ldfld, m_fOverwriteOk));
            if (ifOverwriteIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the m_fOverwriteOk check.");
                return instructions;
            }

            int loadTextIdx = insts.FindIndex(ifOverwriteIdx, inst => inst.opcode == OpCodes.Ldloc_1);
            if (loadTextIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the ldloc.1 load.");
                return instructions;
            }

            insts.RemoveAt(loadTextIdx); //Remove load for local variable "text", don't need it
            int assignmentIdx = insts.FindIndex(loadTextIdx, inst => inst.opcode == OpCodes.Callvirt);
            if (assignmentIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the set_Item callvirt.");
                return instructions;
            }

            insts.RemoveAt(assignmentIdx); //Remove call to set_Item assignment (this.scripts[text] = mscript)

            //Insert instruction to call InjectedHelpers.OverwriteScriptStates(this.scripts, mscript);
            insts.Insert(assignmentIdx, new CodeInstruction(OpCodes.Call, m_OverwriteScriptStates));

            return insts;
        }

        [HarmonyPatch(typeof(ModelManager), "ParseWaas")]
        [HarmonyPrefix]
        static bool ParseWaasPatch(ModelManager __instance, JSONObject jsWaas)
        {
            if (jsWaas == null || jsWaas.type != JSONObject.Type.OBJECT) return false;

            bool m_fOverwriteOk = new Traverse(__instance).Field("m_fOverwriteOk").GetValue<bool>();
            foreach (string text in jsWaas.keys)
            {
                JSONObject jsObject = jsWaas[text];
                MWaa mwaa = new MWaa(text, jsObject);
                if (m_fOverwriteOk)
                {
                    if (!ShouldLoadDataWithID(text)) continue;

                    __instance.waas[text] = mwaa;
                }
                else if (!__instance.waas.ContainsKey(text))
                {
                    __instance.waas.Add(text, mwaa);
                }
            }

            return false;
        }

        public static bool ShouldLoadDataWithID(string id)
        {
            //Only load modded DLC data objects if the DLC is enabled in the Archipelago options
            //This object -> string -> int -> boolean conversion is gross, but necessary
            return !(WolapPlugin.Archipelago.SlotData.TryGetValue(Constants.DlcEnabledSlotDataFlag, out object dlcEnabled) && !Convert.ToBoolean(int.Parse(dlcEnabled.ToString())) && id.StartsWith("house_"));
        }

        //Disable web calls to stats.westofloathing.com for remote logging events -- only applies to modded saves, patch is manually applied/removed on save load
        private static bool DisableTrackingCommandWebCallsPatch(string strFlag, int nValue)
        {
            return false;
        }

        static MethodInfo m_OverwriteScriptStates = SymbolExtensions.GetMethodInfo(() => InjectedHelpers.OverwriteScriptStates(null, null));
        static FieldInfo m_fOverwriteOk = typeof(ModelManager).GetField("m_fOverwriteOk", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;

namespace WOLAP
{
    [HarmonyPatch(typeof(ModelManager))]
    internal class ModelManagerPatches
    {
        static MethodInfo m_OverwriteScriptStates = SymbolExtensions.GetMethodInfo(() => InjectedHelpers.OverwriteScriptStates(null, null));
        static FieldInfo m_fOverwriteOk = typeof(ModelManager).GetField("m_fOverwriteOk", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch("ParseScripts")]
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

            //Insert instruction to call ModelManagerPatches.OverwriteScriptStates(this.scripts, mscript);
            insts.Insert(assignmentIdx, new CodeInstruction(OpCodes.Call, m_OverwriteScriptStates));

            return insts;
        }
    }
}

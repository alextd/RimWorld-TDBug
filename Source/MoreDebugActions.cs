using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu), "DoListingItems_MapActions")]
	public static class MoreDebugActions_MapActions
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			MoreDebugActions.Transpiler(instructions);
	}
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu), "DoListingItems_MapTools")]
	public static class MoreDebugActions
	{
		public struct DA { public string label; public string action; }
		public static Dictionary<string, DA> insertAfter = new Dictionary<string, DA>() {
			{ "T: Try place near stacks of 75...", new DA() {label = "T: Try place near full stacks...", action = "fullStackAction"} },
			{ "Destroy all things", new DA() {label = "Destroy all selected", action = "destroySelectedAction"} }
		};
		
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DebugActionInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");

			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count; i++)
			{
				CodeInstruction inst = instList[i];

				yield return inst;

				if (inst.opcode == OpCodes.Call && inst.operand == DebugActionInfo)
				{
					if (instList[i - 8].opcode == OpCodes.Ldstr && instList[i - 8].operand is string afterLabel)
					{
						foreach (var kvp in insertAfter)
						{
							if (kvp.Key == afterLabel)
							{
								yield return new CodeInstruction(OpCodes.Ldarg_0);//this 
								yield return new CodeInstruction(OpCodes.Ldstr, kvp.Value.label);//string
								//delegates are easier when you have a compiler, but for these purposes use a static Action field and provide the name here.
								yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreDebugActions), kvp.Value.action));//Action
								yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MoreDebugActions), nameof(DebugAction)));//this.DebugAction(string, Action)
							}
						}
					}
				}
			}
		}

		public static MethodInfo DebugInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");
		public static void DebugAction(Dialog_DebugOptionLister dialog, string label, Action action) =>
			DebugInfo.Invoke(dialog, new object[] { label, action });

		public static Action fullStackAction = delegate
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs
				.Where(def => DebugThingPlaceHelper.IsDebugSpawnable(def, false) && def.stackLimit >= 2))
			{
				ThingDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, false);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		public static Action destroySelectedAction = delegate
		{
			foreach (Thing current in Find.Selector.SelectedObjectsListForReading.Where(s => s is Thing).ToList())
			{
				current.Destroy(DestroyMode.Vanish);
			}
		};
	}
}

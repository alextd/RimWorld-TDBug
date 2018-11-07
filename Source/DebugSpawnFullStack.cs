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
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu), "DoListingItems_MapTools")]
	public static class DebugSpawnFullStack
	{
		//base.DoLabel("Tools - Spawning");
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DebugActionInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");

			MethodInfo InsertFullStackAfterInfo = AccessTools.Method(typeof(DebugSpawnFullStack), nameof(InsertFullStackAfter));

			bool after75 = false;
			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Ldstr && (i.operand as string) == "T: Try place near stacks of 75...")
					after75 = true;

				if(after75 && i.opcode == OpCodes.Call && i.operand == DebugActionInfo)
				{
					after75 = false;
					yield return new CodeInstruction(OpCodes.Call, InsertFullStackAfterInfo);
				}
				else
					yield return i;
			}
		}
		
		public static MethodInfo DebugInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");
		public static bool DebugAction(Dialog_DebugOptionLister dialog, string label, Action action)
		{
			return (bool)DebugInfo.Invoke(dialog, new object[] {label, action});
		}

		public static bool InsertFullStackAfter(Dialog_DebugOptionLister dialog, string label, Action action)
		{
			bool result = DebugAction(dialog, label, action);

			DebugAction(dialog, "T: Try place near full stacks...", delegate
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
			});
			return result;
		}
	}
}

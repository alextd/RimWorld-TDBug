using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Harmony;
using RimWorld;

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
		public struct DA { public string label; public string action; public string tool; } //Tool debug DebugAction, 
		public static Dictionary<string, DA> insertAfter = new Dictionary<string, DA>() {
			{ "T: Try place near stacks of 75...", new DA() {label = "T: Try place near full stacks...", action = "fullStackAction"} },
			{ "Destroy all things", new DA() {label = "Destroy all selected", action = "destroySelectedAction"} },
			{ "T: Heal random injury (10)", new DA() {label = "T: Full Heal", action = "healFullAction", tool="DebugToolMapForPawns" } },
			{ "T: Make roof", new DA() {label = "T: Make roof (by def)", action = "makeRoofByDef"} },
			{ "T: Damage apparel", new DA() {label = "T: Add selected things to inventory", action = "addSeltoInv", tool="DebugToolMapForPawns"} },
			{ "T: Joy -20%", new DA() {label = "T: Need -20%", action = "addNeed"} },
			{ "T: Chemical -20%", new DA() {label = "Fulfill all needs", action = "fulfillAllNeeds"} },
			{ "T: Delete roof", new DA() {label = "T: Set Deep Resource", action = "addDeepResource"} }
		};
		
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//check after either
			MethodInfo DebugActionInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");
			MethodInfo DebugToolMapInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugToolMap");
			MethodInfo DebugToolMapForPawnsInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugToolMapForPawns");

			List<CodeInstruction> instList = instructions.ToList();
			string lastString = "";
			for (int i = 0; i < instList.Count; i++)
			{
				CodeInstruction inst = instList[i];

				yield return inst;

				if (inst.opcode == OpCodes.Ldstr && inst.operand is string afterLabel)
					lastString = afterLabel;
				if (inst.opcode == OpCodes.Call && 
					(inst.operand == DebugActionInfo || inst.operand == DebugToolMapInfo || inst.operand == DebugToolMapForPawnsInfo))
				{
					foreach (var kvp in insertAfter)
					{
						if (kvp.Key == lastString)
						{
							string tool = kvp.Value.tool ?? nameof(DebugAction);
							yield return new CodeInstruction(OpCodes.Ldarg_0);//this 
							yield return new CodeInstruction(OpCodes.Ldstr, kvp.Value.label);//string
							//delegates are easier when you have a compiler, but for these purposes use a static Action field and provide the name here.
							yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MoreDebugActions), kvp.Value.action));//Action
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MoreDebugActions), tool));//this.DebugAction(string, Action)
						}
					}
				}
			}
		}

		// static Dialog_DebugOptionLister so delegate can use it
		public static Dialog_DebugOptionLister dlg;

		// What 'tool' above refers to:
		public static MethodInfo DebugInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugAction");
		public static void DebugAction(Dialog_DebugOptionLister dialog, string label, Action action)
		{
			dlg = dialog;
			DebugInfo.Invoke(dialog, new object[] { label, action });
		}

		public static MethodInfo DebugToolMapForPawnsInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugToolMapForPawns");
		public static void DebugToolMapForPawns(Dialog_DebugOptionLister dialog, string label, Action<Pawn> action)
		{
			dlg = dialog;
			DebugToolMapForPawnsInfo.Invoke(dialog, new object[] { label, action });
		}

		public static MethodInfo DebugToolMapInfo = AccessTools.Method(typeof(Dialog_DebugOptionLister), "DebugToolMap");
		public static void DebugToolMap(Dialog_DebugOptionLister dialog, string label, Action action)
		{
			dlg = dialog;
			DebugToolMapInfo.Invoke(dialog, new object[] { label, action });
		}

		//what 'action' above refers to
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
		public static Action<Pawn> healFullAction = delegate (Pawn p)
		{
			
			foreach(Hediff_Injury hediff_Injury in (from x in p.health.hediffSet.GetHediffs<Hediff_Injury>()
					 where x.CanHealNaturally() || x.CanHealFromTending()
					 select x))
			{
				hediff_Injury.Heal(10000f);//probably enough

			}
		};
		public static Action makeRoofByDef = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (RoofDef current in DefDatabase<RoofDef>.AllDefs)
			{
				RoofDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					CellRect.CellRectIterator iterator = CellRect.CenteredOn(UI.MouseCell(), 1).GetIterator();
					while (!iterator.Done())
					{
						Find.CurrentMap.roofGrid.SetRoof(iterator.Current, localDef);
						iterator.MoveNext();
					}
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		public static Action<Pawn> addSeltoInv = delegate (Pawn p)
		{
			foreach (Thing t in Find.Selector.SelectedObjectsListForReading.
				Where(o => o is Thing t && t.def.EverHaulable).Cast<Thing>().ToList())//ToList to copy since SelectedObjects changed when despawned
			{
				p.inventory.GetDirectlyHeldThings().TryAdd(t.SplitOff(t.stackCount));
			}
		};

		//private void OffsetNeed(NeedDef nd, float offsetPct)
		public static MethodInfo OffsetNeedInfo = AccessTools.Method(typeof(Dialog_DebugActionsMenu), "OffsetNeed");
		public static Action addNeed = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();

			list.Add(new DebugMenuOption("All Needs", DebugMenuOptionMode.Tool, delegate
			{
				foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
					OffsetNeedInfo.Invoke(dlg, new object[] { current, -0.2f });
			}));

			foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
			{
				NeedDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					OffsetNeedInfo.Invoke(dlg, new object[] { localDef, -0.2f });
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		public static Action fulfillAllNeeds = delegate ()
		{
			foreach(Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
			{
				foreach(Need need in pawn.needs.AllNeeds)
				{
					need.CurLevelPercentage = 1f;
				}
			}
		};
		public static Action addDeepResource = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			for (int size = 0; size < 5; size++)
			{
				int localSize = size;
				list.Add(new DebugMenuOption($"{ size * 2 + 1 }x{ size * 2 + 1 }", DebugMenuOptionMode.Action, delegate
				{
					AddDeepResourceSize(localSize);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};

		public static void AddDeepResourceSize(int size)
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			list.Add(new DebugMenuOption($"Deplete deep resource", DebugMenuOptionMode.Tool, delegate
			{
				CellRect.CellRectIterator iterator = CellRect.CenteredOn(UI.MouseCell(), size).GetIterator();
				while (!iterator.Done())
				{
					Find.CurrentMap.deepResourceGrid.SetAt(iterator.Current, null, 0);
					iterator.MoveNext();
				}
			}));
			foreach (var current in DefDatabase<ThingDef>.AllDefs.Where(def => def.deepCommonality > 0))
			{
				ThingDef localDef = current;
				list.Add(new DebugMenuOption($"Spawn deep resource: {localDef.LabelCap}", DebugMenuOptionMode.Tool, delegate
				{
					CellRect.CellRectIterator iterator = CellRect.CenteredOn(UI.MouseCell(), size).GetIterator();
					while (!iterator.Done())
					{
						Find.CurrentMap.deepResourceGrid.SetAt(iterator.Current, localDef, localDef.deepCountPerCell);
						iterator.MoveNext();
					}
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}
	}
}

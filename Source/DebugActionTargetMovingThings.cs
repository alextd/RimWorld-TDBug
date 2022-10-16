using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using HarmonyLib;

namespace TDBug
{
	[StaticConstructorOnStartup]
	public static class DebugActionTargetMovingThings
	{
		//transpile DebugActionMapForPawns,
		//(and all the various DebugActions and their delegates)

		//to find:
		//foreach (Thing item in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).ToList())
		//replace with:
		//GenUI.ThingsUnderMouse(UI.MouseMapPosition(), 1f, targetingParameters);
		//ThingsUnderMouse just calls ThingsAt anyway -- but also checks for moving pawns

		//This transpiler will be called a lot so save ReflectionInfos once statically. TODO: Clean after load , no big deal though
		static readonly MethodInfo CurrentMapInfo = AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap));
		static readonly FieldInfo thingGridInfo = AccessTools.Field(typeof(Map), nameof(Map.thingGrid));
		static readonly MethodInfo MouseCellInfo = AccessTools.Method(typeof(UI), nameof(UI.MouseCell));
		static readonly MethodInfo ThingsAtInfo = AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.ThingsAt));

		static readonly MethodInfo MouseMapPositionInfo = AccessTools.Method(typeof(UI), nameof(UI.MouseMapPosition));
		static readonly FieldInfo allTargetingParamsInfo = AccessTools.Field(typeof(DebugActionTargetMovingThings), nameof(all));
		static readonly MethodInfo ThingsUnderMouseInfo = AccessTools.Method(typeof(GenUI), nameof(GenUI.ThingsUnderMouse));

		public readonly static TargetingParameters all = new TargetingParameters()
		{
			//public bool canTargetSelf;//not used, whatever.
			canTargetFires = true,//have to turn some on
			canTargetItems = true,
			canTargetPlants = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			//all other defaults are fine. Why is the default not "all things" I dunno.
		};


		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
		{
			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count; i++)
			{
				var inst = instList[i];

				if (inst.Calls(CurrentMapInfo) && i + 3 < instList.Count
					&& instList[i + 1].LoadsField(thingGridInfo)
					&& instList[i + 2].Calls(MouseCellInfo)
					&& instList[i + 3].Calls(ThingsAtInfo))
				{
					//What was
					//Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())

					inst.opcode = OpCodes.Call;//In case inst has labels, reuse it instead of new
					inst.operand = MouseMapPositionInfo;
					yield return inst;//UI.MouseMapPosition()
					yield return new CodeInstruction(OpCodes.Ldc_R4, 1.0f);//1.0f , like in Selector
					yield return new CodeInstruction(OpCodes.Ldsfld, allTargetingParamsInfo);//all
					yield return new CodeInstruction(OpCodes.Ldnull);//null (source)
					yield return new CodeInstruction(OpCodes.Call, ThingsUnderMouseInfo);//UI.ThingsUnderMouse()

					//is now
					//GenUI.ThingsUnderMouse(UI.MouseMapPosition(), 1.0f, all, source = null);

					i += 3;//Skip .thingGrid.ThingsAt(UI.MouseCell())
				}
				else
					yield return inst;
			}
		}

		static DebugActionTargetMovingThings()
		{
			Harmony harmony = new Harmony("Uuugggg.rimworld.TDBug.auto");
			HarmonyMethod transpilerMagic = new(typeof(DebugActionTargetMovingThings), nameof(Transpiler));

			//Okay let's find ALL USES of ThingsAt in ALL Debug action classes.
			//Find any methods nested in any these types or nested in their compiler-generated types
			List<Type> typesToMangle = new()
			{
				typeof(Dialog_DebugOptionLister),
				typeof(DebugActionsIdeo),
				//DebugActionsMapManagement Eh, Transfer doesn't need this
				typeof(DebugTools_Health),
				typeof(DebugToolsGeneral),
				//DebugToolsMisc Eh don't need attach fire
				typeof(DebugToolsPawns),
			};

			while (typesToMangle.Any())
			{
				Type type = typesToMangle.Pop();
				typesToMangle.AddRange(type.GetNestedTypes(BindingFlags.NonPublic));

				foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type).Where(NeedsTranspiler))
					harmony.Patch(method, transpiler: transpilerMagic);
			}
		}

		public static bool NeedsTranspiler(MethodInfo m)
		{
			List<CodeInstruction> instList = PatchProcessor.GetOriginalInstructions(m).ToList();
			for (int i = 0; i < instList.Count - 3; i++)
			{
				if (instList[i].Calls(CurrentMapInfo)
					&& instList[i + 1].LoadsField(thingGridInfo)
					&& instList[i + 2].Calls(MouseCellInfo)
					&& instList[i + 3].Calls(ThingsAtInfo))
					return true;
			}

			return false;
		}
	}
}

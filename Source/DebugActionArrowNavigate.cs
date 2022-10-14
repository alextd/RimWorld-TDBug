using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[DefOf]
	public static class TDKeyBindingDefOf
	{
		public static KeyBindingDef Dev_BackSelectedDebugAction;
	}

	// Include the back key to trigger the highlight index change
	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DoListingItems")]
	public static class DebugActionArrowNavigate
	{
		delegate void ChangeHighlightedOptionDel(Dialog_DebugOptionLister lister);
		static ChangeHighlightedOptionDel ChangeHighlightedOption =
			AccessTools.MethodDelegate<ChangeHighlightedOptionDel>(AccessTools.Method(typeof(Dialog_DebugOptionLister), "ChangeHighlightedOption"), virtualCall: true);

		public static void Postfix(Dialog_DebugOptionLister __instance)
		{
			Log.Message($"DoListingItems :: {TDKeyBindingDefOf.Dev_BackSelectedDebugAction}");
			if (TDKeyBindingDefOf.Dev_BackSelectedDebugAction.IsDownEvent)
			{
				Log.Message($"DoListingItems :: {TDKeyBindingDefOf.Dev_BackSelectedDebugAction} AYE");
				ChangeHighlightedOption(__instance);
			}
		}
	}

	// 1.4 replicated this chunk of code to a place that's not a simple method argggg
	[HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.DoWindowContents))]
	public static class DebugActionArrowNavigate_Dialog_Debug
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo IsDownEventInfo = AccessTools.PropertyGetter(typeof(KeyBindingDef), nameof(KeyBindingDef.IsDownEvent));

			foreach (var inst in instructions)
			{
				if(inst.Calls(IsDownEventInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DebugActionArrowNavigate_Dialog_Debug), nameof(IsDownEventOrOtherKeyIsDown)));
				}
				else yield return inst;
			}
		}

		public static bool IsDownEventOrOtherKeyIsDown(KeyBindingDef kDef)
		{
			return kDef.IsDownEvent || TDKeyBindingDefOf.Dev_BackSelectedDebugAction.IsDownEvent;
		}
	}

	// Ignore arrow event inside the textbox
	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.DoWindowContents))]
	public static class DontUseArrowEvent
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(Widgets), nameof(Widgets.TextField), new Type[] { typeof(Rect), typeof(string) }),
				AccessTools.Method(typeof(DontUseArrowEvent), nameof(TextFieldIfNoArrow)));

		//public static string TextField(Rect rect, string text)
		public static string TextFieldIfNoArrow(Rect rect, string text)
		{
			if (KeyBindingDefOf.Dev_ChangeSelectedDebugAction.KeyDownEvent ||
				TDKeyBindingDefOf.Dev_BackSelectedDebugAction.KeyDownEvent)
			{
				return text;  //roundabout to return text just to assign itself, but transpiling above would be tricker to remove all ilcode to set the field
			}

			//TextField will consume the event and set type to Used instead of KeyDown.
			//Plus, up/down just moves text input line and that's honestly annoying

			return Widgets.TextField(rect, text);
		}
	}

	// 1.4 added Dialog_Debug that does mostly the same as Dialog_OptionLister
	[HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.DoWindowContents))]
	public static class DontUseArrowEvent_Dialog_Debug
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			DontUseArrowEvent.Transpiler(instructions);
	}

	//Reverse order for TDKeyBindingDefOf.Dev_BackSelectedDebugAction.KeyDownEvent
	//protected override void ChangeHighlightedOption()
	[HarmonyPatch(typeof(Dialog_DebugOptionListLister), "ChangeHighlightedOption")]
	public static class ReverseChangeOrder
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//change
			//	(highlightedIndex + num + 1)
			//to 
			//  (highlightedIndex - num - 1)
			//if TDKeyBindingDefOf.Dev_BackSelectedDebugAction.KeyDownEvent

			//change

			//ldloc.0      // highlightedIndex
			//ldloc.1      // index1
			//add          
			//ldc.i4.1     // 1
			//add          

			//to

			//
			//ldloc.0      // highlightedIndex
			//ldloc.1      // index1
			//Call AddOrSubtract(highlightedIndex, index1)

			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count; i++)
			{
				var inst = instList[i];

				if (inst.opcode == OpCodes.Add && i + 2 < instList.Count
					&& instList[i + 2].opcode == OpCodes.Add)
				{
					inst.opcode = OpCodes.Call;
					inst.operand = AccessTools.Method(typeof(ReverseChangeOrder), nameof(AddOrSubstract));

					i += 2;// skip adding 1
				}
				else if (inst.opcode == OpCodes.Rem)
				{
					inst.opcode = OpCodes.Call;
					inst.operand = AccessTools.Method(typeof(ReverseChangeOrder), nameof(ModulusActually));
				}

				yield return inst;
			}
		}

		public static int AddOrSubstract(int a, int b)
		{
			//change
			//	(highlightedIndex + num + 1)
			//to 
			//  (highlightedIndex - num - 1)

			return TDKeyBindingDefOf.Dev_BackSelectedDebugAction.KeyDownEvent ?
				a - b - 1 : a + b + 1;
		}

		public static int ModulusActually(int a, int b) => ((a %= b) < 0) ? a + b : a;
	}

	// 1.4 replicated a lot of code across these Debug classes O_o
	[HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.DoWindowContents))]
	public static class ReverseChangeOrder_Dialog_Debug
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			=> ReverseChangeOrder.Transpiler(instructions);
	}
}

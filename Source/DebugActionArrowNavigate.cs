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
	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DoListingItems")]
	public static class DebugActionArrowNavigate
	{

		delegate void ChangeHighlightedOptionDel(Dialog_DebugOptionLister lister);
		static ChangeHighlightedOptionDel ChangeHighlightedOption =
			AccessTools.MethodDelegate<ChangeHighlightedOptionDel>(AccessTools.Method(typeof(Dialog_DebugOptionLister), "ChangeHighlightedOption"), virtualCall: true);

		public static void Postfix(Dialog_DebugOptionLister __instance)
		{
			//But don't set KeyBinding Dev_ChangeSelectedDebugAction to be up or down,cause then this would trigger twice.
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == UnityEngine.KeyCode.DownArrow)
			{
				ChangeHighlightedOption(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.DoWindowContents))]
	public static class DontUseArrowEvent
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(Widgets), nameof(Widgets.TextField), new Type[] {typeof(Rect) , typeof(string)}),
				AccessTools.Method(typeof(DontUseArrowEvent), nameof(TextFieldIfNoArrow)));

		//public static string TextField(Rect rect, string text)
		public static string TextFieldIfNoArrow(Rect rect, string text)
		{
			if (Event.current.type == EventType.KeyDown &&
			(Event.current.keyCode == UnityEngine.KeyCode.DownArrow ||
			Event.current.keyCode == UnityEngine.KeyCode.UpArrow))
				return text;
			
			//TextField will consume the event and set type to Used instead of KeyDown.
			//Plus, up/down just moves text input line and that's honestly annoying

			return Widgets.TextField(rect, text);
		}
	}
}

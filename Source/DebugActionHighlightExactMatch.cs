using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TDBug
{
	[HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.HighlightedIndex), MethodType.Getter)]
	//protected override int HighlightedIndex
	static class DebugActionHighlightExactMatch
	{
		public static bool FilterExactlyMatches(string filter, string label)
		{
			if (label.StartsWith("T: "))
				label = label.Substring(3);
			return String.Equals(filter, label, StringComparison.OrdinalIgnoreCase);
		}

		//After getting highlighted index, check if it exactly matches one and highlight that instead.
		public static void Postfix(ref int __result, Dialog_Debug __instance, string ___filter, DebugTabMenu ___currentTabMenu, ref int ___prioritizedHighlightedIndex)
		{
			if (FilterExactlyMatches(___filter, ___currentTabMenu.LabelAtIndex(__result)))
				return;

			for (int i = 0; i < ___currentTabMenu.Count; i++)
			{
				if (FilterExactlyMatches(___filter, ___currentTabMenu.LabelAtIndex(i)))
				{
					___prioritizedHighlightedIndex = i;
					__result = i;
					return;
				}
			}
		}
	}
}

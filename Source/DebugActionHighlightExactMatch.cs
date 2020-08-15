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
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu), "HighlightedIndex", MethodType.Getter)]
	//protected override int HighlightedIndex
	static class DebugActionHighlightExactMatch
	{
		public static MethodInfo FilterAllowsInfo = AccessTools.Method(typeof(Dialog_OptionLister), "FilterAllows");
		public static bool FilterAllows(Dialog_DebugOptionLister window, string label) =>
			(bool)FilterAllowsInfo.Invoke(window, new object[] { label });

		public static FieldInfo filterInfo = AccessTools.Field(typeof(Dialog_OptionLister), "filter");
		public static string GetFilter(this Dialog_OptionLister window) =>
			(string)filterInfo.GetValue(window);

		public static bool FilterExactlyMatches(string filter, string label)
		{
			if (label.StartsWith("T: "))
				label = label.Substring(3);
			return String.Equals(filter, label, StringComparison.OrdinalIgnoreCase);
		}

		//After getting highlighted index, check if it exactly matches one and highlight that instead.
		public static void Postfix(ref int __result, Dialog_DebugActionsMenu __instance, List<Dialog_DebugActionsMenu.DebugActionOption> ___debugActions, ref int ___prioritizedHighlightedIndex)
		{
			if (FilterExactlyMatches(__instance.GetFilter(), ___debugActions[__result].label))
				return;

			for (int i = 0; i < ___debugActions.Count; i++)
			{
				if (FilterExactlyMatches(__instance.GetFilter(), ___debugActions[i].label))
				{
					___prioritizedHighlightedIndex = i;
					__result = i;
					return;
				}
			}
		}
	}
}

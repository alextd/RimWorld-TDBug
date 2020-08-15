using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TDBug
{
	//The original filter method checked an exact substring so "pawn spawn" wouldn't find "spawn pawn"
	//Let's separate by word
	[HarmonyPatch(typeof(Dialog_OptionLister), "FilterAllows")]
	static class FilterSplitOnSpace
	{
		//protected bool FilterAllows(string label)
		static void Postfix(ref bool __result, string label, string ___filter)
		{
			if (__result) return;

			if (___filter.Contains(' ') || ___filter.Contains('\t'))
				__result = ___filter.Split(' ', '\t').All(
				f => label.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
		}
	}
}

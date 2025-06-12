﻿using System;
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
	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.FilterAllows))]
	public static class FilterSplitOnSpace
	{
		//protected bool FilterAllows(string label)
		public static void Postfix(ref bool __result, string label, string ___filter)
		{
			if (__result) return;

			if (___filter.Contains(' ') || ___filter.Contains('\t'))
				__result = ___filter.Split(' ', '\t').All(
				f => label.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
		}
	}

	// 1.4 Added Dialog_Debug for the main debug actions menu, Dialog_OptionLister kept for various sub-option menus
	[HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.FilterAllows))]
	static class FilterSplitOnSpace_Dialog_Debug
	{
		//protected bool FilterAllows(string label)
		static void Postfix(ref bool __result, string label, string ___filter) =>
			FilterSplitOnSpace.Postfix(ref __result, label, ___filter);
	}
}

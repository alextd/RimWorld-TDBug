using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	static class OnlyAction
	{
		public static bool done;
		public static Action action;
		public static Rect actionRect;
		public static int dropdownIndex;
		public static int currentIndex;

		public static MethodInfo FilterAllowsInfo = AccessTools.Method(typeof(Dialog_OptionLister), "FilterAllows");
		public static bool FilterAllowsAccess(this Dialog_DebugOptionLister window, string label) =>
			(bool)FilterAllowsInfo.Invoke(window, new object[] { label });

		public static FieldInfo filterInfo = AccessTools.Field(typeof(Dialog_OptionLister), "filter");
		public static string GetFilter(this Dialog_OptionLister window) =>
			(string)filterInfo.GetValue(window);

		public static FieldInfo listingInfo = AccessTools.Field(typeof(Dialog_OptionLister), "listing");
		public static Listing_Standard GetListing(this Dialog_OptionLister window) =>
			(Listing_Standard)listingInfo.GetValue(window);

		public static void AddAction(Dialog_DebugOptionLister window, string label, Action newAction)
		{
			if (window.FilterAllowsAccess(label))
			{
				if (label.StartsWith("T: "))
					label = label.Substring(3);

				if (currentIndex++ == dropdownIndex || String.Equals(window.GetFilter(), label, StringComparison.OrdinalIgnoreCase))
				{
					//Exact match or selected
					done = true;

					action = newAction;

					actionRect = window.GetListing().GetRect(23f);
					window.GetListing().Gap(-23f);//this feels like a cheat
				}
				else if (done) { }
				else if (action == null)
				{
					//first action found, allow it:
					action = newAction;

					actionRect = window.GetListing().GetRect(23f);
					window.GetListing().Gap(-23f);//this feels like a cheat
				}
				else
				{
					//second action, so too many
					action = null;
					done = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Dialog_OptionLister), MethodType.Constructor)]
	public static class DropdownIndexInit
	{
		public static void Prefix()
		{
			OnlyAction.dropdownIndex = -1;
		}
	}

	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.DoWindowContents))]
	public static class OnlyActionInit
	{
		//public override void DoWindowContents(Rect inRect)
		public static void Prefix()
		{
			OnlyAction.action = null;
			OnlyAction.done = false;
			OnlyAction.currentIndex = 0;

			if (Event.current.keyCode == KeyCode.DownArrow && Event.current.type == EventType.KeyDown)
			{
				OnlyAction.dropdownIndex++;
				Event.current.Use();
			}
			if (Event.current.keyCode == KeyCode.UpArrow && Event.current.type == EventType.KeyDown)
			{
				OnlyAction.dropdownIndex--;
				Event.current.Use();
			}
		}
		
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo EndInfo = AccessTools.Method(typeof(Listing), nameof(Listing.End));

			foreach(var i in instructions)
			{
				if(i.Calls(EndInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OnlyActionInit), nameof(DrawHighlight)));
				}
				yield return i;
			}
		}

		public static Color highlightColor = new Color(1, 1, 0, 0.3f);
		public static void DrawHighlight()
		{
			if (OnlyAction.action != null)
				Widgets.DrawBoxSolid(OnlyAction.actionRect, highlightColor);
		}
	}

	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.PostClose))]
	class DebugActionDoOnlyOption
	{
		//public override void PostClose()
		public static void Postfix()
		{
			if (Event.current.keyCode == KeyCode.Return)
				OnlyAction.action?.Invoke();
		}
	}

	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DebugAction")]
	static class DebugActionCheckItem
	{
		//protected bool DebugAction(string label, Action action)
		public static void Prefix(Dialog_DebugOptionLister __instance, string label, Action action)
		{
			OnlyAction.AddAction(__instance, label, action);
		}
	}

	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DebugToolMap")]
	static class DebugToolMapCheckItem
	{
		//protected void DebugToolMap(string label, Action toolAction)
		public static void Prefix(Dialog_DebugOptionLister __instance, string label, Action toolAction)
		{
			OnlyAction.AddAction(__instance, label, () => DebugTools.curTool = new DebugTool(label, toolAction));
		}
	}

	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DebugToolWorld")]
	static class DebugToolWorldCheckItem
	{
		//protected void DebugToolWorld(string label, Action toolAction)
		public static void Prefix(Dialog_DebugOptionLister __instance, string label, Action toolAction)
		{
			OnlyAction.AddAction(__instance, label, () => DebugTools.curTool = new DebugTool(label, toolAction));
		}
	}

	//CheckboxLabeledDebug would be nice but return closes the window


	//The original filter method checked an exact substring so "pawn spawn" wouldn't find "spawn pawn"
	//Let's separate by word
	[HarmonyPatch(typeof(Dialog_OptionLister), "FilterAllows")]
	static class FilterSplitOnSpace
	{
		//protected bool FilterAllows(string label)
		public static bool Prefix(Dialog_OptionLister __instance, string ___filter, string label, ref bool __result)
		{
			__result = true;
			if (!___filter.NullOrEmpty() && !label.NullOrEmpty())
				foreach (string filterWord in ___filter.Split(' ', '\t'))
					if (label.IndexOf(filterWord, StringComparison.OrdinalIgnoreCase) < 0)
						__result = false;

			return false;
		}
	}
}

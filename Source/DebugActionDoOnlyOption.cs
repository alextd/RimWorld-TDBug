using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace TDBug
{
	static class OnlyAction
	{
		public static bool done;
		public static Action action;

		public static MethodInfo FilterAllowsInfo = AccessTools.Method(typeof(Dialog_OptionLister), "FilterAllows");
		public static bool FilterAllowsAccess(this Dialog_DebugOptionLister window, string label) =>
			(bool)FilterAllowsInfo.Invoke(window, new object[] { label });

		public static FieldInfo filterInfo = AccessTools.Field(typeof(Dialog_OptionLister), "filter");
		public static string GetFilter(this Dialog_DebugOptionLister window) =>
			(string) filterInfo.GetValue(window);

		public static void AddAction(Dialog_DebugOptionLister __instance, string label, Action action)
		{
			if (__instance.FilterAllowsAccess(label))
			{
				if(label.StartsWith("T: "))
					label = label.Substring(3);

				if (String.Equals(__instance.GetFilter(), label, StringComparison.OrdinalIgnoreCase))
				{
					//Exact match
					OnlyAction.action = action;
					done = true;
				}
				else if (OnlyAction.done) { }
				else if (OnlyAction.action == null)
				{
					//first action allowed:
					OnlyAction.action = action;
				}
				else
				{
					//second action allowed, so too many
					OnlyAction.action = null;
					OnlyAction.done = true;
				}
			}
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
		public static void Postfix(Dialog_DebugOptionLister __instance, string label, Action action)
		{
			OnlyAction.AddAction(__instance, label, action);
		}
	}

	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DebugToolMap")]
	static class DebugToolMapCheckItem
	{
		//protected void DebugToolMap(string label, Action toolAction)
		public static void Postfix(Dialog_DebugOptionLister __instance, string label, Action toolAction)
		{
			OnlyAction.AddAction(__instance, label, () => DebugTools.curTool = new DebugTool(label, toolAction));
		}
	}

	[HarmonyPatch(typeof(Dialog_DebugOptionLister), "DebugToolWorld")]
	static class DebugToolWorldCheckItem
	{
		//protected void DebugToolWorld(string label, Action toolAction)
		public static void Postfix(Dialog_DebugOptionLister __instance, string label, Action toolAction)
		{
			OnlyAction.AddAction(__instance, label, () => DebugTools.curTool = new DebugTool(label, toolAction));
		}
	}

	//CheckboxLabeledDebug would be nice but return closes the window
}

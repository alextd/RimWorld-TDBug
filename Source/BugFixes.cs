using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	/*
	 * Fix bug where esc key closes two windows.
	 * Bunch of commented-out methods to test to find out that Dialog_MessageBox.OnCancelKeyPressed wasn't doing Event.current.Use() after calling Close().
	 * And to find that a Page underneath a Dialog_MessageBox DOESN'T have this problem, since it has absorbInputAroundWindow = true,
	 * roundaboutly making the Dialog think it doesn't get input and therefore should use the event (the one that just closed it!)
	 * Without this absorb, a normal window underneath a Dialog_MessageBox will close on esc . I guess that never happens in vanilla.

	[HarmonyPatch(typeof(Page), MethodType.Constructor)]
	public static class NewFeature22
	{
		public static void Postfix(Page __instance)
		{
			__instance.absorbInputAroundWindow = false;
		}
	}

	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.HandleEventsHighPriority))]
	public static class NewFeature2
	{
		public static void Prefix(WindowStack __instance)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"HandleEventsHighPriority GO : {Event.current}");
		}
		public static void Postfix(WindowStack __instance)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"HandleEventsHighPriority DONE : {Event.current}");
		}
	}

	[HarmonyPatch(typeof(Window), nameof(Window.InnerWindowOnGUI))]
	public static class NewFeature27
	{
		public static void Prefix(Window __instance)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Window.InnerWindowOnGUI GO : {Event.current}");
		}
		public static void Postfix(Window __instance)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Window.InnerWindowOnGUI DONE : {Event.current}");
		}
	}


	[HarmonyPatch(typeof(Dialog_MessageBox), nameof(Dialog_MessageBox.OnCancelKeyPressed))]
	public static class NewFeature3
	{
		public static void Prefix(Dialog_MessageBox __instance)
		{
			Log.Message($"Dialog_MessageBox.OnCancelKeyPressed : {__instance.cancelAction} / {Event.current}");
			//public override void OnCancelKeyPressed()
		}
	}

	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Notify_PressedCancel))]
	public static class NewFeature
	{
		public static bool Prefix(WindowStack __instance)
		{
			Log.Message($"Notify_PressedCancel GO : {Event.current}");
			for (int num = __instance.windows.Count - 1; num >= 0; num--)
			{
				Log.Message($"?  {num}: {__instance.windows[num]} : {__instance.windows[num].closeOnCancel} || {__instance.windows[num].forceCatchAcceptAndCancelEventEvenIfUnfocused} && {__instance.GetsInput(__instance.windows[num])}");
				if ((__instance.windows[num].closeOnCancel || __instance.windows[num].forceCatchAcceptAndCancelEventEvenIfUnfocused) && __instance.GetsInput(__instance.windows[num]))
				{
					Log.Message($"Yes {num}");
					__instance.windows[num].OnCancelKeyPressed();
					break;
				}
			}
			Log.Message($"Notify_PressedCancel Done : {Event.current}");

			return false;
		}
	}
	*/

	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Notify_PressedCancel))]
	public static class Notify_PressedCancelUseEvent
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo OnCancelKeyPressedInfo = AccessTools.Method(typeof(Window), nameof(Window.OnCancelKeyPressed));

			foreach (var inst in instructions)
			{
				yield return inst;

				if(inst.Calls(OnCancelKeyPressedInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), nameof(Event.current)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Event), nameof(Event.Use)));
				}
			}
		}
	}

	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Notify_PressedAccept))]
	public static class Notify_PressedAcceptUseEvent
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo OnAcceptKeyPressedInfo = AccessTools.Method(typeof(Window), nameof(Window.OnAcceptKeyPressed));

			foreach (var inst in instructions)
			{
				yield return inst;

				if (inst.Calls(OnAcceptKeyPressedInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Event), nameof(Event.current)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Event), nameof(Event.Use)));
				}

			}
		}
	}
}

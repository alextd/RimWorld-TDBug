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


	// WidgetRow.ButtonText does A Text.CalcSize to find the size of the button ; that height ends up 22.
	// But when given a width override, the height is just flatly set to 24 (The icon height is 24)
	// This makes buttons inconsistently 2 px different. So just set that 24 to a 22 here.
	[HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.ButtonRect))]
	public static class ButtonrectHeightFixer
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var inst in instructions)
			{
				if (inst.operand is float f && f == 24f)
					inst.operand = 22f;

				yield return inst;
			}
		}
	}

	/*
	 * Fix bug where a draggable window preventing reorderable widgets from reordering
	 * The Event.current.mousePosition of the reorderable was always adjusted to have not moved
	 * Because GUI.DragWindow would adjust it to anchor to the cursor
	 * Simply fix: don't GUI.DragWindow if a reoderable rect has been clicked
	 * 
	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable))]
	public static class NewFeature
	{
		//public static bool Reorderable(int groupID, Rect rect, bool useRightButton = false, bool highlightDragged = true)
		public static void Prefix(int groupID)
		{
			if(Event.current.type == EventType.Repaint)
				Log.Message($"if (draggingReorderable({ReorderableWidget.draggingReorderable}) != -1 && dragBegun({ReorderableWidget.dragBegun}) || (Vector2.Distance(clickedAt({ReorderableWidget.clickedAt}), Event.current.mousePosition({Event.current.mousePosition}) > 5f && groupClicked({ReorderableWidget.groupClicked}) == groupID({groupID})");

		}
	}

	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.ReorderableWidgetOnGUI_AfterWindowStack))]
	public static class NewFeatureReorderableWidgetOnGUI_AfterWindowStack
	{
		public static void Prefix()
		{
			if (Event.current.type == EventType.Repaint && ReorderableWidget.clicked)
				Log.Message($"ReorderableWidgetOnGUI_AfterWindowStack Repaint : clicked = {ReorderableWidget.clicked}");
		}

		public static void Postfix()
		{
			Log.Message($"ReorderableWidgetOnGUI_AfterWindowStack Post: draggingReorderable = {ReorderableWidget.draggingReorderable}, groupClicked={ReorderableWidget.groupClicked}, lastInsertNear = {ReorderableWidget.lastInsertNear}, hoveredGroup = {ReorderableWidget.hoveredGroup}");
			
		}
	}

	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable))]
	public static class NewFeaturez
	{
		//	public static bool Reorderable(int groupID, Rect rect, bool useRightButton = false, bool highlightDragged = true)

		public static void Postfix(int groupID, Rect rect)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Reorderable clicked = {ReorderableWidget.clicked} : {groupID} / {rect}");
		}
	}

	[HarmonyPatch(typeof(Window), nameof(Window.InnerWindowOnGUI))]
	public static class Logevent
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DragWindowInfo = AccessTools.Method(typeof(GUI), nameof(GUI.DragWindow));

			foreach (var inst in instructions)
			{
				if (inst.Calls(DragWindowInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Logevent), nameof(LogEvent1)));

					yield return inst;

					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Logevent), nameof(LogEvent2)));

				}
				else
					yield return inst;
			}
		}
		public static void LogEvent1()
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Event b4 is {Event.current} :: clicked = {ReorderableWidget.clicked}");
		}
		public static void LogEvent2()
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Event af is {Event.current} :: clicked = {ReorderableWidget.clicked}");
		}
	}
	*/

	[HarmonyPatch(typeof(Window), nameof(Window.InnerWindowOnGUI))]
	public static class FixWindowDragInsteadOfReorderable
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo draggableInfo = AccessTools.Field(typeof(Window), nameof(Window.draggable));
			foreach (var inst in instructions)
			{
				if(inst.LoadsField(draggableInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixWindowDragInsteadOfReorderable), nameof(DraggableAndNotReordering)));
				}
				else
					yield return inst;
			}
		}

		public static bool DraggableAndNotReordering(Window window)
		{
			return window.draggable && ReorderableWidget.draggingReorderable == -1;
		}
	}


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

				if (inst.Calls(OnCancelKeyPressedInfo))
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
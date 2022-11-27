using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	// It turns out that reorderable widgets, with multi-groups, and dragging onto an empty group, doesn't work in the game UI
	// This is 100% because of the COLONIST BAR at the top center of the screen.
	// For some reason its rect is the entire screen. This rect is only used for multi-group reordering which it doesn't even do.
	// The code would find what group's rect is "hovered" over to drop onto that group.
	// That works fine in the modlist, because there's no colonist bar, so you can drop onto an empty modlist and it'll handle that
	// But when there's a goddamn colonist bar drawing after all your UI that says it takes up the entire screen,
	// it hijacks all that and says I'M ON TOP, I GET THE REORDER DROP
	// But then later it goes "oh wait, I'm not in your group, nevermind" and so nothing happens.
	// All the code needs to do is NOT set the hoveredGroup is that group is not inthe multigroup for the dragged reorderable.

	// SECOND. To support nested reordering areas. It has to do a few more checks about those rects. Which are done in the below patch
	
	/*
	//[HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.Visible), MethodType.Getter)]
	public static class NoBar
	{
		public static void Postfix(ref bool __result)
		{
			__result = false;
		}
	}

	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.ReorderableWidgetOnGUI_AfterWindowStack))]
	public static class HijackAfterWindowStack
	{
		public static bool Prefix()
		{
			if (Event.current.rawType == EventType.MouseUp)
			{
				ReorderableWidget.released = true;
			}
			if (Event.current.type != EventType.Repaint)
			{
				return false;
			}
			if (ReorderableWidget.clicked)
			{
				ReorderableWidget.StopDragging();
				for (int i = 0; i < ReorderableWidget.reorderables.Count; i++)
				{
					Log.Message($"if (ReorderableWidget.reorderables[{i}].groupID  ({ ReorderableWidget.reorderables[i].groupID}) == { ReorderableWidget.groupClicked} && { ReorderableWidget.reorderables[i].rect} == {ReorderableWidget.clickedInRect}))");
					if (ReorderableWidget.reorderables[i].groupID == ReorderableWidget.groupClicked && ReorderableWidget.reorderables[i].rect == ReorderableWidget.clickedInRect)
					{
						ReorderableWidget.draggingReorderable = i;
						ReorderableWidget.dragStartPos = Event.current.mousePosition;
						break;
					}
				}
				ReorderableWidget.clicked = false;
			}
			if (ReorderableWidget.draggingReorderable >= ReorderableWidget.reorderables.Count)
			{
				ReorderableWidget.StopDragging();
			}
			if (ReorderableWidget.reorderables.Count != ReorderableWidget.lastFrameReorderableCount)
			{	
				ReorderableWidget.StopDragging();
			}
			ReorderableWidget.lastInsertNear = ReorderableWidget.CurrentInsertNear(out ReorderableWidget.lastInsertNearLeft);
			if (ReorderableWidget.lastInsertNear >= 0)
				Log.Message($"STARTING with lastInsertNear = {ReorderableWidget.lastInsertNear} ; lastInsertNearLeft = {ReorderableWidget.lastInsertNearLeft} ; hoveredGroup = {ReorderableWidget.hoveredGroup} ; {(ReorderableWidget.hoveredGroup >= 0 ? ReorderableWidget.groups[ReorderableWidget.hoveredGroup].absRect:null)}");
			ReorderableWidget.hoveredGroup = -1;
			for (int j = 0; j < ReorderableWidget.groups.Count; j++)
			{
				// Original if statement didn't handle nested rects.
				// if (ReorderableWidget.groups[j].absRect.Contains(Event.current.mousePosition))
				
				if (ReorderableWidget.lastInsertNear >= 0)
					Log.Message($@"
					if ({ReorderableWidget.groups[j].absRect}.Contains({Event.current.mousePosition})
					&&
					({ReorderableWidget.lastInsertNear} == -1 ||
					!{ReorderableWidget.groups[j].absRect}.Contains({ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear<0?0: ReorderableWidget.lastInsertNear].absRect}))
					&&
					({ReorderableWidget.hoveredGroup} == -1 ||
					{ReorderableWidget.groups[ReorderableWidget.hoveredGroup<0?0: ReorderableWidget.hoveredGroup].absRect}.Contains({ReorderableWidget.groups[j].absRect}))");
				
				if (ReorderableWidget.groups[j].absRect.Contains(Event.current.mousePosition)
					&&
					(ReorderableWidget.lastInsertNear == -1 ||
					!ReorderableWidget.groups[j].absRect.Contains(ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear].absRect.ContractedBy(1))) 
					&&
					(ReorderableWidget.hoveredGroup == -1 ||
					ReorderableWidget.groups[ReorderableWidget.hoveredGroup].absRect.Contains(ReorderableWidget.groups[j].absRect))
)
				{
					// Original if block directly set hoveredGroup without checking if it was in the multigroup
					// ReorderableWidget.hoveredGroup = j;
					if (ReorderableWidget.lastInsertNear >= 0)
						Log.Message($" Could be {j} : {ReorderableWidget.groups[j].absRect}");
					if (ReorderableWidget.lastInsertNear >= 0 && ReorderableWidget.AreInMultiGroup(j, ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear].groupID) && ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear].groupID != j)
					{
						ReorderableWidget.lastInsertNear = ReorderableWidget.FindLastReorderableIndexWithinGroup(j);
						ReorderableWidget.lastInsertNearLeft = ReorderableWidget.lastInsertNear < 0;
						ReorderableWidget.hoveredGroup = j; // < - this right here fixes it
						Log.Message($"  SETTING lastInsertNear = {ReorderableWidget.lastInsertNear}; lastInsertNearLeft = {ReorderableWidget.lastInsertNearLeft} ; hoveredGroup = {ReorderableWidget.hoveredGroup} ; {ReorderableWidget.groups[ReorderableWidget.hoveredGroup].absRect}");
					}
				}
			}
			if (ReorderableWidget.released)
			{
				ReorderableWidget.released = false;
				if (ReorderableWidget.dragBegun && ReorderableWidget.draggingReorderable >= 0)
				{
					Log.Message($"RELEASED! : hoveredGroup = {ReorderableWidget.hoveredGroup} lastInsertNear = {ReorderableWidget.lastInsertNear}; lastInsertNearLeft = {ReorderableWidget.lastInsertNearLeft}");
					int fromIndex = ReorderableWidget.GetIndexWithinGroup(ReorderableWidget.draggingReorderable);
					int fromID = ReorderableWidget.reorderables[ReorderableWidget.draggingReorderable].groupID;
					int toIndex = ((ReorderableWidget.lastInsertNear == ReorderableWidget.draggingReorderable) ? fromIndex : ((!ReorderableWidget.lastInsertNearLeft) ? (ReorderableWidget.GetIndexWithinGroup(ReorderableWidget.lastInsertNear) + 1) : ReorderableWidget.GetIndexWithinGroup(ReorderableWidget.lastInsertNear)));
					int toID = -1;
					Log.Message($" Thinking ({fromIndex}, {fromID}, {toIndex}, {toID})");
					if (ReorderableWidget.lastInsertNear >= 0)
					{
						toID = ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear].groupID;
						Log.Message($"  toID = {toID}");
					}
					if (ReorderableWidget.AreInMultiGroup(fromID, ReorderableWidget.hoveredGroup) && ReorderableWidget.hoveredGroup >= 0 && ReorderableWidget.hoveredGroup != toID)
					{
						toID = ReorderableWidget.hoveredGroup;
						toIndex = ReorderableWidget.GetIndexWithinGroup(ReorderableWidget.FindLastReorderableIndexWithinGroup(toID)) + 1;
						Log.Message($"  toID = {toID}; toIndex = {toIndex}");
					}
					if (ReorderableWidget.AreInMultiGroup(fromID, toID))
					{
						Log.Message($" Doing it for {fromID}: ({fromIndex}, {fromID}, {toIndex}, {toID}");
						ReorderableWidget.GetMultiGroupByGroupID(fromID).Value.reorderedAction(fromIndex, fromID, toIndex, toID);
						SoundDefOf.DropElement.PlayOneShotOnCamera();
					}
					else if (toIndex >= 0 && toIndex != fromIndex && toIndex != fromIndex + 1)
					{
						SoundDefOf.DropElement.PlayOneShotOnCamera();
						try
						{
							Log.Message($"Doing it for {ReorderableWidget.draggingReorderable}: ({fromIndex}, {toIndex})");
							ReorderableWidget.groups[ReorderableWidget.reorderables[ReorderableWidget.draggingReorderable].groupID].reorderedAction(fromIndex, toIndex);
						}
						catch (Exception ex)
						{
							Log.Error("Could not reorder elements (from " + fromIndex + " to " + toIndex + "): " + ex);
						}
					}
				}
				ReorderableWidget.StopDragging();
			}
			ReorderableWidget.lastFrameReorderableCount = ReorderableWidget.reorderables.Count;
			ReorderableWidget.multiGroups.Clear();
			ReorderableWidget.groups.Clear();
			ReorderableWidget.reorderables.Clear();



			return false;
		}
	}
	*/
	

	// Simply move hoveredGroup = j into the if block which checks if we care about j, isn't that smart
	// Secondly, add support for overlapping reorder rects.
	// To support nested rects, also check if the new hoveredRect is inside the old one,
	// and the "nearest" reorderable is not within the rect.
	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.ReorderableWidgetOnGUI_AfterWindowStack))]
	public static class FixReorderableHoveredGroup
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ContainsInfo = AccessTools.Method(typeof(Rect), nameof(Rect.Contains), new Type[] { typeof(Vector2)});

			FieldInfo hoveredGroupInfo = AccessTools.Field(typeof(ReorderableWidget), nameof(ReorderableWidget.hoveredGroup));
			FieldInfo lastInsertNearLeftInfo = AccessTools.Field(typeof(ReorderableWidget), nameof(ReorderableWidget.lastInsertNearLeft));

			List<CodeInstruction> instList = instructions.ToList();
			int hoverGroupIndex = -1;
			for (int i = 0; i < instList.Count; i++)
			{
				// 1. Check if hoveredRect contains new hoveredRect
				if(instList[i].Calls(ContainsInfo))
				{
					instList[i].operand = AccessTools.Method(typeof(FixReorderableHoveredGroup), nameof(RectContainsMouseAndInsideOld));
				}

				// 2.a. Move 'hoveredGroup = j' inside the if
				else if (i < instList.Count - 1)
				{
					//If it's not hoveredGroup = -1, it's gonna be hoveredGroup = j (local int)
					if (instList[i + 1].StoresField(hoveredGroupInfo) && instList[i].opcode != OpCodes.Ldc_I4_M1)
					{
						hoverGroupIndex = i;
						i += 2;
					}
				}

				yield return instList[i];

				if (instList[i].StoresField(lastInsertNearLeftInfo))
				{
					// 2.b. Do 'hoveredGroup = j' only after 'lastInsertNearLeft = ...'
					yield return instList[hoverGroupIndex]; // load j
					yield return instList[hoverGroupIndex + 1]; // set static hoveredGroup = j
				}
			}
		}

		public static bool RectContainsMouseAndInsideOld(ref Rect checkIfHoveredRect, Vector2 mousePos) =>
			// Vanilla check "Is the mouse over this group"
			checkIfHoveredRect.Contains(mousePos)
				&&
			// Also Check that the closest reorderable row is not within this rect
			(ReorderableWidget.lastInsertNear == -1 ||
			!checkIfHoveredRect.Contains(ReorderableWidget.reorderables[ReorderableWidget.lastInsertNear].absRect.ContractedBy(1)))
				&&
			// Also Check that this rect is within the already hovered rect, if it exists.
			(ReorderableWidget.hoveredGroup == -1 ||
			ReorderableWidget.groups[ReorderableWidget.hoveredGroup].absRect.Contains(checkIfHoveredRect.ContractedBy(1)));



		public static bool Contains(this Rect self, Rect rect) =>
			self.Contains(rect.min) && self.Contains(rect.max);
	}


	// For good measure while I'm here, fix the absRect in ReorderableWidget.NewGroup which threw out the rect position and used zero.
	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.NewGroup))]
	public static class FixNewGroupAbsRect
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo VectorZeroInfo = AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero));

			foreach (var inst in instructions)
			{
				if(inst.Calls(VectorZeroInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarga_S, 2); // ref rect, as this
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.position))); // rect.position
				}
				else
					yield return inst;
			}
		}
	}

	// OF COURSE with the above patch, the vanilla code passes the wrong rect into NewGroup. So let's fix that.
	// The DoModList calls BeginScroll then immediately NewGroup, so the GUI coords were at 0,0. 
	// But the rect passed in could be elsewhere. Just nudge that rect to 0,0 for the newgroup.
	[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoModList))]
	public static class FixModsConfigRect
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(ReorderableWidget), nameof(ReorderableWidget.NewGroup)),
				AccessTools.Method(typeof(FixModsConfigRect), nameof(NewGroupWithRectAtZero)));

		
		public static int NewGroupWithRectAtZero(Action<int, int> reorderedAction, ReorderableDirection direction, Rect rect, float drawLineExactlyBetween_space = -1f, Action<int, Vector2> extraDraggedItemOnGUI = null, bool playSoundOnStartReorder = true) =>
			ReorderableWidget.NewGroup(reorderedAction, direction, rect.AtZero(), drawLineExactlyBetween_space, extraDraggedItemOnGUI, playSoundOnStartReorder);
	}






	// WidgetRow.Label and TextFieldNumeric apply a gap before and after the rect it uses
	// But instead of Gap() it directly calls IncrementPosition, without checking if startX==curX
	// So labels at the beginning of a widgetRow on somr rect would have a gap applied
	//  (whereas a similar Widgets.Label in the same rect does not)
	// Which is really funny, because caling Gap() first explicitly doesn't create a gap, but label does create a gap

	//  so TL;DR the first IncrementPosition in these methods should be Gap so it doesn't actually gap when curX == 0

	[HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.Label))]
	public static class FixWidgetRowLabelGap
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo IncrementPositionInfo = AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.IncrementPosition));
			MethodInfo GapInfo = AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.Gap));

			bool first = true;
			foreach (var inst in instructions)
			{
				if(first && inst.Calls(IncrementPositionInfo))
				{
					first = false;
					inst.operand = GapInfo;
				}
				yield return inst;
			}
		}
	}

	/*
	//TextFieldNumeric is generic and crashes rimworld on start
	[HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.TextFieldNumeric))]
	public static class FixWidgetRowTextFieldNumericGap
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			=> FixWidgetRowLabelGap.Transpiler(instructions);
	}
	*/


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
	 * Simply fix: don't GUI.DragWindow if a reoderable rect has been ReorderableWidget.clicked
	 * 
	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable))]
	public static class NewFeature
	{
		//public static bool Reorderable(int groupID, Rect rect, bool useRightButton = false, bool highlightDragged = true)
		public static void Prefix(int groupID)
		{
			if(Event.current.type == EventType.Repaint)
				Log.Message($"if (ReorderableWidget.draggingReorderable({ReorderableWidget.draggingReorderable}) != -1 && ReorderableWidget.dragBegun({ReorderableWidget.dragBegun}) || (Vector2.Distance(clickedAt({ReorderableWidget.clickedAt}), Event.current.mousePosition({Event.current.mousePosition}) > 5f && ReorderableWidget.groupClicked({ReorderableWidget.groupClicked}) == groupID({groupID})");

		}
	}

	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.ReorderableWidgetOnGUI_AfterWindowStack))]
	public static class NewFeatureReorderableWidgetOnGUI_AfterWindowStack
	{
		public static void Prefix()
		{
			if (Event.current.type == EventType.Repaint && ReorderableWidget.clicked)
				Log.Message($"ReorderableWidgetOnGUI_AfterWindowStack Repaint : ReorderableWidget.clicked = {ReorderableWidget.clicked}");
		}

		public static void Postfix()
		{
			Log.Message($"ReorderableWidgetOnGUI_AfterWindowStack Post: ReorderableWidget.draggingReorderable = {ReorderableWidget.draggingReorderable}, ReorderableWidget.groupClicked={ReorderableWidget.groupClicked}, ReorderableWidget.lastInsertNear = {ReorderableWidget.lastInsertNear}, ReorderableWidget.hoveredGroup = {ReorderableWidget.hoveredGroup}");
			
		}
	}

	[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable))]
	public static class NewFeaturez
	{
		//	public static bool Reorderable(int groupID, Rect rect, bool useRightButton = false, bool highlightDragged = true)

		public static void Postfix(int groupID, Rect rect)
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Reorderable ReorderableWidget.clicked = {ReorderableWidget.clicked} : {groupID} / {rect}");
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
				Log.Message($"Event b4 is {Event.current} :: ReorderableWidget.clicked = {ReorderableWidget.clicked}");
		}
		public static void LogEvent2()
		{
			if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
				Log.Message($"Event af is {Event.current} :: ReorderableWidget.clicked = {ReorderableWidget.clicked}");
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
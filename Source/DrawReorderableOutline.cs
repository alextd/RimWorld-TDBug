using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	// Add an option "Draw Reorderable Boxes" to draw a border on menu items that canbe drag-reordered using ReorderableWidget

	public static class ReorderBoxes
	{
		public static bool drawReorderableBoxes;
		//Maybe I won't use depth in any clever way. It's not easy to tell if box is inside box actually.
		/*
		public static int depth = 0;


		[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.ReorderableWidgetOnGUI_AfterWindowStack))]
		public static class ClearDepth
		{
			// public static void ReorderableWidgetOnGUI_AfterWindowStack()
			public static void Postfix()
			{
				depth = 0;
			}
		}
		*/


		[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.NewGroup))]
		public static class ReorderableWidgetNewGroup
		{
			// public static int NewGroup(Action<int, int> reorderedAction, ReorderableDirection direction, Rect rect, float drawLineExactlyBetween_space = -1f, Action<int, Vector2> extraDraggedItemOnGUI = null, bool playSoundOnStartReorder = true)
			public static void Postfix(Rect rect, int __result)
			{
				if (!drawReorderableBoxes) return;

				Text.Anchor = TextAnchor.LowerLeft;
				GUI.color = Color.red;

				Widgets.DrawBox(rect, 2);
				rect.xMin += 4;
				Widgets.Label(rect, __result.ToString());

				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;

				//depth++;
			}
		}

		[HarmonyPatch(typeof(ReorderableWidget), nameof(ReorderableWidget.Reorderable))]
		public static class ReorderableWidgetReorderable
		{
			// public static bool Reorderable(int groupID, Rect rect, bool useRightButton = false, bool highlightDragged = true)
			public static void Postfix(int groupID, Rect rect)
			{
				if (!drawReorderableBoxes) return;

				Text.Anchor = TextAnchor.LowerRight;
				GUI.color = Color.green;

				Widgets.DrawBox(rect);
				rect.width -= 4;
				Widgets.Label(rect, $"{groupID} : {ReorderableWidget.reorderables.Count - 1}");

				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;

				//depth++;
			}
		}



		[HarmonyPatch(typeof(DebugTabMenu_Settings), nameof(DebugTabMenu_Settings.InitActions))]
		public static class DebugSetting
		{
			public static void Postfix(DebugTabMenu_Settings __instance)
			{
				__instance.AddNode(AccessTools.Field(typeof(ReorderBoxes), nameof(drawReorderableBoxes)), "View");
			}
		}
	}
}

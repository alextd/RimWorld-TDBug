using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Harmony;

namespace TDBug
{
	public static class Listing_StandardExtensions
	{
		//listing.listingRect = rect;
		public static FieldInfo rectInfo = AccessTools.Field(typeof(Listing_Standard), "listingRect");
		//listing.columnWidthInt = listing.listingRect.width;
		public static FieldInfo widthInfo = AccessTools.Field(typeof(Listing_Standard), "columnWidthInt");
		//listing.curX = 0f;
		public static FieldInfo curXInfo = AccessTools.Field(typeof(Listing_Standard), "curX");
		//listing.curY = 0f;
		public static FieldInfo curYInfo = AccessTools.Field(typeof(Listing_Standard), "curY");
		public static FieldInfo fontInfo = AccessTools.Field(typeof(Listing_Standard), "font");
		public static void BeginScrollViewEx(this Listing_Standard listing, Rect rect, ref Vector2 scrollPosition, ref Rect viewRect)
		{
			//Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			//rect.height = 100000f;
			//rect.width -= 20f;
			//this.Begin(rect.AtZero());

			//Need BeginGroup before ScrollView, listingRect needs rect.width-=20 but the group doesn't

			GUI.BeginGroup(rect);
			Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, viewRect, true);

			rect.height = 100000f;
			rect.width -= 20f;
			//base.Begin(rect.AtZero());


			//listing.listingRect = rect;
			rectInfo.SetValue(listing, rect);
			//listing.columnWidthInt = listing.listingRect.width;
			widthInfo.SetValue(listing, rect.width);
			//listing.curX = 0f;
			curXInfo.SetValue(listing, 0);
			//listing.curY = 0f;
			curYInfo.SetValue(listing, 0);

			Text.Font = (GameFont)fontInfo.GetValue(listing);
		}
	}

	[HarmonyPatch(typeof(EditWindow_DebugInspector), nameof(EditWindow_DebugInspector.DoWindowContents))]
	public static class DebugInspectorScrollable
	{
		//public override void DoWindowContents(Rect inRect)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//dunno how this isn't Listing_Standard but apparently it's Listing in ILCode 
			MethodInfo beginInfo = AccessTools.Method(typeof(Listing), nameof(Listing.Begin));
			MethodInfo endInfo = AccessTools.Method(typeof(Listing), nameof(Listing.End));

			foreach (CodeInstruction i in instructions)
			{
				//listing_Standard.Begin(inRect);
				if (i.opcode == OpCodes.Callvirt && i.operand == beginInfo)
				{
					Log.Message("TRBG");
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DebugInspectorScrollable), nameof(BeginScroll)));
				}
				else if (i.opcode == OpCodes.Callvirt && i.operand == endInfo)
				{
					Log.Message("TREN");
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DebugInspectorScrollable), nameof(EndScroll)));
				}
				else 
					yield return i;
			}
		}

		public static Vector2 scrollPosition;
		public static Rect viewRect;
		public static float scrollViewHeight;
		public static void BeginScroll(Listing_Standard listing, Rect rect)
		{
			Log.Message("BEGIN");
			viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
			listing.BeginScrollViewEx(rect, ref scrollPosition, ref viewRect);
		}
		public static void EndScroll(Listing_Standard listing)
		{
			Log.Message("END");
			listing.EndScrollView(ref viewRect);
			scrollViewHeight = viewRect.height;
		}
	}
}

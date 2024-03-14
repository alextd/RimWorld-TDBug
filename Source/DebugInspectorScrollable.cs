using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace TDBug
{
	public static class Listing_StandardExtensions
	{
		public static FieldInfo fontInfo = AccessTools.Field(typeof(Listing_Standard), "font");
		public static void BeginScrollViewEx(this Listing_Standard listing, Rect rect, ref Vector2 scrollPosition, Rect viewRect)
		{
			//Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			//rect.height = 100000f;
			//rect.width -= 20f;
			//this.Begin(rect.AtZero());

			//Need BeginGroup before ScrollView, listingRect needs rect.width-=20 but the group doesn't

			Widgets.BeginGroup(rect);
			Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, viewRect, true);

			rect.height = 100000f;
			rect.width -= 20f;
			//base.Begin(rect.AtZero());


			listing.listingRect = rect;
			listing.columnWidthInt = rect.width;
			listing.curX = 0f;
			listing.curY = 0f;


			Text.Font = listing.font;
		}
	}

	//TOOD: Remove the "Make the columns wider" buttons
	[HarmonyPatch(typeof(EditWindow_DebugInspector), nameof(EditWindow_DebugInspector.DoWindowContents))]
	public static class DebugInspectorScrollable
	{
		//public override void DoWindowContents(Rect inRect)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//1.5 removed listing and manually adjusts the x and y shit, so let's just rewrite that section of code entirely
			//
			//

			// Replace everything after the SECOND:
			//    IL_0145: call         instance void [UnityEngine.CoreModule]UnityEngine.Rect::set_yMin(float32)

			MethodInfo setYMinInfo = AccessTools.PropertySetter(typeof(Rect), nameof(Rect.yMin));
			bool calledOnce = false;

			foreach (CodeInstruction i in instructions)
			{
				yield return i;

				if(i.Calls(setYMinInfo))
				{
					if (calledOnce)
					{
						// this
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// inRect
						yield return new CodeInstruction(OpCodes.Ldarg_1);

						// ref this.debugStringBuilder
						yield return new CodeInstruction(OpCodes.Ldarg_0); //this
						yield return new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(EditWindow_DebugInspector), nameof(EditWindow_DebugInspector.debugStringBuilder)));

						// ReWriteInspectorList(this, inRect, ref this.debugStringBuilder)
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DebugInspectorScrollable), nameof(ReWriteInspectorList)));

						//I guess this is needed
						yield return new CodeInstruction(OpCodes.Ret);


						//Kill the rest as I just rewrote it in ReWriteInspectorList
						yield break;
					}
					else
						calledOnce = true;
				}
			}
		}

		public static void ReWriteInspectorList(EditWindow_DebugInspector instance, Rect inRect, ref StringBuilder debugStringBuilder)
		{
			//from 1.4 code:
			Listing_Standard listing_Standard = new Listing_Standard(GameFont.Tiny);
			listing_Standard.ColumnWidth = Mathf.Min(360, inRect.width);
			BeginScroll(listing_Standard, inRect);//<- previously just replaced this
			string[] array = debugStringBuilder.ToString().Split('\n');
			foreach (string label in array)
			{
				listing_Standard.Label(label);
				listing_Standard.Gap(-9f);
			}
			EndScroll(listing_Standard);//< -previously just replaced this

			//TODO: this is unchanged 1.5 but it'd be harder to NOT remove it from ilcode.
			if (Event.current.type == EventType.Repaint)
			{
				debugStringBuilder = new StringBuilder();
				debugStringBuilder.Append(instance.CurrentDebugString());
			}
		}


		public static Vector2 scrollPosition;
		public static float scrollViewHeight;
		public static void BeginScroll(Listing_Standard listing, Rect rect)
		{
			Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
			listing.BeginScrollViewEx(rect, ref scrollPosition, viewRect);
		}
		public static void EndScroll(Listing_Standard listing)
		{
			scrollViewHeight = listing.CurHeight;
			Widgets.EndScrollView();
			listing.End();
		}
	}
}

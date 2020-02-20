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
	[HarmonyPatch(typeof(EditWindow_DebugInspector), nameof(EditWindow_DebugInspector.DoWindowContents))]
	public static class ThingById_GUI
	{
		public static float headerHeight = 30f;
		//public override void DoWindowContents(Rect inRect)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ToggleableIconInfo = AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.ToggleableIcon));

			bool firstIcon = true;
			foreach (CodeInstruction i in instructions)
			{
				if (i.opcode == OpCodes.Callvirt && i.operand.Equals(ToggleableIconInfo) && firstIcon)
				{
					firstIcon = false;

					//widgetRow.ToggleableIcon(ref DebugViewSettings.writeCellContents, TexButton.InspectModeToggle, "Toggle shallow inspection for things on the map.", null, null);
					i.operand = AccessTools.Method(typeof(ThingById_GUI), nameof(InsertToggleableIcon));
				}

				yield return i;

				if (i.opcode == OpCodes.Ldc_R4 && ((float)i.operand).Equals(headerHeight))
				{
					//Add another 30 pixels before readout to make room for id input (can't put it right of title, parent class controls that)
					yield return new CodeInstruction(OpCodes.Ldc_R4, headerHeight);
					yield return new CodeInstruction(OpCodes.Add);
				}
			}
		}

		public static bool showSelected = true;
		public static void InsertToggleableIcon(WidgetRow row, ref bool toggleable, Texture2D tex, string tooltip, SoundDef mouseoverSound = null, string tutorTag = null)
		{
			row.ToggleableIcon(ref showSelected, tex, "Toggle deep inspection mode for selected things", mouseoverSound);//no tutor just in case.
			row.ToggleableIcon(ref toggleable, tex, tooltip, mouseoverSound, tutorTag);
		}

		public static int idToFind;
		public static string idBuffer;
		public static List<Thing> foundThings = new List<Thing>();
		public static void Prefix(Rect inRect)
		{
			if (Current.Game == null) return;

			Rect idRect = new Rect(inRect.x, inRect.y + headerHeight, inRect.width, headerHeight);
			int prevId = idToFind;
			Widgets.TextFieldNumeric(idRect.LeftHalf(), ref idToFind, ref idBuffer);
			if (idToFind != prevId)
				foundThings = new List<Thing>(FindThings());
			if (idBuffer == "" && idToFind != 0)
				foundThings = new List<Thing>();

			if (!foundThings.NullOrEmpty())
			{
				TargetHighlighter.Highlight(foundThings[0]);
				if (Widgets.ButtonText(idRect.RightHalf(), "Go to"))
				{
					Thing thing = foundThings[0];
					Current.Game.CurrentMap = thing.MapHeld;
					Find.CameraDriver.JumpToCurrentMapLoc(thing.PositionHeld);
				}
			}
		}

		public static IEnumerable<Thing> FindThings()
		{
			foreach (Thing thing in FindParentThings())
			{
				if (thing.thingIDNumber == idToFind)
					yield return thing;
				if (thing is IThingHolder holder)
					foreach (Thing childThing in ThingOwnerUtility.GetAllThingsRecursively(holder))
						if (childThing.thingIDNumber == idToFind)
							yield return childThing;
			}
		}

		public static IEnumerable<Thing> FindParentThings()
		{
			foreach (Map map in Current.Game.Maps)
				foreach (Thing thing in map.listerThings.AllThings)
					yield return thing;
			foreach (Thing thing in Current.Game.World.worldPawns.AllPawnsAliveOrDead)
				yield return thing;
		}
	}

	[HarmonyPatch(typeof(EditWindow_DebugInspector), "CurrentDebugString")]
	public static class ThingById_Readout
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo writeStorytellerInfo = AccessTools.Field(typeof(DebugViewSettings), nameof(DebugViewSettings.writeStoryteller));
			
			foreach (CodeInstruction i in instructions)
			{
				//ldfld        bool Verse.EditWindow_DebugInspector::fullMode
				if (i.opcode == OpCodes.Ldsfld && i.operand.Equals(writeStorytellerInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0) { labels = i.labels }; //local StringBuilder ; todo: find it better
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingById_Readout), nameof(ReadoutFoundThings)));
					i.labels = null;
				}

				yield return i;
			}
		}

		public static void ReadoutFoundThings(StringBuilder sb)
		{
			foreach (Thing thing in ThingById_GUI.foundThings)
			{
				sb.AppendLine($"--- Thing By ID: {thing}");

				IThingHolder holder = thing.ParentHolder;
				while (holder != null)
				{
					if (holder is Thing owner)
					{
						sb.AppendLine($"--- Held by {owner}");
						break;
					}
					holder = holder.ParentHolder;
				}
				sb.AppendLine(Scribe.saver.DebugOutputFor(thing));
			}

			if(ThingById_GUI.showSelected)
			{
				sb.AppendLine("--- Selected Things");
				foreach(object o in Find.Selector.SelectedObjectsListForReading)
				{
					if (o is Thing selected)
					{
						sb.AppendLine(Scribe.saver.DebugOutputFor(selected));
					}
				}
			}
		}
	}
}

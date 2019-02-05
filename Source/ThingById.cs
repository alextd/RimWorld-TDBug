using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;
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
			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.opcode == OpCodes.Ldc_R4 && (float)i.operand == headerHeight)
				{
					//Add another 30 pixels before readout to make room for id input (can't put it right of title, parent class controls that)
					yield return new CodeInstruction(OpCodes.Ldc_R4, headerHeight);
					yield return new CodeInstruction(OpCodes.Add);
				}
			}
		}

		public static int idToFind;
		public static string idBuffer;
		public static void Prefix(Rect inRect)
		{
			Rect textRect = new Rect(inRect.x, inRect.y + headerHeight, inRect.width, headerHeight);
			Widgets.TextFieldNumeric(textRect, ref idToFind, ref idBuffer);
		}
	}

	[HarmonyPatch(typeof(EditWindow_DebugInspector), "CurrentDebugString")]
	public static class ThingById_Readout
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo writeCellContentsInfo = AccessTools.Field(typeof(DebugViewSettings), nameof(DebugViewSettings.writeCellContents));
			

			foreach (CodeInstruction i in instructions)
			{
				//ldfld        bool Verse.EditWindow_DebugInspector::fullMode
				if (i.opcode == OpCodes.Ldsfld && i.operand == writeCellContentsInfo)
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
			foreach (Thing thing in FindThings(ThingById_GUI.idToFind))
			{
				sb.AppendLine("--- Item By Id");
				sb.AppendLine(Scribe.saver.DebugOutputFor(thing));
			}
		}

		public static IEnumerable<Thing> FindThings(int id)
		{
			if (Current.Game == null) yield break;
			foreach (Map map in Current.Game.Maps)
				foreach (Thing thing in map.listerThings.AllThings)
					if (thing.thingIDNumber == id)
						yield return thing;
			foreach (Thing thing in Current.Game.World.worldPawns.AllPawnsAliveOrDead)
				if (thing.thingIDNumber == id)
					yield return thing;
			//Todo: inner containers. or just somehow ALL THINGS
		}
	}
}

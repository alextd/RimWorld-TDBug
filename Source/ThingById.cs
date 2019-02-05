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
	class ThingById
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
					Log.Message("DID IT");
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
}

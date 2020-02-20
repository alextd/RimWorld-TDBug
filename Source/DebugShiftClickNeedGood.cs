using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(DebugToolsPawns), "OffsetNeed")]
	class DebugShiftClickNeedGood
	{
		//private static void OffsetNeed(NeedDef nd, float offsetPct)
		public static void Prefix(ref float offsetPct)
		{
			if (Event.current.shift)
				offsetPct = -offsetPct;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Harmony;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu), "OffsetNeed")]
	class DebugShiftClickNeedGood
	{
		//private void OffsetNeed(NeedDef nd, float offsetPct)
		public static void Prefix(ref float offsetPct)
		{
			if (Event.current.shift)
				offsetPct = -offsetPct;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(Area), "Set")]
	public static class GodmodeRoof
	{
		//protected virtual void Set(IntVec3 c, bool val)
		public static void Prefix(Area __instance, IntVec3 c, bool val)
		{
			if (!DebugSettings.godMode || ! val) return;

			bool? godmodeSet = (__instance is Area_BuildRoof) ? val
				: (__instance is Area_NoRoof) ? !val : default(bool?);

			if(godmodeSet.HasValue)
			{
				__instance.Map.roofGrid.SetRoof(c, godmodeSet.Value ? RoofDefOf.RoofConstructed : null);
			}
		}
	}
}

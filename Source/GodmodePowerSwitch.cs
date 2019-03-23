using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(FlickUtility), nameof(FlickUtility.UpdateFlickDesignation))]
	class GodmodePowerSwitch
	{
		public static bool Prefix(Thing t)
		{
			if (!DebugSettings.godMode) return true;
			
			ThingWithComps thingWithComps = (ThingWithComps)t;
			for (int i = 0; i < thingWithComps.AllComps.Count; i++)
			{
				CompFlickable compFlickable = thingWithComps.AllComps[i] as CompFlickable;
				if (compFlickable != null && compFlickable.WantsFlick())
				{
					compFlickable.DoFlick();
				}
			}

			return false;
		}
	}
}

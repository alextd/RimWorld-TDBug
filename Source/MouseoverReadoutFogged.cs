using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace TDBug
{
	//public void MouseoverReadoutOnGUI()
	[HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
	public static class MouseoverReadoutFogged
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return HarmonyLib.Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.Fogged), new Type[] { typeof(IntVec3), typeof(Map) }),
				AccessTools.Method(typeof(MouseoverReadoutFogged), nameof(FoggedAndNotDebug)));
		}
		//public static bool Fogged(this IntVec3 c, Map map)
		public static bool FoggedAndNotDebug(IntVec3 c, Map map)
		{
			if (DebugSettings.godMode) return false;

			return c.Fogged(map);
		}
	}
}

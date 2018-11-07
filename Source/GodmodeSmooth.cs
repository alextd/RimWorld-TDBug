using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(Designator_SmoothSurface), "DesignateSingleCell")]
	public static class GodmodeSmooth
	{
		//public override void DesignateSingleCell(IntVec3 c)
		public static bool Prefix(IntVec3 c, Designator_SmoothSurface __instance)
		{
			if (!DebugSettings.godMode) return true;

			Building edifice = c.GetEdifice(__instance.Map);
			if (edifice != null && edifice.def.IsSmoothable)
			{
				__instance.Map.designationManager.TryRemoveDesignation(c, DesignationDefOf.Mine);
				SmoothableWallUtility.SmoothWall(edifice, __instance.Map.mapPawns.FreeColonistsSpawned.First());
				return false;
			}
			TerrainDef smoothedTerrain = c.GetTerrain(__instance.Map).smoothedTerrain;
			__instance.Map.terrainGrid.SetTerrain(c, smoothedTerrain);
			FilthMaker.RemoveAllFilth(c, __instance.Map);

			return false;
		}
	}
}

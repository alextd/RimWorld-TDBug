using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TDBug
{
	[HarmonyPatch(typeof(Designator_SmoothSurface), "DesignateSingleCell")]
	public static class GodmodeSmooth
	{
		//public override void DesignateSingleCell(IntVec3 c)
		public static bool Prefix(IntVec3 c, Designator_SmoothSurface __instance)
		{
			if (!DebugSettings.godMode) return true;

			Pawn worker = __instance.Map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
			if (worker == null)
			{
				Log.Warning("TDBug can't do work without a colonist to credit it with");
				return false;
			}
			Building edifice = c.GetEdifice(__instance.Map);
			if (edifice != null && edifice.def.IsSmoothable)
			{
				__instance.Map.designationManager.TryRemoveDesignation(c, DesignationDefOf.Mine);
				SmoothableWallUtility.SmoothWall(edifice, worker);
				return false;
			}
			TerrainDef smoothedTerrain = c.GetTerrain(__instance.Map).smoothedTerrain;
			__instance.Map.terrainGrid.SetTerrain(c, smoothedTerrain);
			FilthMaker.RemoveAllFilth(c, __instance.Map);

			return false;
		}
	}
}

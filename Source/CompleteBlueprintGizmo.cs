using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace TDBug
{
	[StaticConstructorOnStartup]
	public static class Tex
	{
		public static Texture2D frame = ContentFinder<Texture2D>.Get("Things/Building/Misc/CaravanPackingSpot");
	}

	[HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
	class CompleteBlueprintGizmo : Command
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static void Postfix(ref IEnumerable<Gizmo> __result, ThingWithComps __instance)
		{
			if (!Prefs.DevMode || !DebugSettings.godMode) return;

			List<Gizmo> result = __result.ToList();
			Map map = __instance.Map;
			Pawn builder = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
			p.workSettings.WorkIsActive(WorkTypeDefOf.Construction) &&
			(p.story == null || !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction)));


			if (__instance is Blueprint || __instance is Frame)
				result.Add(new Command_Action()
				{
					defaultLabel = "Complete",
					icon = TexCommand.Install,
					defaultDesc = "Complete this building",
					action = delegate
					{
						if(builder == null)
						{
							Log.Warning("TDBug can't do construction without a colonist to credit it with");
							return;
						}
						if (__instance is Frame frame)
						{
							frame.CompleteConstruction(builder);
						}

						if (__instance is Blueprint_Build blueprint)
						{
							if (blueprint.def.entityDefToBuild is ThingDef thingDef)
							{
								Thing thing = ThingMaker.MakeThing(thingDef, blueprint.UIStuff());
								thing.SetFactionDirect(Faction.OfPlayer);
								GenSpawn.Spawn(thing, blueprint.Position, map, blueprint.Rotation);
							}
							else if (blueprint.def.entityDefToBuild is TerrainDef terrainDef)
							{
								map.terrainGrid.SetTerrain(blueprint.Position, terrainDef);
								FilthMaker.RemoveAllFilth(blueprint.Position, map);
							}
							blueprint.Destroy(DestroyMode.Vanish);
						}

						if (__instance is Blueprint_Install install)
						{
							if (install.MiniToInstallOrBuildingToReinstall is Building building)
							{
								building.MakeMinified();
							}
							install.TryReplaceWithSolidThing(builder, out Thing thing, out bool ended);
						}
					}
				});

			if (__instance is Blueprint_Build bp)
				result.Add(new Command_Action()
				{
					defaultLabel = "Make Frame",
					icon = Tex.frame,
					defaultDesc = "Make into a frame",
					action = delegate
					{
						if (builder == null)
						{
							Log.Warning("TDBug can't do construction without a colonist to credit it with");
							return;
						}
						bp.TryReplaceWithSolidThing(builder, out Thing thing, out bool dummy);
					}
				});

			__result = result;
		}
	}
}

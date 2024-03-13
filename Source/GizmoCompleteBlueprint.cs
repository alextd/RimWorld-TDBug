using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[StaticConstructorOnStartup]
	public static class Tex
	{
		public static Texture2D frame = ContentFinder<Texture2D>.Get("Things/Building/Misc/PartySpot");
	}

	[HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
	class GizmoCompleteBlueprint : Command
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, ThingWithComps __instance)
		{
			foreach (var r in __result)
				yield return r;

			if (!Prefs.DevMode || !DebugSettings.godMode) yield break;

			if (__instance is Frame frame)
				yield return new Command_Action()
				{
					defaultLabel = "Fill",
					icon = Tex.frame,
					defaultDesc = "Fill Frame with needed materials",
					action = delegate
					{
						//1.5 todo: don't overdo it.
						var thingsToAdd = frame.TotalMaterialCost().Select(count =>
						{
							var thing = ThingMaker.MakeThing(count.thingDef);
							thing.stackCount = count.count;
							return thing;
						});
						frame.GetDirectlyHeldThings().TryAddRangeOrTransfer(thingsToAdd, destroyLeftover: true);
					}
				};


			if (__instance is Blueprint || __instance is Frame)
			{
				Map map = __instance.MapHeld;
				Pawn builder = map?.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
					p.workSettings.WorkIsActive(WorkTypeDefOf.Construction) &&
					!p.WorkTypeIsDisabled(WorkTypeDefOf.Construction));

				yield return new Command_Action()
				{
					defaultLabel = "Complete",
					icon = TexCommand.Install,
					defaultDesc = "Complete this building",
					action = delegate
					{
						if (builder == null)
						{
							Log.Warning("TDBug can't do construction without a colonist to credit it with");
							return;
						}
						if (__instance is Frame f)
						{
							f.CompleteConstruction(builder);
						}

						if (__instance is Blueprint_Build blueprint)
						{
							if (blueprint.def.entityDefToBuild is ThingDef thingDef)
							{
								Thing thing = ThingMaker.MakeThing(thingDef, blueprint.EntityToBuildStuff());
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
				};

				if (__instance is Blueprint_Build bp)
					yield return new Command_Action()
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
							Find.Selector.Select(thing);
						}
					};
			}
		}
	}
}

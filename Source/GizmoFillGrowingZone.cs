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
	[HarmonyPatch(typeof(Zone_Growing), "GetGizmos")]
	class GizmoFillGrowingZone : Command
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Zone_Growing __instance)
		{
			foreach (var r in __result)
				yield return r;

			if (!Prefs.DevMode || !DebugSettings.godMode) yield break;

			Map map = __instance.Map;
			Pawn harvester = map?.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
				p.workSettings.WorkIsActive(WorkTypeDefOf.Growing) &&
				!p.WorkTypeIsDisabled(WorkTypeDefOf.Growing));

			yield return new Command_Action()
			{
				defaultLabel = "Fill",
				icon = TexCommand.Install,
				defaultDesc = "Fill this Growing zone with plants",
				action = delegate
				{
					foreach(var cell in __instance.Cells)
					{
						Plant plant = cell.GetFirstThing<Plant>(map);

						if (plant != null)
						{
							if (!plant.sown)
							{
								plant.Destroy();
								plant = null;
							}
						}
						
						if(plant == null)
						{
							plant = (Plant)GenSpawn.Spawn(__instance.PlantDefToGrow, cell, map);
							plant.Growth = 0.0001f;
							plant.sown = true;

							map.mapDrawer.MapMeshDirty(plant.Position, MapMeshFlag.Things);
						}
					}
				}
			};

			yield return new Command_Action()
			{
				defaultLabel = "Grow",
				icon = ContentFinder<Texture2D>.Get("UI/Designators/Uninstall"),
				defaultDesc = "Grow all plants",
				action = delegate
				{
					foreach (var cell in __instance.Cells)
					{
						Plant plant = cell.GetFirstThing<Plant>(map);

						if (plant != null)
						{
							plant.Growth = 1;

							map.mapDrawer.MapMeshDirty(plant.Position, MapMeshFlag.Things);
						}
					}
				}
			};

			yield return new Command_Action()
			{
				defaultLabel = "Harvest",
				icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest"),
				defaultDesc = "Harvest all plants",
				action = delegate
				{
					foreach (var cell in __instance.Cells)
					{
						Plant plant = cell.GetFirstThing<Plant>(map);

						if (plant != null)
						{
							if (plant.def.plant.harvestedThingDef != null)
							{
								StatDef stat = ((plant.def.plant.harvestedThingDef.IsDrug || plant.def.plant.drugForHarvestPurposes) ? StatDefOf.DrugHarvestYield : StatDefOf.PlantHarvestYield);
								float statValue = harvester.GetStatValue(stat);

								int num = plant.YieldNow();
								if (statValue > 1f)
								{
									num = GenMath.RoundRandom((float)num * statValue);
								}
								if (num > 0)
								{
									Thing thing = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef);
									thing.stackCount = num;
									GenPlace.TryPlaceThing(thing, harvester.Position, map, ThingPlaceMode.Near);
									harvester.records.Increment(RecordDefOf.PlantsHarvested);
								}
							}

							plant.PlantCollected(harvester, PlantDestructionMode.Cut);
						}
					}
				}
			};
		}
	}
}
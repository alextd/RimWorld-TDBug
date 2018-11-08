using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace TDBug
{
	[HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
	class CompleteBlueprintGizmo : Command
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static void Postfix(ref IEnumerable<Gizmo> __result, ThingWithComps __instance)
		{
			if (!Prefs.DevMode) return;

			List<Gizmo> result = __result.ToList();

			if(__instance is Blueprint || __instance is Frame)
				result.Add(new Command_Action()
				{
					defaultLabel = "Complete",
					icon = TexCommand.Install,
					defaultDesc = "Complete this building",
					action = delegate
					{
						if (__instance is Frame frame)
						{
							Pawn builder = frame.Map.mapPawns.FreeColonistsSpawned.First(p =>
							p.workSettings.WorkIsActive(WorkTypeDefOf.Construction) &&
							(p.story == null || !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction)));
							frame.CompleteConstruction(builder);
						}

						if(__instance is Blueprint_Build blueprint && blueprint.def.entityDefToBuild is ThingDef thingDef)
						{
							Thing thing = ThingMaker.MakeThing(thingDef, blueprint.UIStuff());
							thing.SetFactionDirect(Faction.OfPlayer);
							GenSpawn.Spawn(thing, blueprint.Position, blueprint.Map, blueprint.Rotation);
							blueprint.Destroy(DestroyMode.Vanish);
						}
					}
				});

			__result = result;
		}
	}
}

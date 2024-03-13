using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace TDBug
{
	public static class MoreDebugActions
	{
		/*
		 * I would like to insert these things in specific places, but the ease of DebugAction makes that not so possible
		 * 
			{ "T: Try place near stacks of 75...", new DA() {label = "T: Try place near full stacks...", action = "fullStackAction"} },
			{ "Destroy all things", new DA() {label = "Destroy all selected", action = "destroySelectedAction"} },
			{ "T: Heal random injury (10)", new DA() {label = "T: Full Heal", action = "healFullAction", tool="DebugToolMapForPawns" } },
			{ "T: Make roof", new DA() {label = "T: Make roof (by def)", action = "makeRoofByDef"} },
			{ "T: Damage apparel", new DA() {label = "T: Add selected things to inventory", action = "addSeltoInv", tool="DebugToolMapForPawns"} },
			{ "T: Joy -20%", new DA() {label = "T: Need -20%", action = "addNeed"} },
			{ "T: Chemical -20%", new DA() {label = "Fulfill all needs", action = "fulfillAllNeeds"} },
			{ "T: Delete roof", new DA() {label = "T: Set Deep Resource", action = "addDeepResource"} },
			{ "T: Destroy trees 21x21", new DA() {label = "T: Move selection to...", action = "moveSelection", tool="DebugToolMap"} }
		};
		*/


		[DebugAction(DebugActionCategories.General, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void DestroyAllSelected()
		{
			foreach (Thing current in Find.Selector.SelectedObjectsListForReading.Where(s => s is Thing).ToList())
			{
				current.Destroy(DestroyMode.Vanish);
			}
		}


		static List<Hediff_Injury> injuries = new();
		[DebugAction(DebugActionCategories.Pawns, null, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void FullHeal(Pawn p)
		{
			p.health.hediffSet.GetHediffs<Hediff_Injury>(ref injuries);//passing by ref even though method doesn't use it
			foreach (Hediff_Injury hediff_Injury in injuries.Where(x => x.CanHealNaturally() || x.CanHealFromTending()))
			{
				hediff_Injury.Heal(10000f);//probably enough
			}
		}


		[DebugAction(DebugActionCategories.Pawns, "Add selected things to inventory", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void AddSelToInv(Pawn p)
		{
			foreach (Thing t in Find.Selector.SelectedObjectsListForReading.
				Where(o => o is Thing t && t.def.EverHaulable).Cast<Thing>().ToList())//ToList to copy since SelectedObjects changed when despawned
			{
				p.inventory.GetDirectlyHeldThings().TryAdd(t.SplitOff(t.stackCount));
			}
		}


		//1.4 removed OffsetNeed debug action ? ? 
		private static void OffsetNeed(NeedDef nd, float offsetPct)
		{
			foreach (Pawn item in (from t in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())
														 where t is Pawn
														 select t).Cast<Pawn>())
			{
				Need need = item.needs.TryGetNeed(nd);
				if (need != null)
				{
					need.CurLevel += offsetPct * need.MaxLevel * (Event.current.shift ? -1 : 1);
					DebugActionsUtility.DustPuffFrom(item);
				}
			}
		}

		[DebugAction(DebugActionCategories.Pawns, "Need -20% (shift:+)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void AddNeed()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();

			list.Add(new DebugMenuOption("All Needs", DebugMenuOptionMode.Tool, delegate
			{
				foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
					OffsetNeed(current, -0.2f);
			}));

			foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
			{
				NeedDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					OffsetNeed(localDef, -0.2f);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}


		[DebugAction(DebugActionCategories.Pawns, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void FulfillAllNeeds()
		{
			foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
			{
				foreach (Need need in pawn.needs.AllNeeds)
				{
					need.CurLevelPercentage = 1f;
				}
			}
		}


		public static AccessTools.FieldRef<Pawn_WorkSettings, DefMap<WorkTypeDef, int>> prioritiesField = AccessTools.FieldRefAccess<Pawn_WorkSettings, DefMap<WorkTypeDef, int>>("priorities");
		[DebugAction(DebugActionCategories.Pawns, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void EverybodyWorkEverything()
		{
			foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
			{
				if (pawn.skills != null)
				{
					foreach (SkillDef sDef in DefDatabase<SkillDef>.AllDefs)
						pawn.skills.Learn(sDef, 1E+08f);
				}

				if (pawn.workSettings != null)
				{
					prioritiesField.Invoke(pawn.workSettings).SetAll(1);
					pawn.workSettings.Notify_UseWorkPrioritiesChanged();
				}
			}
		}


		[DebugAction(DebugActionCategories.General, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void SetDeepResource()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			for (int size = 0; size < 5; size++)
			{
				int localSize = size;
				list.Add(new DebugMenuOption($"{ size * 2 + 1 }x{ size * 2 + 1 }", DebugMenuOptionMode.Action, delegate
				{
					AddDeepResourceSize(localSize);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		public static void AddDeepResourceSize(int size)
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			list.Add(new DebugMenuOption($"Deplete deep resource", DebugMenuOptionMode.Tool,
				() => AddDeepResources(size, null)));
			foreach (var current in DefDatabase<ThingDef>.AllDefs.Where(def => def.deepCommonality > 0))
			{
				ThingDef localDef = current;
				list.Add(new DebugMenuOption($"Spawn deep resource: {localDef.LabelCap}", DebugMenuOptionMode.Tool,
					() => AddDeepResources(size, localDef)));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		public static void AddDeepResources(int size, ThingDef def)
		{
			Map map = Find.CurrentMap;
			foreach (var pos in CellRect.CenteredOn(UI.MouseCell(), size))
				if (pos.InBounds(map))
					Find.CurrentMap.deepResourceGrid.SetAt(pos, def, def?.deepCountPerCell ?? 0);
		}


		[DebugAction(DebugActionCategories.General, "Move selection to...", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void MoveSelection()
		{
			if (UI.MouseCell().InBounds(Find.CurrentMap))
			{
				IntVec3 pos = UI.MouseCell();
				foreach (Thing current in Find.Selector.SelectedObjectsListForReading.Where(s => s is Thing).ToList())
				{
					current.Position = pos;
					if (current is Pawn pawn)
					{
						pawn.Notify_Teleported(false, true);
					}
				}
			}
		}


		[DebugAction(DebugActionCategories.Pawns, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void ImprisonSelected()
		{
			if (UI.MouseCell().InBounds(Find.CurrentMap))
			{
				IntVec3 pos = UI.MouseCell();
				foreach (Pawn pawn in Find.Selector.SelectedObjectsListForReading.Where(s => s is Pawn).ToList())
				{
					if (pawn.guest.Released)
					{
						pawn.guest.Released = false;
						pawn.guest.interactionMode = PrisonerInteractionModeDefOf.MaintainOnly;
						GenGuest.RemoveHealthyPrisonerReleasedThoughts(pawn);
					}
					if (!pawn.IsPrisonerOfColony)
					{
						pawn.guest.CapturedBy(Faction.OfPlayer, pawn);
					}
				}
			}
		}

		//-----------
		//Spawn needed materials for blueprints and bills:
		//-----------

		[DebugAction(DebugActionCategories.Spawning, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void SpawnNeededMaterials()
		{
			foreach (Thing thing in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
				if (thing is IConstructible cons)
				{
					Log.Message($"Spawning for {thing}:");
					SpawnMaterialsNear(cons.TotalMaterialCost(), thing.Position);
				}

			foreach (Thing thing in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
				if (thing is IConstructible cons)
				{
					Log.Message($"Spawning for {thing}:");
					SpawnMaterialsNear(cons.TotalMaterialCost(), thing.Position);
				}

			foreach (Thing thing in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.PotentialBillGiver).ListFullCopy())  //ListFullCopy because we might spawn corpses that change this list
				if (thing is Building_WorkTable worktable)
				{
					Log.Message($"Spawning for {thing}:");
					foreach (Bill bill in worktable.BillStack)
					{
						Log.Message($"-Spawning for {bill}:");

						foreach (var ing in bill.recipe.ingredients)
						{
							Log.Message($"--Spawning for {ing}:");
							ThingDef mat = FindIngredient(bill, ing);
							SpawnMaterialNear(mat, bill.RepeatCount() * ing.CountRequiredOfFor(mat, bill.recipe), thing.Position);
						}
					}
				}

		}

		public static ThingDef FindIngredient(Bill bill, IngredientCount ing)
		{
			if (ing.IsFixedIngredient) return ing.FixedIngredient;

			if (ing.filter.AllowedThingDefs.FirstOrDefault(
				d => bill.recipe.fixedIngredientFilter.Allows(d) &&
					bill.ingredientFilter.Allows(d)) is ThingDef def)
				return def;

			Log.Warning("Whoops, sorry, don't know what to spawn for that");
			return ThingDefOf.WoodLog;
		}

		public static int RepeatCount(this Bill bill)
		{
			Bill_Production billP = bill as Bill_Production;
			if (billP?.repeatMode == BillRepeatModeDefOf.Forever)
			{
				return 20;
			}
			if (billP?.repeatMode == BillRepeatModeDefOf.RepeatCount)
			{
				return billP.repeatCount;
			}
			if (billP?.repeatMode == BillRepeatModeDefOf.TargetCount)
			{
				float todo = billP.targetCount - billP.recipe.WorkerCounter.CountProducts(billP);
				todo /= bill.recipe.products.FirstOrDefault()?.count ?? 1;
				return UnityEngine.Mathf.CeilToInt(todo);
			}
			return 1;
		}

		public static void SpawnMaterialsNear(List<ThingDefCountClass> mats, IntVec3 pos)
		{
			foreach (var mat in mats)
				SpawnMaterialNear(mat.thingDef, mat.count, pos);
		}
		public static void SpawnMaterialNear(ThingDef def, int total, IntVec3 pos)
		{
			Log.Message($"---Spawning {def}x{total}");
			try
			{
				while (total > 0)
				{
					if (def.IsCorpse)
					{
						SpawnCorpse(def, pos);
						total--;
						continue;
					}
					Thing thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));

					thing.stackCount = total > def.stackLimit ? def.stackLimit : total;
					total -= thing.stackCount;

					GenSpawn.Spawn(thing, pos, Find.CurrentMap);
				}
			}
			catch(Exception e)
			{
				Log.Warning($"Sorry couldn't spawn that for some reason({e})");
			}
		}

		// taken from SymbolResolver_DesiccatedCorpses
		public static void SpawnCorpse(ThingDef corpseDef, IntVec3 spawnPosition)
		{
			ThingDef pawnDef = corpseDef.ingestible.sourceDef;
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnDef.race.AnyPawnKind));
			if (!pawn.Dead)
			{
				pawn.Kill(null);
			}
			if (pawn.inventory != null)
			{
				pawn.inventory.DestroyAll();
			}
			if (pawn.apparel != null)
			{
				pawn.apparel.DestroyAll();
			}
			if (pawn.equipment != null)
			{
				pawn.equipment.DestroyAllEquipment();
			}
			pawn.Corpse.Age = 1000;
			pawn.relations.hidePawnRelations = true;
			GenSpawn.Spawn(pawn.Corpse, spawnPosition, Find.CurrentMap);
			pawn.Corpse.GetComp<CompRottable>().RotProgress += pawn.Corpse.Age;
		}

		//-----------
		//-----------
		//-----------


	}
}

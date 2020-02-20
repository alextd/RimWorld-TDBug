using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(Page_SelectStoryteller), nameof(Page_SelectStoryteller.DoWindowContents))]
	public static class QuickstartFromScenarioConfig
	{
		//public override void DoWindowContents(Rect rect)
		public static MethodInfo DoBottomButtonsInfo = AccessTools.Method(typeof(Page), "DoBottomButtons");
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DoBottomButtonsDebug = AccessTools.Method(typeof(QuickstartFromScenarioConfig), nameof(DoBottomButtonsDebug));

			return Transpilers.MethodReplacer(instructions, DoBottomButtonsInfo, DoBottomButtonsDebug);
		}

		public static void DoBottomButtonsDebug(Page page, Rect rect, string nextLabel = null, string midLabel = null, Action midAct = null, bool showNext = true, bool doNextOnKeypress = true)
		{
			if (Prefs.DevMode)
			{
				Action debugAction = QuickScenarioConfiguration;
				DoBottomButtonsInfo.Invoke(page, new object[] { rect, nextLabel, "Quickstart", debugAction, showNext, doNextOnKeypress });
			}
			else
				DoBottomButtonsInfo.Invoke(page, new object[] { rect, nextLabel, midLabel, midAct, showNext, doNextOnKeypress });
		}

		public static void QuickScenarioConfiguration()
		{
			Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
			Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal);
			Find.GameInitData.ChooseRandomStartingTile();
			Find.GameInitData.mapSize = 150;
			Find.GameInitData.PrepForMapGen();
			Find.Scenario.PreMapGenerate();


			LongEventHandler.QueueLongEvent(null, "Play", "GeneratingMap", true, null);
		}
	}
}

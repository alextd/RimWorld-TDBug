using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

namespace TDBug
{
	[HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Initialize))]
	class GodmodeSpawnFullFuel
	{
		//public override void Initialize(CompProperties props)
		public static void Postfix(CompRefuelable __instance, ref float ___fuel)
		{
			if (!DebugSettings.godMode) return;

			___fuel = __instance.Props.fuelCapacity;
		}
	}

	[HarmonyPatch(typeof(CompPower), nameof(CompPower.SetUpPowerVars))]
	class GodmodeSpawnFullEnergy
	{
		//public virtual void SetUpPowerVars()
		public static void Postfix(CompPower __instance)
		{
			if (!DebugSettings.godMode) return;

			if (__instance is CompPowerBattery compBattery)
			{
				compBattery.SetStoredEnergyPct(1);
			}
		}
	}
}

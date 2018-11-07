using System.Reflection;
using System;
using Verse;
using UnityEngine;
using Harmony;
using RimWorld;

namespace TDBug
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.TDBug.main");
			
			harmony.PatchAll();
		}
	}
}
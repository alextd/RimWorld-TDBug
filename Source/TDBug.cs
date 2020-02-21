using System.Reflection;
using System;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace TDBug
{
	[StaticConstructorOnStartup]
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			//TODO: patches before load. Probably won't be needed in TDBug. Patching here causes DefOf warnings.
		}

		static Mod()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Harmony harmony = new Harmony("Uuugggg.rimworld.TDBug.main");
			LongEventHandler.QueueLongEvent(() => harmony.PatchAll(), null, true, null);
		}
	}
}
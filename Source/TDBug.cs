using System.Reflection;
using System;
using Verse;
using UnityEngine;
using Harmony;
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
			HarmonyInstance.DEBUG = true;
#endif
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.TDBug.main");
			LongEventHandler.QueueLongEvent(() => harmony.PatchAll(), null, true, null);
		}
	}
}
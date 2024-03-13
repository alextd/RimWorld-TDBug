global using LudeonTK;


using System.Reflection;
using System;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace TDBug
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif

			Harmony harmony = new Harmony("Uuugggg.rimworld.TDBug.main");

			harmony.PatchAll();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld; //for SoundDefOf I guess
using HarmonyLib;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(DebugWindowsOpener), nameof(DebugWindowsOpener.DevToolStarterOnGUI))]
	[StaticConstructorOnStartup]
	class GodmodeScreenGlow
	{
		public static Texture2D gradientTex = ContentFinder<Texture2D>.Get("UI/Overlays/TargetHighlight_Side");
		public static Color glowColor = new Color(1, 1, 0, .5f);
		public static void Postfix()
		{
			if (Current.ProgramState == ProgramState.Playing && DebugSettings.godMode)
			{
				GUI.color = glowColor;
				GUI.DrawTexture(new Rect(0, 0, Screen.width, 50f), gradientTex);

				//Tried to rotate to draw on sides, not working, oh well.
				//Widgets.DrawTextureRotated(new Rect(0, 0, Screen.width, 50f), gradientTex, 0);
				//Widgets.DrawTextureRotated(new Rect(0, 0, 50f, Screen.height), gradientTex, -90);
				//Widgets.DrawTextureRotated(new Rect(Screen.width - 50f, 0, 50f, Screen.height), gradientTex, 90);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace TDBug
{
	[HarmonyPatch(typeof(Window), "SetInitialSizeAndPosition")]
	public static class LoadWindowSize
	{
		public static HashSet<Type> windowTypes = new HashSet<Type>()
		{ typeof(EditWindow_Log), typeof(EditWindow_TweakValues), typeof(EditWindow_DebugInspector) };
		public static Dictionary<Type, Rect> savedSizes = new Dictionary<Type, Rect>();

		//protected virtual void SetInitialSizeAndPosition()
		public static void Postfix(Window __instance)
		{
			Type windowType = __instance.GetType();
			if (savedSizes.TryGetValue(windowType, out Rect savedRect))
			{
				__instance.windowRect = savedRect;
				savedSizes.Remove(windowType);
			}
		}
	}
	
	[HarmonyPatch(typeof(Window), "PreClose")]
	public static class SaveWindowSize
	{
		public static void Prefix(Window __instance)
		{
			Type windowType = __instance.GetType();
			if(LoadWindowSize.windowTypes.Contains(windowType))
				LoadWindowSize.savedSizes[windowType] = __instance.windowRect;
		}
	}
}

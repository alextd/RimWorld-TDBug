using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;

namespace TDBug
{
	[HarmonyPatch(typeof(Page_ModsConfig), MethodType.Constructor)]
	class CloseEsc
	{
		public static void Postfix(Page_ModsConfig __instance)
		{
			__instance.closeOnCancel = true;
		}
	}
	
	[HarmonyPatch(typeof(Page_ModsConfig), "PreOpen")]
	class OpenSaveList
	{
		public static List<string> modIDs;
		public static void Postfix()
		{
			modIDs = ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageId).ToList();
		}
	}
	
	[HarmonyPatch(typeof(Page_ModsConfig), "PostClose")]
	class CloseConfirm
	{
		public static bool Prefix(Page_ModsConfig __instance, int ___activeModsWhenOpenedHash)
		{
			if (___activeModsWhenOpenedHash != ModLister.InstalledModsListHash(true))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				Action confirm = delegate
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					ModsConfig.Save();
					ModsConfig.RestartFromChangedMods();
				};
				Action save = delegate
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					ModsConfig.Save();
				};
				Action cancel = delegate
				{
					ModsConfig.SetActiveToList(OpenSaveList.modIDs);
				};
				var box = new Dialog_MessageBox("Restart with new mod list?",
					"Yes".Translate(), confirm, "No".Translate(), cancel, null, true, confirm, cancel)
				{
					buttonCText = "Just save mod list",
					buttonCAction = save,
					buttonCClose = false
				};
				Find.WindowStack.Add(box);

				return false;
			}
			return true;
		}
	}
}

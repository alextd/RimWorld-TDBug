using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using Verse.Sound;
using RimWorld;
using Harmony;

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
			modIDs = ModsConfig.ActiveModsInLoadOrder.Select(m => m.Identifier).ToList();
		}
	}
	
	[HarmonyPatch(typeof(Page_ModsConfig), "PostClose")]
	class CloseConfirm
	{
		public static FieldInfo activeHash = AccessTools.Field(typeof(Page_ModsConfig), "activeModsWhenOpenedHash");
		public static bool Prefix(Page_ModsConfig __instance)
		{
			if ((int)activeHash.GetValue(__instance) != ModLister.InstalledModsListHash(true))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				Action confirm = delegate
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					ModsConfig.Save();
					ModsConfig.RestartFromChangedMods();
				};
				Action cancel = delegate
				{
					ModsConfig.SetActiveToList(OpenSaveList.modIDs);
				};
				Find.WindowStack.Add(new Dialog_MessageBox("Restart with new mod list?",
					"Yes".Translate(), confirm, "No".Translate(), cancel, null, true, confirm, cancel));

				return false;
			}
			return true;
		}
	}
}

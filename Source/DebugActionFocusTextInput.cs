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
	[HarmonyPatch(typeof(Dialog_OptionLister), nameof(Dialog_OptionLister.DoWindowContents))]
	public static class DebugActionFocusTextInput
	{
		public static void Prefix()
		{
			GUI.SetNextControlName("Dialog_OptionLister_TextInput");
		}
		
		public static void Postfix(Dialog_OptionLister __instance)
		{
			//The inital opened window would capture the '/' used to open it, so wait a frame to actually focus
			if (!FocusedBool.focused && FocusedBool.wait)
			{
				UI.FocusControl("Dialog_OptionLister_TextInput", __instance);
				FocusedBool.focused = true;
			}
			if (!FocusedBool.wait) FocusedBool.wait = true;
		}
	}

	[HarmonyPatch(typeof(Dialog_OptionLister), MethodType.Constructor)]
	public static class FocusedBool
	{
		public static bool focused, wait;
		public static void Postfix()
		{
			focused = wait = false;
		}
	}
}
